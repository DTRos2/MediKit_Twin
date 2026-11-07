"""
실제 씬에서 저장한 슬롯 크롭(InspectionCaptures)을 학습용 데이터셋 폴더로 만든다.

합성 이미지만으로는 실제 씬의 흐릿한 구성품(칸 바닥과 색이 비슷한 면봉 등)을 충분히 학습하지 못해서,
실제 크롭에 정답 라벨을 붙여 합성 데이터와 함께 학습하고 평가한다.

사용법 (MLTraining 폴더에서):
    python build_real_set.py --out ..\\Datasets\\RealCaptures\\v1 --train-sessions 20261104-095457 20261104-142341 --test-sessions 20261104-155050

정답 라벨 규칙:
  - 로트의 키트 코드(real_lots.json)가 정한 레시피의 슬롯별 구성품을 정답으로 한다.
  - 검사 구역은 누락 키트를 일부러 만들므로(불량 주입), 기준 모델이 empty로 강하게 읽은 슬롯(--empty-threshold)은 empty로 둔다.
    이 슬롯은 label_source=model_empty로 표시한다. 사람이 이미지를 보고 확인한 것이 아니다.
출력: labels.csv(train/test 분할)와 크롭 PNG 복사본.
"""
import argparse
import csv
import json
import re
import shutil
from pathlib import Path

import numpy as np
from PIL import Image

from spec import INPUT_HEIGHT, INPUT_WIDTH

ROOT = Path(__file__).resolve().parent.parent
RECIPE_DIR = ROOT / "Assets" / "_Project" / "Recipes"
CAPTURES = ROOT / "InspectionCaptures"


def read_recipes():
    """Recipe_*.asset에서 키트 코드별 슬롯 번호 -> 구성품 ID를 읽는다."""
    recipes = {}
    for path in RECIPE_DIR.glob("Recipe_*.asset"):
        text = path.read_text(encoding="utf-8")
        code = re.search(r"_kitCode:\s*(\S+)", text).group(1)
        pairs = re.findall(r"_componentId:\s*(\S+)\s+_shelfSlotId:\s*\S+\s+_slotNumber:\s*(\d+)", text)
        recipes[code] = {int(slot): component for component, slot in pairs}
    return recipes


def load_model(model_path, classes_path):
    import onnxruntime as ort

    classes = json.loads(Path(classes_path).read_text(encoding="utf-8"))["classes"]
    return ort.InferenceSession(str(model_path)), classes


def predict(session, classes, image_path):
    image = Image.open(image_path).convert("RGB").resize((INPUT_WIDTH, INPUT_HEIGHT), Image.BILINEAR)
    x = (np.asarray(image, dtype=np.float32) / 255.0).transpose(2, 0, 1)[None]
    logits = session.run(None, {"input": x})[0][0]
    p = np.exp(logits - logits.max())
    p /= p.sum()
    i = int(p.argmax())
    return classes[i], float(p[i])


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", required=True, help="만들 데이터셋 폴더")
    parser.add_argument("--train-sessions", nargs="*", default=[], help="학습에 쓸 InspectionCaptures 세션 폴더 이름")
    parser.add_argument("--test-sessions", nargs="*", default=[], help="평가에만 쓸 세션 폴더 이름")
    parser.add_argument("--lots", default="real_lots.json")
    parser.add_argument("--model", default="../Assets/_Project/Models/Inspection/slot_cnn.onnx", help="empty 슬롯을 가르는 기준 모델")
    parser.add_argument("--classes", default="../Assets/_Project/Models/Inspection/classes.json")
    parser.add_argument("--empty-threshold", type=float, default=0.9)
    args = parser.parse_args()

    recipes = read_recipes()
    lots = json.loads(Path(args.lots).read_text(encoding="utf-8"))
    session, classes = load_model(args.model, args.classes)

    out = Path(args.out)
    (out / "crops").mkdir(parents=True, exist_ok=True)
    rows = []
    splits = [("train", s) for s in args.train_sessions] + [("test", s) for s in args.test_sessions]
    for split, name in splits:
        for lot, kit in sorted(lots[name].items(), key=lambda kv: int(kv[0])):
            for slot in sorted(recipes[kit]):
                src = CAPTURES / name / f"LOT-{int(lot):04d}" / f"slot{slot}.png"
                if not src.exists():
                    continue
                expected = recipes[kit][slot]
                label, source = expected, "recipe"
                predicted, confidence = predict(session, classes, src)
                if predicted == "empty" and confidence >= args.empty_threshold:
                    label, source = "empty", "model_empty"
                dst = out / "crops" / f"{name}_lot{int(lot):04d}_slot{slot}.png"
                shutil.copyfile(src, dst)
                rows.append({
                    "image_path": f"crops/{dst.name}",
                    "split": split,
                    "case_index": f"{name[-6:]}-{int(lot):02d}",
                    "kit_code": kit,
                    "slot_number": slot,
                    "expected_component": expected,
                    "label": label,
                    "box_variant": source,
                })

    with open(out / "labels.csv", "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    for split in ("train", "test"):
        part = [r for r in rows if r["split"] == split]
        empties = sum(r["label"] == "empty" for r in part)
        print(f"{split}: 크롭 {len(part)}장 (empty로 둔 슬롯 {empties}개)")
    print(f"출력: {out}")


if __name__ == "__main__":
    main()
