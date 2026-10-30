# MediKit_Twin 협업 컨벤션

> Unity · GitHub (2인 팀) · 요구사항 기준 문서: [docs/AURIX_SRS.docx](docs/AURIX_SRS.docx)
> 커밋 메시지 규칙은 [COMMIT_RULES.md](COMMIT_RULES.md) 참고

---

## 0. 최초 1회 설정 (clone 직후)

```bash
git clone https://github.com/DTRos2/MediKit_Twin.git
cd MediKit_Twin
git config core.hooksPath .githooks   # 커밋 규칙 자동 검사 활성화
git checkout develop
```

- Unity 에디터 버전은 **팀 전원 동일 버전**(`ProjectSettings/ProjectVersion.txt` 기준)을 사용한다. 버전 업은 `chore:` 커밋으로 한 사람이 올리고 공유한다.
- `Edit → Project Settings → Editor` 에서 아래 두 값이 바뀌어 있지 않은지 확인한다.
  - Version Control Mode: **Visible Meta Files**
  - Asset Serialization Mode: **Force Text**
- Git LFS는 사용하지 않는다. 대용량 바이너리가 늘어나면 팀원과 상의해 도입한다.

---

## 1. 브랜치 구조

| 브랜치 | 역할 | 직접 커밋 |
|---|---|---|
| `main` | 항상 실행 가능한 안정 버전 | ❌ 금지 — develop에서만 머지 |
| `develop` | 팀 통합 브랜치, 매일 작업의 기준 | ❌ 금지 — PR로만 머지 |
| `feature/<이니셜>-<기능>` | 1인 1기능 작업 공간 | ✅ |

### feature 브랜치 이름 규칙

```
feature/<이니셜>-<기능>
```

- 영문 소문자, 숫자, 하이픈(`-`)만 사용한다. `<이니셜>`은 각자 정해 쓰는 작업자 식별자다.
- 이니셜만 쓰지 않고 **기능까지** 적는다. (예: `feature/<이니셜>-conveyor`, `feature/<이니셜>-stock-refill`)
- 대문자, 언더스코어, `feature/` 접두어 누락은 `pre-commit` 훅이 차단한다.

---

## 2. 작업 흐름

```bash
# 1. develop 최신화 후 브랜치 생성
git checkout develop
git pull origin develop
git checkout -b feature/<이니셜>-<기능>

# 2. 작업 · 짧게 끊어서 자주 커밋
git add .
git commit -m "feat(conveyor): 구역 점유 인터록 추가"

# 3. PR 올리기 전 develop 최신 변경 반영
git pull origin develop

# 4. 푸시 후 GitHub에서 develop 대상으로 PR 생성
git push -u origin feature/<이니셜>-<기능>
```

5. 일정이 짧으므로 리뷰는 가볍게 한다. **머지 전에 상대에게 PR을 알리고**, 상대가 확인하면 머지한다. 머지된 feature 브랜치는 삭제한다.
6. develop이 안정화되고 **실행이 확인되면** develop → main PR을 올리고 태그를 단다: `git tag v0.1.0 && git push origin v0.1.0`

---

## 3. 씬(Scene) 충돌 방지 규칙

- 씬 파일(`.unity`)은 Git이 자동 병합하지 못하는 경우가 많다. **씬 담당자는 1명**으로 정하고, 나머지는 기능을 **프리팹**으로 만들어 씬에는 인스턴스만 배치한다.
- 같은 씬을 건드려야 하면 작업 전에 상대에게 알리고, 짧게 작업한 뒤 바로 커밋·머지한다.
- 씬 변경은 다른 변경과 섞지 않고 **단독 커밋**으로 만든다.
- 프리팹은 **Prefab Mode**에서 수정한다. 씬에서 수정했다면 **Overrides → Apply All**을 누른다. (인스턴스마다 달라야 하는 위치·회전만 씬에 남긴다.)
- 충돌 시 UnityYAMLMerge(Smart Merge)를 사용한다.

---

## 4. Unity × Git 기본 규칙

- **`.meta` 파일은 반드시 원본 에셋과 같이 커밋한다.** 에셋 이동·이름 변경은 **Unity Project 창 안에서** 한다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`은 커밋하지 않는다. (`.gitignore` 처리됨)
- 커밋 전 Unity에서 **File → Save (Ctrl+S)** 로 씬·프리팹을 저장했는지 확인한다.
- Console에 **에러가 있는 상태로 PR을 올리지 않는다.**
- 시크릿(API 키 등)은 `.env`에 두고 커밋하지 않는다.
- `docs/`의 docx, pptx, xlsx는 병합이 불가능하다. **한 번에 한 명만 수정**하고, 수정 전에 상대에게 알린다.

---

## 5. 요구사항 관리 (SRS 연동)

- 구현과 테스트는 SRS의 요구사항 ID(`FR-xx`, `NFR-xx`)와 테스트 케이스 ID(`TC-xx`)를 기준으로 한다. PR 템플릿에 관련 ID를 적는다.
- 요구사항이 바뀌면 SRS 개정 이력, 한 장 기획표, 공정 흐름도를 **함께** 고치고 `docs:` 커밋으로 남긴다. (SRS 8장 변경 관리)
- 선택 확장(ROS 2, ML)의 범위 축소도 개정 이력에 기록한다.

---

## 6. C# 코딩 컨벤션

### 6-1. 네이밍

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스, 구조체, enum, 메서드, 프로퍼티 | PascalCase | `ArmController`, `ResetStation()` |
| 인터페이스 | `I` + PascalCase | `IStation` |
| private 필드 | `_camelCase` | `_moveSpeed` |
| 지역 변수, 파라미터 | camelCase | `targetSlot` |
| 상수 | PascalCase | `RejectBinCapacity` |
| 이벤트(신호) | SRS 4.4의 이름 그대로 | `KitReady`, `AtInspection`, `LidClosed` |
| ScriptableObject | 접미사 `SO` | `RecipeSO` |

- 파일 하나에 클래스 하나를 두고, **파일명은 클래스명과 같게** 한다.
- 네임스페이스는 `MediKitTwin.<영역>` 형식을 쓴다. (예: `MediKitTwin.Conveyor`, `MediKitTwin.Inspection`)
- 불리언은 `is/has/can`으로 시작한다.
- 설비 사이의 신호는 **`CellEvents` 이벤트 버스로만** 주고받고, 설비끼리 직접 참조하지 않는다.
- 의존 방향은 어셈블리 정의(asmdef)로 강제한다. `MediKitTwin.Recipes`는 아무것도 참조하지 않고, `MediKitTwin.Common`은 `Recipes`만 참조한다. `Ros`는 `Common`만 참조하고, `Common`은 `Ros`를 참조하지 않는다. 새 폴더(Orders, ArmA 등)에 asmdef를 만들 때는 `Common`만 참조한다.

### 6-2. 작성 규칙

- 인스펙터 노출은 `public` 필드 대신 **`[SerializeField] private`** 을 쓴다. 필요하면 `[Header]`, `[Tooltip]`, `[Range]`를 붙인다.
- 중괄호는 Allman 스타일(새 줄), 들여쓰기는 스페이스 4칸.
- **수치는 코드에 직접 쓰지 않는다.** 누락 확률(10%), 고정 누락 순번(3), 불량 박스 정원(5), 보충 대기 시간(30초), 보충 수량(20) 등은 인스펙터 필드나 SO로 둔다.
- 주석은 "무엇"이 아니라 **"왜"** 를 적는다. 주석 처리된 죽은 코드는 커밋하지 않는다.
- 커밋 전 불필요한 `Debug.Log`를 제거한다.

### 6-3. 시뮬레이션 규칙

- **난수는 시드를 받는 `System.Random` 인스턴스**로 만든다. 같은 시드면 같은 키트에서 누락이 발생해야 한다. (FR-04, NFR-06) `UnityEngine.Random` 전역 상태는 누락 규칙에 쓰지 않는다.
- 시간은 `SimClock.Now`(시작·완료 시각, 보충 대기, 타임아웃, 로그 시각)와 `Time.deltaTime`(프레임 이동량)처럼 **시뮬레이션 시간** 기준으로 계산한다. 배속(0.5, 1, 2, 4배)이 로봇, 컨베이어, 하역 로봇, 대시보드 지표에 모두 적용되어야 한다. (FR-15) 실제 시간(`DateTime`, `Time.unscaledTime`)은 지표 계산에 쓰지 않는다. 예외는 ROS 토픽 발행 주기와 설비 주기 통지(`StationAlive`)뿐이다. ROS 제어 모드에서는 `SimClock.SetSpeedLocked(true)`로 1배속에 고정한다.
- **검사 판정 코드는 누락 정답 라벨을 읽지 않는다.** 정답 라벨은 판정이 끝난 뒤 로그 비교에만 쓴다. (FR-06)
- 키트 종류는 `RecipeSO`로만 구분한다. 키트를 추가할 때 스크립트를 고치지 않고 레시피 파일 1개만 추가해야 한다. (NFR-07)
- 설비 상태는 `가동 / 대기 / 정지` 중 하나로 통일하고, 상태가 바뀔 때 `StationStateChanged`를 보내고, 상태가 같아도 1초마다 `StationAlive`를 보낸다. (`StationHeartbeat`, FR-14) 로거와 주문 관리는 `StationStateChanged`만 구독한다.
- 생산 로그 CSV는 실행마다 새 파일로 저장한다. (덮어쓰지 않음) 설비는 로그 파일을 직접 다루지 않고, `CellEvents`를 구독하는 로거(`IProductionLog`)가 기록한다.
- 키트 코드는 대문자 `string`(EM, BL, DX)이다. 재고는 `IStock`, 주문 현황은 `IOrderStatus`, 다음 키트는 `IKitSource`, 박스는 `IBoxSupply`, 판정기는 `IInspector`로만 접근하고 구체 클래스를 직접 참조하지 않는다.

### 6-4. Unity 메시지 메서드 순서

`Awake` → `OnEnable` → `Start` → `Update` → `FixedUpdate` → `LateUpdate` → `OnDisable` → `OnDestroy` 순으로 두고, 그 아래에 public → private 메서드를 둔다.

### 6-5. 성능

- `Update` 안에서 `GetComponent`, `Find*`, `Camera.main`을 호출하지 않는다. `Awake`/`Start`에서 캐싱한다.
- 매 프레임 `new`(배열, 리스트)나 문자열 연결을 하지 않는다.
- 태그 비교는 `CompareTag`를 쓴다.
- 1배속 평균 30 FPS 이상(NFR-01)을 지킨다. 4배속에서 FPS가 20 미만이면 경고를 표시한다.

---

## 7. ML 파이프라인 규칙

학습은 Unity 밖(PyTorch)에서, 추론은 Unity 안(Sentis)에서 한다.

```
Unity 데이터 생성기(FR-08) → PNG + labels.csv → PyTorch 학습 → ONNX → Unity Sentis 추론(FR-07)
```

- 파이썬 학습 코드는 `ml/` 폴더에 둔다. 패키지는 `ml/requirements.txt`에 버전과 함께 적는다.
- 합성 데이터셋(이미지)은 용량이 크므로 **커밋하지 않는다.** `ml/data/`는 `.gitignore` 처리하고, 생성 설정(장수, 랜덤 범위, 시드)을 문서나 설정 파일로 남겨 다시 만들 수 있게 한다.
- 데이터는 학습, 검증, 테스트 세트로 분리하고, **테스트 세트는 정상·누락을 반반**으로 만든다. 클래스는 정상, 누락 2개다.
- 학습·검증·테스트에 쓰는 시드와 하이퍼파라미터는 코드나 설정 파일에 기록한다.
- 합격 기준은 정확도 90% 이상 그리고 누락 재현율 90% 이상이다. (NFR-04) 측정 결과는 `docs/`나 PR에 남긴다.
- 학습이 끝난 모델(`.onnx`)만 `Assets/_Project/ML/Models/`에 두고 `train:` 커밋으로 올린다.
- 모델 로드나 추론이 실패하면 규칙 판정(FR-06)으로 대체한다. 11/02까지 기준에 미달하면 규칙 판정을 유지한다.

---

## 8. 폴더 구조와 에셋 네이밍

```
Assets/
├─ _Project/
│  ├─ Scripts/{Orders, Recipes, ArmA, ArmB, Conveyor, Inspection, Pusher, Label, Inventory, Unloader, Logging, Dashboard, CameraRig, ML, Rl, Mobile, Demo, Ros, Common}
│  ├─ Recipes/          # RecipeSO 파일
│  ├─ Config/           # 설정 에셋: ScenarioConfigSO(시드, 누락 확률, 재고), ShelfLayoutConfigSO(선반 치수, 배치, 위치, 그리퍼 열림 폭, 로봇암 도달 반경), ArmBaseApproachConfigSO(베이스가 서는 거리와 이동 정밀도), ShelfItemLooksSO(구성품 모양), ArmProfileSO
│  ├─ Prefabs/
│  ├─ Scenes/
│  ├─ Materials/
│  ├─ Models/
│  └─ ML/Models/        # .onnx
├─ StreamingAssets/Orders/   # 주문 파일 (FR-01, 빌드에서도 읽기 위해)
├─ Resources/                # ROSConnectionPrefab (ROS-TCP-Connector가 생성)
└─ (서드파티 에셋은 _Project 밖에 둔다)
ml/                    # 파이썬 학습 코드 (데이터셋은 커밋하지 않음)
medikit_ws/           # ROS 2 워크스페이스 (colcon)
docs/                  # SRS, 기획표, WBS, 공정 흐름도
```

| Scripts 폴더 | 내용 | 요구사항 |
|---|---|---|
| `Orders` | 주문 파일 읽기와 교체(`OrderSession`), 주문 대기열, 보류·재개 | FR-01, FR-13, FR-16 |
| `Recipes` | `RecipeSO`와 레시피 데이터 타입 | FR-02 |
| `ArmA` | 피킹·키팅, 누락 발생 규칙, 관절 구동·IK·그리퍼(`ArmController`, `IkArmTarget`, `JointGripper`) | FR-03, FR-04 |
| `Conveyor` | 구역 이송, 구역 점유 | FR-05 |
| `Inspection` | 슬롯 센서 규칙 판정 (`IInspector`) | FR-06 |
| `ML` | Sentis 추론, 합성 데이터 생성기 | FR-07, FR-08 |
| `Pusher` | 불량 배출, 불량 박스 | FR-09 |
| `ArmB` | 뚜껑 덮기 | FR-10 |
| `Label` | 병원명·로트 라벨 표시 | FR-11 |
| `Unloader` | 하역 로봇, AGV 경로 | FR-12 |
| `Inventory` | 선반 재고, 보충 (`IStock`) | FR-13 |
| `Logging` | `IProductionLog` 구현, 생산 로그 CSV | FR-14 |
| `Dashboard` | 지표 표시, 배속 UI | FR-14, FR-15 |
| `CameraRig` | 마우스 오른쪽 버튼 카메라 조작 (Input System 사용) | - |
| `Mobile` | 바퀴·궤도 위에서 굴러가는 베이스의 이동(`MobileBaseDriver`)과 로봇암 얹기(`MountedBaseFollower`). `Common`만 참조 | - |
| `Demo` | 화면 시험 패널(`ShelfStockDemoPanel`, `OrderCsvDemoPanel`, `KittingStatusDemoPanel`)과 임시 장치(`ConveyorHandoffStub`). 패널은 공통 뼈대 `DemoPanel`을 상속하고, Inspector의 `Anchor`와 `Margin`으로 화면 왼쪽과 오른쪽에 나눠 붙이며 `Is Visible`로 끈다. 제품 코드와 분리해 빌드에서 뺄 수 있다. `Common`, `ArmA`, `Inventory`, `Orders` 참조 | - |
| `Rl` | 강화학습 환경(관측, 행동 해석, 보상, 목표 뽑기). `Common` 인터페이스만 본다 | - |
| `Ros` | 관절 명령 구독, 상태 발행, 정책 노드 연동(`RosPolicy`) | FR-17, FR-18 |
| `Common` | `CellEvents`, `SimClock`, 공통 인터페이스 | - |

- 각 Scripts 폴더에는 같은 이름의 asmdef(`MediKitTwin.<폴더명>`)가 있다. `Recipes`, `Common`, `CameraRig`를 뺀 나머지는 `Common`만 참조한다. `CameraRig`는 `Unity.InputSystem`만 참조한다. (6-1)
- ROS 2 노드는 `medikit_ws/`, Unity 쪽 ROS 코드는 `Assets/_Project/Scripts/Ros/`에 둔다.

| 에셋 | 접두사 | 예시 |
|---|---|---|
| Prefab | `PF_` | `PF_KitBox` |
| Material | `M_` | `M_BoxRed` |
| Texture | `T_` | `T_Floor_Albedo` |
| ScriptableObject | `SO_` 또는 `*SO` 접미사 | `Recipe_EM` |
| Scene | `Scene_` | `Scene_Cell` |

- 폴더·에셋 이름에 공백과 한글을 쓰지 않는다. 키트 코드는 SRS와 같이 `EM`, `BL`, `DX`를 쓴다.

---

## 9. 체크리스트

작업 시작 전, 커밋 전에 한 번씩 확인한다.

- [ ] `main` / `develop`에 직접 커밋하지 않았는가?
- [ ] `develop`에서 `feature/<이니셜>-<기능>` 브랜치를 새로 따서 작업했는가?
- [ ] 기능은 가능하면 프리팹으로 만들었는가?
- [ ] 프리팹은 Prefab Mode에서 수정했는가? (또는 Overrides → Apply All)
- [ ] 같은 씬이나 문서 파일을 건드렸다면 상대에게 알렸는가?
- [ ] 작업을 짧게 끊어서 커밋했는가?
- [ ] `.meta` 파일이 같이 커밋되었는가?
- [ ] 수치를 코드에 직접 쓰지 않고 인스펙터나 SO로 뺐는가?
- [ ] 불필요한 `Debug.Log`와 죽은 코드를 지웠는가?
- [ ] PR 올리기 전 `git pull origin develop`으로 최신 변경을 받아왔는가?
