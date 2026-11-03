# 슬롯 크롭 CNN 학습 (FR-07, FR-08, NFR-04)

Unity의 합성 데이터셋으로 슬롯 크롭 하나를 "빈 슬롯 또는 구성품 12종" 중 하나로 분류하는 CNN을 학습하고,
Unity Sentis(`com.unity.ai.inference`)가 읽을 ONNX로 내보낸다. 이 폴더는 `Assets` 밖에 있어 Unity가 불러오지 않는다.

## 1. 환경 (Windows)
1. Python 3.12 설치 (PyTorch가 지원하는 3.9~3.13 중 하나면 된다).
2. 가상환경과 PyTorch:
   ```
   cd MLTraining
   python -m venv .venv
   .venv\Scripts\activate
   # GPU가 RTX 50 시리즈(예: RTX 5060)이면 CUDA 12.8 빌드가 필요하다:
   pip install torch --index-url https://download.pytorch.org/whl/cu128
   # GPU를 쓰지 않으면: pip install torch
   pip install -r requirements.txt
   ```
3. 장치 확인: `python -c "import torch; print(torch.cuda.is_available())"` (데이터가 작아 CPU로도 학습할 수 있다.)

## 2. 데이터셋
Unity 메뉴 `Tools > MediKit > Synthetic Dataset (Inspection)`으로 만든다. 출력은 프로젝트 루트의 `Datasets/<실행 폴더>/`이며
`labels.csv`와 `train/`, `validation/`, `test/` 크롭 PNG가 들어 있다. 기본값(학습 800, 검증 100, 테스트 200 박스)은 크롭 약 4,400장이다.
조명 색은 흰색부터 씬의 따뜻한 조명까지 흔든다. 씬의 조명이 바뀌면 데이터셋을 다시 만들어 재학습한다. 학습 결과의 NFR-04는 합성 테스트 세트의 값이므로, 실제 씬(`InspectionCaptures`)에서도 맞는지는 따로 확인한다.

## 3. 학습
```
python train.py --dataset ..\Datasets\<실행 폴더>
```
- 학습 후 같은 스크립트가 테스트 세트로 NFR-04를 계산한다. 슬롯 분류를 박스 단위로 올려서(슬롯 하나라도 레시피와 다르면 불량)
  **박스 정확도**와 **불량 재현율**을 구하며, 둘 다 0.90 이상이면 통과이다.
- 결과는 `runs/<시각>/`에 저장한다: `slot_cnn.onnx`, `classes.json`, `metrics.json`, `best.pt`.

## 4. Unity에 넣기
`runs/<시각>/`의 `slot_cnn.onnx`와 `classes.json`을 `Assets/_Project/Models/Inspection/`로 복사한다. `PF_InspectionStation`의 `CnnInspector`가 이 두 파일을 참조한다(Model, Classes). 같은 학습 결과의 두 파일을 함께 바꾼다.

## 입력 규격 (Unity와 같아야 한다)
- 입력 `input`: `N x 3 x 72 x 128`, RGB, 0~1 실수. 정규화는 모델 안에서 한다.
- 출력 `logits`: `N x 클래스 수`. 번호는 `classes.json`의 `classes` 순서이다.
- 슬롯 크롭(약 155 x 87 픽셀)을 128 x 72로 줄여서 넣는다.
