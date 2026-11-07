"""NFR-04 지표 계산. 슬롯 분류 결과를 박스 단위 정상/불량 판정으로 올려서 정확도와 불량 재현율을 구한다."""
from collections import defaultdict


def slot_accuracy(rows, predictions):
    correct = sum(1 for row, pred in zip(rows, predictions) if row["label"] == pred)
    return correct / len(rows)


def box_metrics(rows, predictions):
    """
    박스 하나의 슬롯 중 하나라도 레시피(expected_component)와 다르게 분류되면 불량으로 판정한다.
    정답 불량은 box_variant가 normal이 아닌 박스이다. 반환: 정확도, 불량 재현율, 혼동 수.
    """
    boxes = defaultdict(lambda: {"pred_defect": False, "true_defect": False})
    for row, pred in zip(rows, predictions):
        box = boxes[(row["split"], row["case_index"])]
        box["pred_defect"] |= pred != row["expected_component"]
        box["true_defect"] = row["box_variant"] != "normal"

    tp = sum(1 for b in boxes.values() if b["true_defect"] and b["pred_defect"])
    fn = sum(1 for b in boxes.values() if b["true_defect"] and not b["pred_defect"])
    tn = sum(1 for b in boxes.values() if not b["true_defect"] and not b["pred_defect"])
    fp = sum(1 for b in boxes.values() if not b["true_defect"] and b["pred_defect"])
    total = tp + fn + tn + fp

    return {
        "boxes": total,
        "accuracy": (tp + tn) / total,
        "defect_recall": tp / (tp + fn) if tp + fn else float("nan"),
        "false_alarm_rate": fp / (fp + tn) if fp + tn else float("nan"),
        "true_defect": tp + fn,
        "missed_defect": fn,
        "false_alarm": fp,
    }
