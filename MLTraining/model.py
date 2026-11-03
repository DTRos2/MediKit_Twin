"""슬롯 크롭 분류용 작은 CNN. 입력은 0~1 RGB이고 정규화는 모델 안에서 하므로 Unity는 0~1 값만 넣으면 된다."""
import torch
from torch import nn

from spec import INPUT_CHANNELS

MEAN = (0.5, 0.5, 0.5)
STD = (0.25, 0.25, 0.25)


def conv_block(in_channels, out_channels):
    return nn.Sequential(
        nn.Conv2d(in_channels, out_channels, kernel_size=3, padding=1, bias=False),
        nn.BatchNorm2d(out_channels),
        nn.ReLU(inplace=True),
        nn.MaxPool2d(2),
    )


class SlotCnn(nn.Module):
    def __init__(self, num_classes):
        super().__init__()
        self.register_buffer("mean", torch.tensor(MEAN).view(1, INPUT_CHANNELS, 1, 1))
        self.register_buffer("std", torch.tensor(STD).view(1, INPUT_CHANNELS, 1, 1))
        self.features = nn.Sequential(
            conv_block(INPUT_CHANNELS, 16),
            conv_block(16, 32),
            conv_block(32, 64),
            conv_block(64, 128),
        )
        self.pool = nn.AdaptiveAvgPool2d(1)
        self.dropout = nn.Dropout(0.2)
        self.classifier = nn.Linear(128, num_classes)

    def forward(self, x):
        x = (x - self.mean) / self.std
        x = self.pool(self.features(x)).flatten(1)
        return self.classifier(self.dropout(x))
