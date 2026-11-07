"""
슬롯 크롭 CNN 학습, 평가(NFR-04), ONNX 내보내기.

사용법 (MLTraining 폴더에서):
    python train.py --dataset ..\\Datasets\\<데이터셋 폴더>

결과는 runs/<시각>/ 에 저장한다:
    best.pt        검증 정확도가 가장 높았던 가중치
    slot_cnn.onnx  Unity Sentis용 모델 (입력 input: N x 3 x 72 x 128, 0~1 RGB / 출력 logits: N x 클래스 수)
    classes.json   출력 번호 -> 라벨(empty 또는 구성품 ID)과 입력 규격
    metrics.json   검증, 테스트 지표 (슬롯 단위와 박스 단위 NFR-04)
"""
import argparse
import json
import random
import time
from pathlib import Path

import numpy as np
import torch
from torch import nn
from torch.utils.data import ConcatDataset, DataLoader

from data import SlotCropDataset, build_classes, read_labels
from metrics import box_metrics, slot_accuracy
from model import SlotCnn
from spec import INPUT_CHANNELS, INPUT_HEIGHT, INPUT_WIDTH, TARGET_METRIC


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dataset", required=True, help="labels.csv가 있는 데이터셋 폴더")
    parser.add_argument("--out", default=None, help="결과 폴더 (기본: runs/<시각>)")
    parser.add_argument("--epochs", type=int, default=30)
    parser.add_argument("--batch", type=int, default=64)
    parser.add_argument("--lr", type=float, default=2e-3)
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--device", default="auto", help="auto, cpu, cuda")
    parser.add_argument("--no-augment", action="store_true", help="학습 때 밝기와 잡음 보강을 끈다")
    parser.add_argument("--real", default=None, help="build_real_set.py가 만든 실제 크롭 폴더. train 분할을 합성 학습 데이터에 섞는다")
    parser.add_argument("--real-repeat", type=int, default=8, help="실제 크롭을 학습에서 몇 번 반복해 넣을지(수가 적어서 늘린다)")
    parser.add_argument("--real-test", default=None, help="평가에만 쓰는 실제 크롭 폴더(보통 --real과 같은 폴더의 test 분할). 학습에는 쓰지 않는다")
    return parser.parse_args()


def pick_device(name):
    if name != "auto":
        return torch.device(name)
    return torch.device("cuda" if torch.cuda.is_available() else "cpu")


def seed_everything(seed):
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)


@torch.no_grad()
def predict(model, dataset, device, batch_size):
    """(예측 클래스 번호 목록, 확신도 목록)."""
    model.eval()
    predictions, confidences = [], []
    for images, _ in DataLoader(dataset, batch_size=batch_size):
        probabilities = torch.softmax(model(images.to(device)), dim=1)
        confidence, index = probabilities.max(dim=1)
        predictions.extend(index.cpu().tolist())
        confidences.extend(confidence.cpu().tolist())
    return predictions, confidences


def evaluate(model, dataset, classes, device, batch_size):
    predictions, confidences = predict(model, dataset, device, batch_size)
    labels = [classes[i] for i in predictions]
    result = {
        "slot_accuracy": slot_accuracy(dataset.rows, labels),
        "mean_confidence": float(np.mean(confidences)),
        "box": box_metrics(dataset.rows, labels),
    }

    errors = {}
    for row, label in zip(dataset.rows, labels):
        if row["label"] != label:
            key = f"{row['label']} -> {label}"
            errors[key] = errors.get(key, 0) + 1
    result["slot_errors"] = dict(sorted(errors.items(), key=lambda kv: -kv[1])[:15])
    return result


def evaluate_real(model, dataset, classes, device, batch_size):
    """실제 크롭의 슬롯 정확도와 로트별 통과 여부(모든 슬롯이 정답이면 통과). 박스 단위 NFR-04와는 다른, 실제 씬 확인용 지표이다."""
    predictions, confidences = predict(model, dataset, device, batch_size)
    labels = [classes[i] for i in predictions]
    lots, errors = {}, {}
    for row, label, confidence in zip(dataset.rows, labels, confidences):
        lot = lots.setdefault(row["case_index"], {"kit": row["kit_code"], "pass": True, "wrong": []})
        if row["label"] != label:
            lot["pass"] = False
            lot["wrong"].append(f"s{row['slot_number']}: {row['label']} -> {label} ({confidence:.2f})")
            key = f"{row['label']} -> {label}"
            errors[key] = errors.get(key, 0) + 1

    return {
        "slot_accuracy": float(np.mean([r["label"] == l for r, l in zip(dataset.rows, labels)])),
        "lots": len(lots),
        "lots_passed": sum(1 for lot in lots.values() if lot["pass"]),
        "failed_lots": {k: v["wrong"] for k, v in sorted(lots.items()) if not v["pass"]},
        "slot_errors": dict(sorted(errors.items(), key=lambda kv: -kv[1])),
    }


def export_onnx(model, classes, path):
    """Sentis가 읽는 ONNX로 내보내고, onnxruntime으로 PyTorch와 같은 출력인지 확인한다."""
    model.eval().cpu()
    dummy = torch.rand(1, INPUT_CHANNELS, INPUT_HEIGHT, INPUT_WIDTH)
    kwargs = dict(
        input_names=["input"],
        output_names=["logits"],
        dynamic_axes={"input": {0: "batch"}, "logits": {0: "batch"}},
        opset_version=15,
    )
    try:
        torch.onnx.export(model, dummy, str(path), dynamo=False, **kwargs)
    except TypeError:
        torch.onnx.export(model, dummy, str(path), **kwargs)

    try:
        import onnxruntime as ort
    except ImportError:
        print("onnxruntime이 없어 ONNX 출력 검증은 건너뛴다. (pip install onnxruntime)")
        return None

    batch = torch.rand(8, INPUT_CHANNELS, INPUT_HEIGHT, INPUT_WIDTH)
    session = ort.InferenceSession(str(path), providers=["CPUExecutionProvider"])
    onnx_out = session.run(["logits"], {"input": batch.numpy()})[0]
    torch_out = model(batch).detach().numpy()
    diff = float(np.abs(onnx_out - torch_out).max())
    print(f"ONNX 검증: PyTorch와 최대 출력 차이 {diff:.2e}")
    return diff


def main():
    args = parse_args()
    seed_everything(args.seed)
    device = pick_device(args.device)
    print(f"장치: {device}")

    dataset_dir = Path(args.dataset)
    classes = build_classes([r for r in read_labels(dataset_dir) if r["split"] == "train"])
    print(f"클래스 {len(classes)}개: {classes}")

    train_set = SlotCropDataset(dataset_dir, "train", classes, augment=not args.no_augment)
    val_set = SlotCropDataset(dataset_dir, "validation", classes)
    test_set = SlotCropDataset(dataset_dir, "test", classes)
    print(f"크롭 수: 학습 {len(train_set)}, 검증 {len(val_set)}, 테스트 {len(test_set)}")

    if args.real:
        real_train = SlotCropDataset(args.real, "train", classes, augment=not args.no_augment)
        train_set = ConcatDataset([train_set] + [real_train] * max(1, args.real_repeat))
        print(f"실제 크롭 {len(real_train)}장 x {max(1, args.real_repeat)}을 학습에 섞었다 (학습 합계 {len(train_set)})")

    out_dir = Path(args.out) if args.out else Path("runs") / time.strftime("%Y%m%d-%H%M%S")
    out_dir.mkdir(parents=True, exist_ok=True)

    model = SlotCnn(len(classes)).to(device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)
    loss_fn = nn.CrossEntropyLoss()
    loader = DataLoader(train_set, batch_size=args.batch, shuffle=True)

    best_accuracy, best_epoch = -1.0, 0
    for epoch in range(1, args.epochs + 1):
        model.train()
        total_loss = 0.0
        for images, targets in loader:
            images, targets = images.to(device), targets.to(device)
            optimizer.zero_grad()
            loss = loss_fn(model(images), targets)
            loss.backward()
            optimizer.step()
            total_loss += loss.item() * len(images)
        scheduler.step()

        predictions, _ = predict(model, val_set, device, args.batch)
        accuracy = float(np.mean([p == t for p, t in zip(predictions, val_set.targets)]))
        print(f"에폭 {epoch:3d}  학습 손실 {total_loss / len(train_set):.4f}  검증 슬롯 정확도 {accuracy:.4f}")
        if accuracy > best_accuracy:
            best_accuracy, best_epoch = accuracy, epoch
            torch.save(model.state_dict(), out_dir / "best.pt")

    print(f"최고 검증 정확도 {best_accuracy:.4f} (에폭 {best_epoch})")
    model.load_state_dict(torch.load(out_dir / "best.pt", map_location=device))

    results = {
        "best_epoch": best_epoch,
        "validation": evaluate(model, val_set, classes, device, args.batch),
        "test": evaluate(model, test_set, classes, device, args.batch),
    }
    if args.real_test:
        real_test = SlotCropDataset(args.real_test, "test", classes)
        results["real_test"] = evaluate_real(model, real_test, classes, device, args.batch)
        r = results["real_test"]
        print("\n=== 실제 크롭 평가 (학습에 쓰지 않은 세션) ===")
        print(f"슬롯 정확도 {r['slot_accuracy']:.4f}, 로트 통과 {r['lots_passed']}/{r['lots']}")
        for lot, wrong in r["failed_lots"].items():
            print(f"  실패 {lot}: {'; '.join(wrong)}")

    test_box = results["test"]["box"]
    results["nfr04_passed"] = bool(
        test_box["accuracy"] >= TARGET_METRIC and test_box["defect_recall"] >= TARGET_METRIC
    )

    (out_dir / "classes.json").write_text(
        json.dumps(
            {
                "classes": classes,
                "input": {
                    "width": INPUT_WIDTH,
                    "height": INPUT_HEIGHT,
                    "channels": INPUT_CHANNELS,
                    "layout": "NCHW",
                    "range": "0..1 RGB",
                },
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    results["onnx_max_diff"] = export_onnx(model, classes, out_dir / "slot_cnn.onnx")
    (out_dir / "metrics.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")

    print("\n=== 테스트 세트 (NFR-04) ===")
    print(f"슬롯 정확도      : {results['test']['slot_accuracy']:.4f}")
    print(f"박스 정확도      : {test_box['accuracy']:.4f}  (목표 {TARGET_METRIC:.2f})")
    print(f"불량 재현율      : {test_box['defect_recall']:.4f}  (목표 {TARGET_METRIC:.2f})")
    print(f"정상 오경보율    : {test_box['false_alarm_rate']:.4f}")
    print(f"NFR-04 통과 여부 : {results['nfr04_passed']}")
    print(f"결과 폴더        : {out_dir}")


if __name__ == "__main__":
    main()
