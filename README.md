# MediKit Twin

의료 키트 포장 공정을 Unity에서 재현하는 디지털 트윈 시뮬레이션이다. 주문 CSV를 읽어 키트(EM, BL, DX)를 만들고, 로봇이 자재 선반에서 구성품을 집어 키트 박스에 담은 뒤 검사, 뚜껑 덮기, 라벨 부착, 하역까지 한 셀 안에서 처리한다. 검사에는 슬롯 크롭 CNN을 사용한다.

팀 AURIX Robotics(2인), 요구사항 기준 문서는 [docs/AURIX_SRS.docx](docs/AURIX_SRS.docx)이다. 코드와 문서의 `FR-xx`, `NFR-xx`는 이 SRS의 요구사항 ID이다.

## 공정 흐름

```
주문(CSV) → 키팅(로봇암 A) → 컨베이어 → 검사(CNN) ─정상→ 팩킹(로봇암 B, 뚜껑) → 라벨 → 컨베이어 끝 → 하역(AGV) → 하역장
                                              └─불량→ 푸셔 → 불량 박스(RejectBin)
```

| 공정 | 하는 일 | 주요 코드 |
|---|---|---|
| 주문 | CSV(순번, 병원명, 키트 코드, 수량)를 읽어 키트 단위로 풀고, 재고가 모자라면 주문을 보류한다 | `Scripts/Orders`, `Scripts/Recipes` |
| 재고 | 자재 선반 12칸(A~C행 × 1~4열)의 재고를 센다 | `Scripts/Inventory` |
| 키팅 | 이동 베이스 위의 로봇암 A(FR5)가 선반에서 집어 키트 박스 슬롯에 놓는다 | `Scripts/ArmA`, `Scripts/Mobile` |
| 컨베이어 | 박스를 검사, 팩킹, 끝 구간으로 옮긴다 | `Scripts/Conveyor` |
| 검사 | 슬롯별 크롭을 CNN으로 분류해 정상/불량을 판정한다(규칙 판정도 있다) | `Scripts/Inspection`, `Scripts/ML` |
| 푸셔 | 불량 박스를 밀어 불량 박스에 넣는다 | `Scripts/Pusher` |
| 팩킹 | 로봇암 B(UR3)가 뚜껑을 집어 덮는다 | `Scripts/ArmB` |
| 라벨 | 뚜껑에 키트 라벨을 붙인다 | `Scripts/Label` |
| 하역 | AGV가 컨베이어 끝의 박스를 집어 하역장의 키트별 구역에 내려놓는다 | `Scripts/Unloader` |
| 대시보드 | 공정 현황, 주문, 재고, 로그, 배속, 카메라 패널 | `Scripts/Dashboard`, `Scripts/Demo` |

하역장은 키트 종류마다 구역이 하나씩 있고(EM, BL, DX), 구역은 칸막이로 나뉘며 줄 끝 푯말에 키트 코드가 적혀 있다. 구역 구성은 `Tools > MediKit > Setup Unloading Dock Platform`이 만든다.

## 요구 환경

| 항목 | 값 |
|---|---|
| Unity | 6000.6.0f1 (`ProjectSettings/ProjectVersion.txt`, 팀 전원 같은 버전) |
| 렌더 파이프라인 | URP 17.6 |
| 입력 | Input System 1.20 (새 Input System 전용) |
| 추론 | `com.unity.ai.inference` 2.6.1 (Sentis, ONNX) |
| 로봇 | URDF Importer v0.5.2, ROS-TCP-Connector (선택) |
| CNN 학습 | Python 3.9~3.13, PyTorch ([MLTraining/README.md](MLTraining/README.md)) |
| ROS (선택) | Ubuntu 22.04, ROS 2 Humble ([medikit_ws/README.md](medikit_ws/README.md)) |

## 시작하기

```bash
git clone https://github.com/DTRos2/MediKit_Twin.git
cd MediKit_Twin
git config core.hooksPath .githooks   # 커밋 규칙 자동 검사
git checkout develop
```

1. Unity Hub에서 이 폴더를 연다(위 에디터 버전). 처음 열 때 패키지를 받느라 시간이 걸린다.
2. `Assets/_Project/Scenes/Scene_Cell.unity`를 연다. 공정 전체가 이 씬 하나에 있다.
3. Play를 누른다. 주문은 `Assets/StreamingAssets/Orders/orders.csv`를 읽는다.
4. 대시보드의 주문 패널에서 주문을 바꿔 적용할 수도 있다(시험용 편의).

주문 CSV 형식은 첫 줄이 머리글이고 열은 `순번,병원명,키트 코드,수량`이다.

```csv
순번,병원명,키트 코드,수량
1,A병원,EM,2
2,B병원,DX,3
3,C병원,BL,4
```

시나리오(누락 확률, 고정 누락 키트, 설비 실패 키트, 난수 시드, 재고)는 `Assets/_Project/Config/ScenarioConfig_Default.asset`에서 바꾼다. 같은 시드와 같은 주문 파일은 같은 결과를 낸다(NFR-06).

## 폴더 구조

```
Assets/_Project/
├── Scenes/        Scene_Cell.unity (셀 전체)
├── Prefabs/       로봇, 검사/라벨 스테이션, 푸셔, 하역장 등
├── Scripts/       런타임 코드 (모듈별 어셈블리 정의 MediKitTwin.*)
│   ├── Common     공통 인터페이스, CellEvents, KitData
│   ├── Recipes    키트 레시피 (RecipeSO)
│   ├── Orders     주문 읽기, 검증, 보류
│   ├── Inventory  선반 재고
│   ├── ArmA       키팅 로봇 (IK, 집기, 놓기, 베이스 접근)
│   ├── ArmB       뚜껑 덮기
│   ├── Mobile     이동 베이스
│   ├── Conveyor   컨베이어와 박스 인계
│   ├── Inspection 검사 카메라, 슬롯 크롭, CNN, 규칙 판정
│   ├── ML         합성 데이터셋 생성
│   ├── Pusher     불량 배출
│   ├── Label      라벨
│   ├── Unloader   하역 AGV, 하역장
│   ├── Dashboard  대시보드 공통 (색, 글꼴, 지표)
│   ├── Demo       대시보드 패널과 시연 도구
│   ├── CameraRig  셀 카메라
│   ├── Rl         강화학습 도달 환경 (선택)
│   └── Ros        ROS 연동 (선택)
├── Editor/        씬/프리팹 구성 도구, 측정 도구, 합성 데이터 도구
├── Config/        로봇 프로필, 시나리오, 선반 배치, 학습 랜덤화 설정
├── Recipes/       Recipe_EM, Recipe_BL, Recipe_DX
├── Models/        검사 CNN(ONNX)
└── Tests/         EditMode, PlayMode 시험
MLTraining/        CNN 학습 스크립트 (Assets 밖)
medikit_ws/        ROS 2 워크스페이스 (선택)
docs/              SRS, 인터페이스 정의서, 트러블슈팅, 도표
```

`Datasets/`, `InspectionCaptures/`는 Git에 올리지 않는다. `InspectionBenchmark/`는 검사 벤치마크 결과(CSV)이다.

## 설계 규칙

- 설비끼리 직접 참조하지 않고 `CellEvents`의 신호(`KitReady`, `AtInspection`, `InspectionResult`, `KitShipped` 등)로만 주고받는다. 신호 목록은 [docs/INTERFACES.md](docs/INTERFACES.md)에 있다.
- `Common`은 ROS를 알지 못한다. `Ros` 코드가 `Common`을 쓰는 한 방향만 허용하며, 어셈블리 정의로 강제한다.
- 시간은 모두 시뮬레이션 시간(`SimClock`, 배속 반영)이다. 예외는 ROS 발행 주기와 설비 주기 통지다.
- 키트 판정 결과의 출처는 `KitData.Verdict` 하나이다.

## 검사 CNN

슬롯 크롭 하나를 빈 슬롯 또는 구성품 12종으로 분류하고, 슬롯 판정을 박스 단위로 올려 정상/불량을 정한다. 합성 데이터셋은 `Tools > MediKit > Synthetic Dataset (Inspection)`으로 만들고, `MLTraining/train.py`로 학습해 ONNX를 `Assets/_Project/Models/Inspection/`에 넣는다. NFR-04 기준은 박스 정확도와 불량 재현율이 모두 0.90 이상이다. 환경 설정, 데이터 구성, 현재 모델의 측정 결과는 [MLTraining/README.md](MLTraining/README.md)를 본다.

## ROS 2 연동 (선택)

로봇암 A는 Unity 단독(IK) 또는 ROS 2(`/arm_a/joint_command` 관절 목표 각도 수신, `/arm_a/joint_states` 발행)로 구동한다. ROS 제어 중에는 배속이 1배로 고정된다. 워크스페이스 빌드와 실행은 [medikit_ws/README.md](medikit_ws/README.md)를 본다. Unity 쪽은 `Robotics > ROS Settings`에서 Protocol ROS2, 포트 7777로 맞춘다.

## 에디터 도구

`Tools > MediKit` 메뉴에 씬 구성과 측정 도구가 있다. 씬을 직접 고치지 않고 이 도구로 배치를 다시 맞춘다. 실행해도 씬은 저장되지 않으므로 확인 후 Ctrl+S는 직접 한다.

| 분류 | 메뉴(예) |
|---|---|
| 씬 구성 | Build Cell Scene, Setup Kitting Station, Setup Conveyor Handoff, Setup Pusher Prefab, Setup Label Station, Setup Unloading Dock Platform, Setup AGV Pick Platform |
| 프리팹 재생성 | Rebuild KittingJig / LidSupplyTable / RejectBin And Shelf / UnloaderRobot Prefab, Rebuild Dashboard Panel Prefabs |
| 측정 | Measure AGV Layout, Measure AGV Pick Reach, Measure ArmB Reach / Workspace (3D) / Gripper Gap, Measure Pusher Layout |
| 데이터 | Synthetic Dataset (Inspection) |

로봇 프로필 생성은 `Tools > Arm`, URDF 재질/메시 변환은 `Tools > URDF` 메뉴에 있다.

## 시험

`Window > General > Test Runner`에서 EditMode와 PlayMode 시험을 돌린다. 시험은 모듈별 어셈블리(`MediKitTwin.*.Tests`)로 나뉘어 있고, 전체 공정을 도는 시험은 PlayMode(`FullProcessTests`, `ReapplyOrderProcessTests` 등)에 있다.

## 협업 규칙

- 브랜치: `main`(안정), `develop`(통합), `feature/<이니셜>-<기능>`. `main`과 `develop`에는 직접 커밋하지 않고 PR로만 머지한다.
- 커밋: `타입: 한국어 명사형 제목`, 범위는 공정 이름이나 요구사항 ID(`feat(fr-12): ...`). 훅이 형식을 검사한다.
- 씬(`.unity`) 변경은 다른 변경과 섞지 않고 단독 커밋으로 만들며, 기능은 프리팹으로 만들어 씬에는 인스턴스만 둔다.
- 자세한 내용은 [CONVENTION.md](CONVENTION.md), [COMMIT_RULES.md](COMMIT_RULES.md)를 본다.

## 문서

| 문서 | 내용 |
|---|---|
| [docs/INTERFACES.md](docs/INTERFACES.md) | 공통 인터페이스(CellEvents, KitData, RecipeSO, ROS 토픽) |
| [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | 만난 문제와 원인, 해결 (상세 기록은 `docs/troubleShooting/`) |
| [MLTraining/README.md](MLTraining/README.md) | CNN 학습과 성능 측정 |
| [medikit_ws/README.md](medikit_ws/README.md) | ROS 2 워크스페이스 |
| [CONVENTION.md](CONVENTION.md) | 협업, 씬, 코딩 컨벤션 |
