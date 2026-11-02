"""학습, 평가, 내보내기가 함께 쓰는 입력 규격. Unity CnnInspector의 전처리와 같아야 한다."""

# 슬롯 크롭(약 155x87 픽셀)의 가로세로 비를 유지한 입력 크기
INPUT_WIDTH = 128
INPUT_HEIGHT = 72
INPUT_CHANNELS = 3

# 정상 박스의 빈 슬롯 라벨. SlotCropRecord.EmptyLabel과 같다.
EMPTY_LABEL = "empty"

# NFR-04: 정확도와 불량 재현율이 각각 이 값 이상이어야 ML 판정을 쓴다.
TARGET_METRIC = 0.90
