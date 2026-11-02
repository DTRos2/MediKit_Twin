"""합성 데이터셋(Unity의 Synthetic Dataset 출력) 읽기."""
import csv
from pathlib import Path

import numpy as np
import torch
from PIL import Image
from torch.utils.data import Dataset

from spec import INPUT_HEIGHT, INPUT_WIDTH


def read_labels(dataset_dir):
    """labels.csv의 모든 행을 dict 목록으로 읽는다."""
    path = Path(dataset_dir) / "labels.csv"
    with open(path, newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def build_classes(rows):
    """학습 라벨에 나온 클래스 목록(정렬). 인덱스가 곧 모델 출력 번호이다."""
    return sorted({row["label"] for row in rows})


def load_image(path):
    """크롭 PNG를 모델 입력 텐서(3 x H x W, 0~1, RGB)로 바꾼다."""
    image = Image.open(path).convert("RGB").resize((INPUT_WIDTH, INPUT_HEIGHT), Image.BILINEAR)
    array = np.asarray(image, dtype=np.float32) / 255.0
    return torch.from_numpy(array).permute(2, 0, 1).contiguous()


class SlotCropDataset(Dataset):
    """한 분할(train, validation, test)의 슬롯 크롭. 이미지가 작아 모두 메모리에 올린다."""

    def __init__(self, dataset_dir, split, classes, augment=False):
        root = Path(dataset_dir)
        self.rows = [row for row in read_labels(root) if row["split"] == split]
        if not self.rows:
            raise ValueError(f"labels.csv에 '{split}' 행이 없다.")

        index = {name: i for i, name in enumerate(classes)}
        unknown = {row["label"] for row in self.rows} - set(index)
        if unknown:
            raise ValueError(f"'{split}'에 학습에 없던 클래스가 있다: {sorted(unknown)}")

        self.images = [load_image(root / row["image_path"]) for row in self.rows]
        self.targets = [index[row["label"]] for row in self.rows]
        self.augment = augment

    def __len__(self):
        return len(self.rows)

    def __getitem__(self, i):
        image = self.images[i]
        if self.augment:
            # 데이터 자체가 위치, 회전, 조명을 랜덤화했으므로 밝기와 잡음만 약하게 더한다.
            gain = 1.0 + (torch.rand(1).item() - 0.5) * 0.2
            noise = torch.randn_like(image) * 0.01
            image = (image * gain + noise).clamp(0.0, 1.0)
        return image, self.targets[i]
