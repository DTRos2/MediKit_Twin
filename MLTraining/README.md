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
Unity 메뉴 `Tools > MediKit > Synthetic Dataset (Inspection)`으로 만든다. 출력은 프로젝트 루트의 `Datasets/InspectionSynthetic/<실행 폴더>/`이며
`labels.csv`와 `train/`, `validation/`, `test/` 크롭 PNG가 들어 있다. 기본값(학습 800, 검증 100, 테스트 200 박스)은 크롭 약 4,400장이다.

창 없이 배치로도 만들 수 있다(촬영에 그리기 장치가 필요하므로 `-nographics`는 쓰지 않는다). 인자를 주지 않으면 창과 같은 기본값을 쓴다.
```
Unity.exe -batchmode -projectPath <프로젝트 경로> -executeMethod SyntheticDatasetBatch.Generate -datasetSeed 20261104
```
(`-datasetOutput`, `-datasetTrain`, `-datasetValidation`, `-datasetTest`로 출력 폴더와 박스 수를 줄 수 있다.)

**합성 이미지는 씬의 촬영 조건에 맞춰 두었다.** 아래가 바뀌면 데이터셋을 다시 만들어 재학습한다.
- **조명**: 씬의 Directional Light와 같은 방향(yaw −30°, pitch 50°, ±25°와 ±10° 흔듦)과 색(흰색부터 씬의 따뜻한 조명까지 흔듦). 환경광·주광·보조광 세기는 `SyntheticLighting.Default`이고, 같은 박스를 씬과 합성에서 찍어 위치별 밝기를 맞춘 값이다. 합성은 환경 반사를 끈 재질을 쓴다. 근거와 측정은 [트러블슈팅 FR-07 3장](../docs/troubleShooting/FR-07_비전_검사_CNN_도입_중_만난_문제들.md)에 있다.
- **배경색**: 박스 밖 색은 검사 카메라와 같은 `InspectionBackground.Color`.
- **구성품 자세**: 원기둥(소독제, 붕대 등)은 85%가 옆으로 눕는다(`_itemLyingChance`). 실제 키팅에서 센 값이다. 판과 상자는 서 있는 자세에서 ±20°만 돈다.
- **구성품 크기**: 선반·그리퍼 기준 크기 상한(`GraspSizeLimit`)을 반영한 현재 크기.

학습 결과의 NFR-04는 합성 테스트 세트의 값이다. 이 점수가 좋아도 씬에서 틀릴 수 있으므로, 실제 씬(`InspectionCaptures`의 크롭, `InspectionBenchmark`)에서도 맞는지 따로 확인한 뒤 모델을 설치한다.

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

## 5. 씬 성능 측정 결과 (NFR-04)

### 5-1. 2026-11-04 재학습 모델 (설치된 모델)
모델 `runs/yaw_fixed_0030`(데이터 `Datasets/InspectionSynthetic/20261104_002012`)이 `Assets/_Project/Models/Inspection/slot_cnn.onnx`에 설치되어 있다. 이전 모델은 실제 키팅 결과(그래픽 켠 전체 공정의 키트 3개)를 모두 불량으로 읽었다. 원인과 고친 과정은 [트러블슈팅 FR-07 3장](../docs/troubleShooting/FR-07_비전_검사_CNN_도입_중_만난_문제들.md)에 있다.

| 평가 | 이전 모델 (`20261103-111741`) | 설치된 모델 |
|---|---|---|
| 합성 테스트 박스 정확도 / 불량 재현율 | NFR-04 통과 | 0.995 / 0.99 (정상 오경보 0) |
| 실제 키팅 크롭 12장(키트 3개) | 4/12 (키트 0/3) | 12/12 (키트 3/3) |
| 샘플 박스 49개(정상·누락, 씬에서 만든 이미지) | 33/49 | 49/49 |
| 씬 `InspectionBenchmark` 박스 60개(불량 30, 흔들림 0) | — | 정확도 1.000, 재현율 1.000, 오경보 0, 규칙 대체 0 |
| 그래픽 켠 전체 공정(`FullProcessTests`) | 키트 3개 모두 불량 | 2개 출하, 1개 불량 격리, 통과 |

- 전부 헤드리스(배치) 실행 결과이고 에디터 화면에서 직접 본 것이 아니다.
- 흔들림 배율 1, 2의 측정은 이 모델로 다시 하지 않았다. 아래 5-2는 이전 모델의 결과다.

### 5-2. 2026-11-03 이전 모델의 흔들림 측정 (기록)
모델 `runs/20261103-111741`을 실제 씬의 검사 구역에서 측정했다. 플레이 모드에서 `InspectionBenchmark`(Demo)의 `Run Benchmark`로 박스 200개(불량 92: 누락과 오구성 절반씩)를 만들어 판정과 정답을 비교한다. 결과는 `InspectionBenchmark/<시각>-j<배율>.csv`에 남는다(git 제외).

| 흔들림 배율 | 박스 정확도 | 불량 재현율 | 정상 오경보율 | 슬롯 위치 일치 | NFR-04 |
|---|---|---|---|---|---|
| 0 (흔들림 없음) | 1.000 | 1.000 | 0.000 | 1.000 | 통과 |
| 1 (학습 범위 끝) | 0.990 | 1.000 | 0.019 | 0.967 | 통과 |
| 2 (학습 범위 두 배) | 0.825 | 0.957 | 0.287 | 0.750 | 미달 |

- 흔들림 배율 1은 학습 랜덤화 범위(박스 ±1cm와 ±5도, 구성품 ±6mm와 ±20도)의 끝까지이다.
- 범위를 넘으면 불량을 놓치기보다 정상 박스를 불량으로 보는 오경보가 늘어난다(배율 2에서 오경보 31건, 놓친 불량 4건).
- 한계: 샘플 박스는 실제 키팅 결과가 아니다. 실제 키팅의 박스와 구성품 위치 편차가 학습 범위를 넘으면 오경보가 늘 수 있다.
  키팅 구현 후 같은 도구로 다시 재고, 필요하면 `SyntheticRandomizationSO`의 위치와 각도 범위를 넓혀 재학습한다.
