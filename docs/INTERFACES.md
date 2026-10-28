# 공통 인터페이스 정의서

> 근거: SRS v1.1 (3장 기능 요구사항, 4.4 통신 인터페이스, 6장 데이터 요구사항)
> 코드: `Assets/_Project/Scripts/Common/` (네임스페이스 `MediKitTwin.Common`)

## 1. 의존 규칙

- 설비끼리 직접 참조하지 않고 `CellEvents`로만 신호를 주고받는다.
- `Common`은 ROS를 알지 못한다. ROS 코드(`Ros/` 폴더)가 `Common`을 사용하는 한 방향만 허용한다.
- 시간은 모두 시뮬레이션 시간(배속 반영)이며 `SimClock`에서 읽는다. 예외는 ROS 토픽 발행 주기(실제 시간 20Hz)와 설비 상태 주기 통지(실제 시간 1초)이다.

## 2. CellEvents

| 신호 | 인자 | 발행 | 구독(예) | 요구사항 |
|---|---|---|---|---|
| `KitReady` | `KitData` | 로봇암 A | 컨베이어 | FR-03 |
| `AtInspection` | `KitData` | 컨베이어 | 검사 | FR-05 |
| `InspectionResult` | `KitData` | 검사 | 컨베이어, 푸셔, 대시보드 | FR-06, FR-07 |
| `RejectBinCleared` | - | 푸셔 | 대시보드 | FR-09 |
| `AtPacking` | `KitData` | 컨베이어 | 로봇암 B | FR-05 |
| `LidClosed` | `KitData` | 로봇암 B(MVP는 자동 처리) | 컨베이어, 라벨 | FR-10, FR-11 |
| `AtConveyorEnd` | `KitData` | 컨베이어 | 하역 로봇 | FR-05 |
| `KitShipped` | `KitData` | 하역 로봇 | 대시보드 | FR-12 |
| `StockEmpty` | 선반 칸 ID, 주문 순번 | 재고 | 주문 관리, 대시보드 | FR-13 |
| `StockRefilled` | 선반 칸 ID | 재고 | 주문 관리, 대시보드 | FR-13 |
| `OrderHeld` | 주문 순번 | 주문 관리 | 대시보드, 로거 | FR-13 |
| `OrderResumed` | 주문 순번 | 주문 관리 | 대시보드, 로거 | FR-13 |
| `StationStateChanged` | `StationId`, `StationState` | 각 설비 | 대시보드 | FR-14 |

`InspectionResult`는 판정 결과를 인자로 싣지 않는다. 검사는 `KitData.Verdict`를 먼저 채운 뒤 신호를 보내고, 구독자는 `kit.Verdict`를 읽는다. 판정 결과의 출처는 `KitData.Verdict` 하나이다. `Verdict`가 비어 있으면 `RaiseInspectionResult`가 신호를 보내지 않고 오류 로그를 남긴다.

`OrderHeld`, `OrderResumed`, `StationStateChanged`는 SRS 4.4의 원래 신호 10개에 없다. 대시보드의 보류 주문 수(FR-13)와 설비 상태(FR-14) 표시에 필요해서 추가했고, SRS 4.4에는 반영했다.

`StockEmpty`와 `OrderHeld`는 함께 발행된다. 재고가 비면 재고(`IStock`)가 `StockEmpty`를 보내고, 주문 관리가 이를 받아 주문을 보류하며 `OrderHeld`를 보낸다. `StockRefilled`도 같다. 보충되면 주문 관리가 보류 주문을 대기열 맨 앞으로 옮기며 `OrderResumed`를 보낸다.

### 선반 칸 ID

`StockEmpty`, `StockRefilled`의 선반 칸 ID와 `RecipeItem.ShelfSlotId`는 같은 형식이다.

- 자재 선반은 3행 4열이다. 행은 키트 종류(A=EM, B=BL, C=DX), 열은 재료 박스(1~4)이며 박스 하나에 재료 하나를 담는다.
- 형식은 `<행><열>` 사이에 `-`를 넣은 `A-1` ~ `C-4`이다. 칸은 모두 12개이다.
- 레시피 항목 순서가 피킹 순서이고, 열 번호는 그 순서와 같다. (예: DX는 C-1 swab, C-2 buffer_vial, C-3 test_cassette, C-4 sample_bag)
- 재고는 칸 단위로 센다. **키트 1개는 칸마다 1개씩 쓴다.** 같은 구성품이 한 키트에 2개 필요한 레시피는 지원하지 않는다.
- 그래서 한 레시피 안에서 구성품 ID, 선반 칸 ID, 슬롯 번호는 각각 겹치지 않는다. 구성품은 최대 4개(열 수)이다.
- 이 규칙은 `RecipeSO.OnValidate`가 검사한다.

| 행 | 키트 | 박스 | 1 | 2 | 3 | 4 |
|---|---|---|---|---|---|---|
| A | EM | L | gauze | bandage | antiseptic | gloves |
| B | BL | M | blood_tube | needle | alcohol_pad | tourniquet |
| C | DX | M | swab | buffer_vial | test_cassette | sample_bag |

구성품 이름은 EM 4종과 DX의 test_cassette만 시나리오서 기준이고 나머지는 임시이다.

## 3. StationState

| 값 | 대시보드 표시 | 의미 |
|---|---|---|
| `Idle` | 대기 | 작업이 없다 |
| `Running` | 가동 | 작업 중이다 |
| `Fault` | 정지 | 목표 자세에 3초 안에 도달하지 못하는 등 오류 |

- "응답 없음"은 설비가 보내는 값이 아니다. 대시보드가 신호를 10초 넘게 받지 못할 때 표시한다. (FR-14)
- **주기 통지:** 각 설비는 상태가 바뀌지 않아도 `StationHeartbeat.IntervalSeconds`(1초)마다 `StationStateChanged`를 같은 값으로 다시 보낸다. 대시보드는 `StationHeartbeat.TimeoutSeconds`(10초) 넘게 받지 못한 설비를 "응답 없음"으로 표시한다.
- 두 값은 실제 시간(`Time.unscaledTime`) 기준이다. 일시정지나 배속 중에도 "응답 없음"으로 오판하지 않기 위해서이다.

## 4. KitData

주문 1건은 수량만큼 키트를 만든다. 누락, 검사, 출하는 키트 단위로, 보류와 재개는 주문 단위로 처리한다.

| 필드 | 설명 |
|---|---|
| `KitSequence`, `OrderSequence`, `LotNumber`, `HospitalName`, `KitCode` | 생성할 때 정해지며 바뀌지 않는다. `KitCode`는 `string`(EM, BL, DX)이며 생성할 때 대문자로 맞춘다 |
| `DetectedComponentIds` | 슬롯 감지 센서가 만든 박스 내용. 인덱스는 슬롯 번호 - 1, 값은 감지된 구성품 ID, 빈 슬롯은 `null`이다. 길이는 레시피의 슬롯 수와 같다 |
| `Verdict` | `InspectionVerdict?` (판정 전에는 null). 판정 결과의 유일한 출처 |
| `StartTime`, `CompleteTime` | 시뮬레이션 시간(초) |
| 누락 정답 | 빠진 슬롯 번호(`int?`). `SetMissingAnswer(slotNumber)`로 기록(FR-04), `GetMissingAnswerForEvaluation()`으로 읽음. 판정의 `MissingSlots`와 같은 번호 체계이다 |

- **검사 판정 코드(FR-06, FR-07)는 `GetMissingAnswerForEvaluation()`을 호출하지 않는다.** 판정이 끝난 뒤 로그 비교에만 쓴다.

## 4-1. InspectionVerdict

`readonly struct`이며 규칙 판정과 ML 판정이 같은 형식을 쓴다.

| 필드 | 설명 |
|---|---|
| `IsPass` | 정상이면 true, 누락이면 false |
| `MissingSlots` | 비어 있던 슬롯 번호 목록. 규칙 판정만 채운다. 위치를 알 수 없으면 `null`이다. CNN은 2클래스 분류라 위치를 모르고, 규칙 판정도 슬롯 센서가 응답하지 않으면(FR-06 예외) `IsPass=false`, `MissingSlots=null`로 보낸다. 빈 목록은 "누락 슬롯 없음"이며 `IsPass`가 true일 때만 쓴다. 생성할 때 복사해 두므로 넘긴 리스트를 나중에 바꿔도 영향이 없다 |
| `Confidence` | 0~1. 규칙 판정은 항상 1 |
| `Method` | `JudgeMethod.Rule` 또는 `JudgeMethod.Cnn`. 대시보드의 판정 방식 표시와 생산 로그에 쓴다 |

- 로그에서 정답과 비교할 때는 `IsPass`를 먼저 비교하고, `MissingSlots`가 있으면 정답 슬롯 번호가 그 안에 있는지도 비교한다. 정답은 슬롯 번호 하나라 구성품 ID로 바꿀 필요가 없다.
- 규칙 판정은 레시피의 각 항목에 대해 `DetectedComponentIds[SlotNumber - 1]`이 `ComponentId`와 다르면 그 슬롯을 누락으로 본다. 빈 슬롯과 다른 구성품이 들어간 슬롯을 같은 방식으로 잡는다.
- `default` 값이 "규칙 판정 불합격"으로 읽히므로 판정 전 상태는 `KitData.Verdict == null`로 나타낸다.

## 4-2. RecipeSO

키트 종류 하나당 에셋 하나이다. 코드: `Assets/_Project/Scripts/Recipes/` (네임스페이스 `MediKitTwin.Recipes`)

| 필드 | 설명 |
|---|---|
| `KitCode` | `string`. 대문자(EM, BL, DX)이며 레시피끼리 겹치지 않는다 |
| `BoxSize` | L 또는 M. 뚜껑 크기도 이 값을 따른다 |
| `Items` | 피킹 순서대로 나열한 `RecipeItem` 목록 (항목 1개 = 슬롯 1칸) |

| `RecipeItem` 필드 | 설명 |
|---|---|
| `ComponentId` | 구성품 종류 ID. 슬롯 감지 센서가 보고하는 ID와 같다 |
| `ShelfSlotId` | 자재 선반 칸 ID (`A-1` ~ `C-4`) |
| `SlotNumber` | 박스 슬롯 번호(1부터). 슬롯 센서와 `MissingSlots`가 쓰는 번호 |
| `SlotLocalPosition` | 그 슬롯 번호에 대응하는 박스 기준 로컬 좌표(m) |

- 조회: `TryGetItemBySlot(slotNumber)`(센서가 보고한 번호로 구성품과 좌표를 얻음), `TryGetItemByComponent(componentId)`(구성품 기준으로 선반 칸과 슬롯을 얻음).
- 슬롯 번호는 피킹 순서와 따로 정할 수 있다. 1부터 항목 수까지 겹치지 않아야 한다.
- 슬롯 위치의 출처는 레시피이다. 박스 프리팹의 슬롯 센서 콜라이더는 레시피 좌표와 같은 위치에 둔다.
- 키트 코드로 레시피를 찾는 조회(없으면 주문 보류, FR-02)는 주문 처리 구현에서 만든다.

## 5. 로봇암 A 입력 (IArmTarget)

- `ArmController`는 관절 목표를 누가 줬는지 모른다. 구현체는 `IkArmTarget`(필수), `RosArmTarget`(선택)이다.
- 입력은 **관절 각도(rad), URDF 관절 이름 기준**으로 통일한다. IK가 손끝 위치를 직접 넘기는 구조는 쓰지 않는다.
- `TryGetTargets(float[] anglesRad)`는 호출하는 쪽이 만든 배열을 재사용하므로 매 프레임 할당이 없다. 배열 순서는 `JointNames` 순서이다.
- ROS의 `JointState`는 순서가 아니라 `name` 배열로 매칭해서 `JointNames` 순서로 옮긴다.

## 6. ROS 2 토픽 (선택 확장, 10/30 판단)

| 토픽 | 방향 | 타입 | 요구사항 |
|---|---|---|---|
| `/arm_a/joint_command` | ROS → Unity | `sensor_msgs/JointState` | FR-17 |
| `/arm_a/joint_states` | Unity → ROS | `sensor_msgs/JointState` | FR-18 |

- 관절 명령은 시간이 정해진 궤적이 아니라 **목표 각도**로만 쓴다. ROS 쪽은 `joint_states`가 목표에 가까워졌는지로 도착을 판단한다.
- 발행 주기는 실제 시간 기준 20Hz(`Time.unscaledDeltaTime`)이다. 도착 판단 타임아웃은 시뮬레이션 시간 기준이다.
- 관절 이름은 URDF 하나(ROS 패키지의 URDF)를 기준으로 하고, Unity는 URDF Importer로 같은 파일을 불러온다.

## 7. 설비가 공통으로 쓰는 인터페이스

### 7-1. 재고 (`IStock`)

로봇암 A는 재고 클래스를 직접 참조하지 않고 이 인터페이스로만 재고를 다룬다. 선반 칸 ID는 위 「선반 칸 ID」 형식이다.

| 멤버 | 설명 |
|---|---|
| `ShelfSlotIds` | 재고를 관리하는 칸 ID 전체(12개). 대시보드의 선반별 재고 표시에 쓴다 |
| `TryTake(shelfSlotId, orderSequence)` | 구성품 1개를 꺼낸다. 재고가 0이면 `false`이고, 그 칸이 처음 0이 될 때 `StockEmpty`를 발행하며 보충 대기를 시작한다 |
| `Return(shelfSlotId)` | 반쯤 담긴 박스를 제거할 때 담겨 있던 구성품 1개를 되돌린다 |
| `GetQuantity(shelfSlotId)` | 현재 수량 |
| `GetRefillRemainingSeconds(shelfSlotId)` | 보충까지 남은 시간(초). 기다리는 중이 아니면 `null` |

- 피킹은 레시피 순서대로 한 개씩 `TryTake`를 호출한다. 누락 규칙으로 건너뛰는 구성품은 `TryTake`를 호출하지 않으므로 재고가 줄지 않는다.
- 보충은 칸 단위로 한다. 한 칸의 보충 대기가 진행 중이면 같은 칸에 타이머를 다시 걸지 않는다. 대기 시간과 수량은 `ScenarioConfigSO`가 정한다.

### 7-2. 주문 현황 (`IOrderStatus`)

대시보드가 1초마다 읽는 현재 값이다. 변화는 `OrderHeld`, `OrderResumed`로 알리고, 이 인터페이스는 현재 값만 준다.

| 멤버 | 설명 |
|---|---|
| `HeldOrderCount` | 보류 중인 주문 수 |
| `PendingKitCount` | 아직 피킹을 시작하지 않은 키트 수(대기열과 보류 주문의 남은 키트) |

보충까지 남은 시간은 주문이 아니라 칸의 값이므로 `IStock.GetRefillRemainingSeconds`로 읽는다. 대시보드는 보류 주문의 부족한 칸을 `StockEmpty`로 받아 둔 칸 ID로 이 값을 조회한다. 이 전달 방식은 임시이며 대시보드 구현 때 조정한다.

### 7-3. 시뮬레이션 시간 (`SimClock`)

- `SimClock.Now`는 시뮬레이션 시간(초)이다. 시작·완료 시각, 보충 대기, 센서 타임아웃, 로그 시각이 모두 이 값을 읽는다.
- 배속은 `Time.timeScale`로 반영한다. `TrySetSpeed`는 0.5, 1, 2, 4만 받고, `Pause`와 `Resume`으로 일시정지한다. (FR-15)

### 7-4. 검사 판정기 (`IInspector`)

- 규칙 판정과 CNN 판정이 같은 인터페이스를 구현한다. `TryJudge(kit, out verdict)`가 `false`이면 호출하는 쪽이 규칙 판정기로 대체하고 로그를 남긴다. (FR-07 예외 처리)
- 판정기는 `KitData.Verdict`를 채우지 않는다. 호출하는 쪽이 채운 뒤 `InspectionResult`를 발행한다.
- 판정기는 `GetMissingAnswerForEvaluation()`을 호출하지 않는다.

### 7-5. 생산 로그 (`IProductionLog`)

- 열은 시각, 이벤트명, 로트번호, 설비 ID, 판정 방식, 비고이다. 실행마다 CSV 1개를 새로 만들고 덮어쓰지 않는다.
- 로그를 쓰는 쪽은 `CellEvents`를 구독하는 로거 하나이다. 설비는 로그 파일을 직접 다루지 않는다.
- 판정과 정답 비교(FR-06, FR-07)는 판정이 끝난 뒤 로거가 비고에 적는다.

### 7-6. 시나리오 설정 (`ScenarioConfigSO`)

시나리오마다 에셋 하나를 두고, 시나리오를 바꿔도 코드는 바꾸지 않는다.

| 필드 | 설명 |
|---|---|
| 누락 확률(%), 고정 누락 키트 순번, 고정 누락 슬롯 번호 | FR-04. 슬롯 번호가 0이면 무작위로 고른다. 시연에서 빠지는 슬롯을 미리 알릴 때 쓴다 |
| 난수 시드 | 같은 시드와 같은 주문 파일은 같은 결과를 낸다 (NFR-06) |
| 기본 재고, 칸별 재고 | FR-13. 목록에 없는 칸은 기본 재고를 쓴다 |
| 보충 대기(초), 보충 수량 | 기본 30초, 20개 |

| 시나리오 | 누락 | 재고 |
|---|---|---|
| S2 누락 키트 불량 배출 | 10%, 고정 3번 | 기본 20 |
| S4 재고 소진·복구 | 0%, 고정 없음 | C-3 = 2 |

## 8. 확정이 필요한 항목

- [ ] 로봇암 A의 URDF 관절 이름 목록. URDF가 확정되면 기재한다. 팀원이 추가한 ROS-TCP-Connector는 통신 패키지일 뿐 관절 이름을 정하지 않는다
- [ ] 보류 주문의 부족한 칸과 보충 남은 시간을 대시보드에 전달하는 방식(7-2의 임시안을 구현하면서 조정)
