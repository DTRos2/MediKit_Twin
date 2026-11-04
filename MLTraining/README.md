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
- **구성품 자세**: 원기둥(소독제, 붕대 등)은 85%가 옆으로 눕는다(`_itemLyingChance`). 실제 키팅에서 센 값이다. 두꺼운 판과 상자(거즈, 장갑)는 서 있는 자세에서 ±20°만 돈다.
- **얇은 판과 가는 막대의 자세** (`ItemPosePlanner`): 얇은 판(시험 카세트, 샘플 백, 알코올 패드)은 한쪽 모서리를 들어 최대 25° 기울고(`_itemTiltDegrees`) 20%는 긴 모서리로 서며(`_itemEdgeStandChance`), 가는 막대(면봉, 바늘)는 최대 25° 기운다. 이 둘은 제자리에서 ±90°까지 돈다(`_itemFlatYawDegrees`). 실제 키팅에서 이런 물건이 칸막이에 기대 비스듬히 놓이거나 가로로 돌아 놓였다(2026-11-04 DX 키트).
- **구성품 크기**: 선반·그리퍼 기준 크기 상한(`GraspSizeLimit`)을 반영한 현재 크기.

학습 결과의 NFR-04는 합성 테스트 세트의 값이다. 이 점수가 좋아도 씬에서 틀릴 수 있으므로, 실제 씬(`InspectionCaptures`의 크롭, `InspectionBenchmark`)에서도 맞는지 따로 확인한 뒤 모델을 설치한다.

### 2-1. 실제 씬 크롭 섞기
합성 이미지만으로는 실제 씬의 흐릿한 구성품(칸 바닥과 색이 비슷한 면봉)을 읽지 못했다. 그래서 `InspectionStation`의 Save Captures로 저장한 실제 크롭(`InspectionCaptures`)에 정답 라벨을 붙여 합성 데이터와 함께 학습한다.
1. `real_lots.json`에 세션 폴더 이름과 로트별 키트 코드(BL, DX, EM)를 적는다.
2. 학습용 폴더를 만든다. 평가에만 쓸 세션은 `--test-sessions`로 따로 준다.
   ```
   python build_real_set.py --out ..\Datasets\RealCaptures\v1 --train-sessions <세션> ... --test-sessions <세션>
   ```
   정답은 레시피의 슬롯별 구성품이다. 단 기준 모델이 `empty`로 0.9 이상 읽은 슬롯은 `empty`로 둔다(불량 주입 누락). 사람이 이미지로 확인한 라벨이 아니다.
3. 학습에 섞는다(`--real`은 train 분할을 `--real-repeat`번 반복해 넣고, `--real-test`는 평가에만 쓴다).
   ```
   python train.py --dataset ..\Datasets\<합성 폴더> --real ..\Datasets\RealCaptures\v1 --real-repeat 8 --real-test ..\Datasets\RealCaptures\v1
   ```
`Datasets/`와 `InspectionCaptures/`는 Git에 올리지 않는다. 다른 PC에서 같은 학습을 하려면 캡처 폴더를 따로 옮겨야 한다.

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

### 5-0. 2026-11-04 오후 모델 (현재 설치된 모델)
모델 `runs/dx_real_0004`(합성 데이터 `Datasets/InspectionSynthetic/20261104_154104`와 실제 크롭 140장 x 8)가 설치되어 있다. DX 정상 키트가 계속 불량으로 판정되던 문제를 고친 것이다. 원인과 과정은 [트러블슈팅 FR-07 DX 문서](../docs/troubleShooting/FR-07_DX_키트가_계속_불량으로_판정된다_기울어진_자세와_흐릿한_면봉을_학습하지_못했다.md)에 있다.

| 평가 | 결과 |
|---|---|
| 합성 테스트 박스 정확도 / 불량 재현율 / 정상 오경보 | 0.955 / 0.92 / 0.01 (NFR-04 통과, 재현율 여유가 얇다) |
| 학습에 쓰지 않은 세션의 실제 크롭 13개 로트 (`dx_real_0003`) | 13/13 통과 |
| 에디터 Play 실행 (설치 후, 작성자 확인) | 전부 정상 판정 |

- 실제 크롭의 라벨은 레시피 기준이고 `empty` 슬롯은 모델이 정했다. 사람이 이미지로 확인하지 않았다.
- 합성 테스트의 남은 오류는 `test_cassette`와 `gauze`, `swab`과 `empty`, `needle`과 `sample_bag` 사이의 혼동이다.

### 5-1. 2026-11-04 오전 재학습 모델 (이전 모델)
모델 `runs/yaw_fixed_0030`(데이터 `Datasets/InspectionSynthetic/20261104_002012`)이 `Assets/_Project/Models/Inspection/slot_cnn.onnx`에 설치되어 있었다(오후에 5-0의 모델로 교체). 이전 모델은 실제 키팅 결과(그래픽 켠 전체 공정의 키트 3개)를 모두 불량으로 읽었다. 원인과 고친 과정은 [트러블슈팅 FR-07 3장](../docs/troubleShooting/FR-07_비전_검사_CNN_도입_중_만난_문제들.md)에 있다.

| 평가 | 이전 모델 (`20261103-111741`) | 오전 모델 |
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
