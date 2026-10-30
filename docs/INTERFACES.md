# 공통 인터페이스 정의서

> 근거: SRS v1.3 (3장 기능 요구사항, 4.4 통신 인터페이스, 6장 데이터 요구사항)
> 코드: `Assets/_Project/Scripts/Common/` (네임스페이스 `MediKitTwin.Common`)

## 1. 의존 규칙

- 설비끼리 직접 참조하지 않고 `CellEvents`로만 신호를 주고받는다.
- `Common`은 ROS를 알지 못한다. ROS 코드(`Ros/` 폴더)가 `Common`을 사용하는 한 방향만 허용한다. 이 방향은 어셈블리 정의로 강제한다: `MediKitTwin.Recipes`는 어떤 어셈블리도 참조하지 않고, `MediKitTwin.Common`은 `Recipes`만 참조하며, 이후 만드는 `Ros` 어셈블리는 `Common`만 참조한다.
- 시간은 모두 시뮬레이션 시간(배속 반영)이며 `SimClock`에서 읽는다. 예외는 ROS 토픽 발행 주기(실제 시간 20Hz)와 설비 주기 통지(`StationAlive`, 실제 시간 1초)이다.

## 2. CellEvents

| 신호 | 인자 | 발행 | 구독(예) | 요구사항 |
|---|---|---|---|---|
| `KitReady` | `KitData` | 로봇암 A | 컨베이어 | FR-03 |
| `AtInspection` | `KitData` | 컨베이어 | 검사 | FR-05 |
| `InspectionResult` | `KitData` | 검사 | 컨베이어, 푸셔, 대시보드 | FR-06, FR-07 |
| `KitRejected` | `KitData` | 푸셔 | 컨베이어, 대시보드 | FR-09 |
| `RejectBinCleared` | - | 푸셔(불량 박스 관리) | 대시보드 | FR-09 |
| `AtPacking` | `KitData` | 컨베이어 | 로봇암 B | FR-05 |
| `LidClosed` | `KitData` | 로봇암 B(MVP는 자동 처리) | 컨베이어, 라벨 | FR-10, FR-11 |
| `AtConveyorEnd` | `KitData` | 컨베이어 | 하역 로봇 | FR-05 |
| `KitShipped` | `KitData` | 하역 로봇 | 대시보드 | FR-12 |
| `StockEmpty` | 선반 칸 ID, 주문 순번 | 재고 | 주문 관리, 대시보드 | FR-13 |
| `StockRefilled` | 선반 칸 ID | 재고 | 주문 관리, 대시보드 | FR-13 |
| `OrderHeld` | 주문 순번 | 주문 관리 | 대시보드, 로거 | FR-13 |
| `OrderResumed` | 주문 순번 | 주문 관리 | 대시보드, 로거 | FR-13 |
| `StationStateChanged` | `StationId`, `StationState` | 각 설비 | 대시보드, 로거, 주문 관리 | FR-14, FR-16 |
| `StationAlive` | `StationId`, `StationState` | 각 설비 | 대시보드 | FR-14 |
| `WarningRaised` | 경고 코드, 메시지 | 각 설비, 주문 관리 | 대시보드, 로거 | FR-05, FR-12, FR-13 |

`InspectionResult`는 판정 결과를 인자로 싣지 않는다. 검사는 `KitData.Verdict`를 먼저 채운 뒤 신호를 보내고, 구독자는 `kit.Verdict`를 읽는다. 판정 결과의 출처는 `KitData.Verdict` 하나이다. `Verdict`가 비어 있으면 `RaiseInspectionResult`가 신호를 보내지 않고 오류 로그를 남긴다.

`KitRejected`, `OrderHeld`, `OrderResumed`, `StationStateChanged`, `StationAlive`, `WarningRaised`는 SRS 4.4의 원래 신호 10개에 없다. 컨베이어의 다음 박스 허용(FR-09), 보류 주문 수(FR-13), 설비 상태와 경고(FR-14) 표시에 필요해서 추가했고, SRS 4.4에는 반영했다.

- **`KitRejected`**: 푸셔가 불량 박스를 밀어내 불량 박스에 넣은 직후, 푸셔가 `CompleteTime`을 채운 뒤에 보낸다. 컨베이어는 이 신호로 검사 구역이 비었음을 알고 다음 박스를 보낸다. 컨베이어의 구역 센서 10초 미감지 경고(FR-05)는 이와 별도의 안전장치이다. `RejectBinCleared`는 불량 박스가 정원에 차서 비워질 때만 나오는 신호로 용도가 다르다.
- **`StockEmpty`와 `OrderHeld`**: 재고(`IStock`)가 꺼내기에 실패할 때마다 `StockEmpty`를 보내고, 주문 관리가 이를 받아 그 주문을 보류하며 `OrderHeld`를 보낸다. 보충되면 재고가 `StockRefilled`를 보내고, 주문 관리가 보류 주문을 대기열 맨 앞으로 옮기며 `OrderResumed`를 보낸다.
- **`StationStateChanged`와 `StationAlive`**: 전자는 상태가 바뀔 때만, 후자는 상태가 같아도 1초마다 보낸다. 로거와 주문 관리(Fault이면 해당 주문 보류, FR-16)는 전자만 구독하므로 같은 값을 반복 처리하지 않는다. 대시보드는 후자가 끊긴 시간으로 "응답 없음"을 판단한다.
- **`WarningRaised`**: 경고 코드와 메시지를 보낸다. 같은 주문이 3번 보류되면 주문 관리가 보낸다(FR-13).
- `CompleteTime`은 끝을 알리는 신호를 보내는 쪽이 신호 직전에 채운다: 하역 로봇(`KitShipped`), 푸셔(`KitRejected`).

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
- **주기 통지:** 각 설비는 상태가 바뀌지 않아도 `StationHeartbeat.IntervalSeconds`(1초)마다 `StationAlive`로 현재 상태를 보낸다. 상태가 바뀔 때는 `StationStateChanged`를 따로 한 번 보낸다. 대시보드는 `StationAlive`를 `StationHeartbeat.TimeoutSeconds`(10초) 넘게 받지 못한 설비를 "응답 없음"으로 표시한다.
- 두 값은 실제 시간(`Time.unscaledTime`) 기준이다. 일시정지나 배속 중에도 "응답 없음"으로 오판하지 않기 위해서이다.

## 4. KitData

주문 1건은 수량만큼 키트를 만든다. 누락, 검사, 출하는 키트 단위로, 보류와 재개는 주문 단위로 처리한다.

| 필드 | 설명 |
|---|---|
| `KitSequence`, `OrderSequence`, `LotNumber`, `HospitalName`, `KitCode` | 생성할 때 정해지며 바뀌지 않는다. `KitCode`는 `string`(EM, BL, DX)이며 생성할 때 대문자로 맞춘다. `KitSequence`는 **투입 순서**이다(7-7) |
| `DetectedComponentIds` | 슬롯 감지 센서가 만든 박스 내용. 인덱스는 슬롯 번호 - 1, 값은 감지된 구성품 ID, 빈 슬롯은 `null`이다. 길이는 레시피의 슬롯 수와 같다 |
| `Verdict` | `InspectionVerdict?` (판정 전에는 null). 판정 결과의 유일한 출처 |
| `StartTime`, `CompleteTime` | 시뮬레이션 시간(초). `CompleteTime`은 `KitShipped`나 `KitRejected`를 보내는 쪽이 신호 직전에 채운다 |
| 누락 정답 | 빠진 슬롯 번호(`int?`). `SetMissingAnswer(slotNumber)`로 기록(FR-04), `GetMissingAnswerForEvaluation()`으로 읽음. 판정의 `MissingSlots`와 같은 번호 체계이다 |

- **검사 판정 코드(FR-06, FR-07)는 `GetMissingAnswerForEvaluation()`을 호출하지 않는다.** 판정이 끝난 뒤 로그 비교에만 쓴다.
- 재고 소진으로 보류되었다가 재개되는 키트는 새 `KitData`로 만든다. 누락 정답과 감지 결과를 초기화하는 별도 방법은 없고 필요하지도 않다.

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

토픽의 전체 목록과 메시지 타입은 팀원이 작성한 「AURIX ROS2 토픽 설계 V3」(`docs/Topic.png`)을 기준으로 한다. 이 절은 공통 인터페이스와 맞닿는 규칙만 적는다.

| 토픽 | 방향 | 타입 | 요구사항 |
|---|---|---|---|
| `/arm_a/joint_command` | ROS → Unity | `sensor_msgs/JointState` | FR-17 |
| `/arm_a/joint_states` | Unity → ROS | `sensor_msgs/JointState` | FR-18 |

- **목표 각도만 보낸다.** ROS는 최종 목표 관절 각도만 보내고, 목표까지 움직이는 동작(보간, 속도)은 Unity(`ArmController`)가 `SimClock` 배속에 맞춰 처리한다. ROS가 시간이 정해진 궤적을 보간해서 보내지 않는다. 그래야 배속을 바꿔도 Unity와 ROS 동작이 어긋나지 않는다.
- ROS는 Unity의 시간 배율을 알 수 없다. ROS 쪽 도착 대기 타임아웃은 **실제 시간** 기준이며, 1배속 동작 시간보다 충분히 길게 잡는다.
- **ROS 제어 모드(`RosArmTarget` 사용 중)에서는 배속을 1배로 고정한다.** `SimClock.SetSpeedLocked(true)`를 호출하고 배속 버튼은 `SimClock.IsSpeedLocked`로 비활성화한다. 배속 변경은 Unity 단독 모드에서만 허용한다.
- 발행 주기는 실제 시간 기준 20Hz(`Time.unscaledDeltaTime`)이다.
- 관절 이름은 URDF 하나(ROS 패키지의 URDF)를 기준으로 하고, Unity는 URDF Importer로 같은 파일을 불러온다.
- `/arm_a/goal`의 목표 자세 이름은 Unity 식별자와 다음 규칙으로 바꾼다. 선반 칸 ID `A-1` → `SHELF_A1`(하이픈을 빼고 `SHELF_`를 붙임), 박스 슬롯 번호 `2` → `SLOT_2`.
- 하역 AGV(Limo)와 로봇암 B의 토픽은 이번 범위가 아니다. 향후 확장으로 두고, 공통 인터페이스는 로봇암 A 기준으로만 정의한다.

## 7. 설비가 공통으로 쓰는 인터페이스

### 7-1. 재고 (`IStock`)

로봇암 A는 재고 클래스를 직접 참조하지 않고 이 인터페이스로만 재고를 다룬다. 선반 칸 ID는 위 「선반 칸 ID」 형식이다.

| 멤버 | 설명 |
|---|---|
| `ShelfSlotIds` | 재고를 관리하는 칸 ID 전체(12개). 대시보드의 선반별 재고 표시에 쓴다 |
| `TryTake(shelfSlotId, orderSequence)` | 구성품 1개를 꺼낸다. 재고가 0이면 `false`이고, **꺼내기에 실패할 때마다** 그 주문 순번으로 `StockEmpty`를 발행한다. 보충 타이머는 칸마다 한 번만 건다 |
| `Return(shelfSlotId)` | 반쯤 담긴 박스를 제거할 때 담겨 있던 구성품 1개를 되돌린다. 보충을 기다리는 칸에는 무시한다 |
| `GetQuantity(shelfSlotId)` | 현재 수량 |
| `GetRefillRemainingSeconds(shelfSlotId)` | 보충까지 남은 시간(초). 기다리는 중이 아니면 `null` |

- 피킹은 레시피 순서대로 한 개씩 `TryTake`를 호출한다. 누락 규칙으로 건너뛰는 구성품은 `TryTake`를 호출하지 않으므로 재고가 줄지 않는다.
- 보충은 칸 단위로 하며 칸의 수량을 보충 수량(기본 20)으로 설정한다(더하지 않는다). 대기 시간과 수량은 `ScenarioConfigSO`가 정한다.
- 보충을 기다리는 30초 동안 다른 주문이 같은 칸을 집으려 해도 `TryTake`가 실패하며 `StockEmpty`가 나가므로, 그 주문도 보류되어 로봇암 A가 멈추지 않는다. 한 키트가 한 칸에서 두 번 꺼내지 않으므로, 반쯤 담긴 박스를 되돌릴 때 `Return`이 보충 대기 중인 칸에 닿는 일은 보통 없다.

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
- `SetSpeedLocked(true)`를 호출하면 배속이 1배로 돌아가고 `TrySetSpeed`가 `false`를 돌려준다. ROS 제어 모드가 시작할 때 켜고 끝날 때 끈다. 배속 버튼은 `IsSpeedLocked`가 `true`이면 비활성화하고 이유를 표시한다.

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
| S8 통합 시연(누락 + 재고 소진) | 0%, 고정 3번, 슬롯 2 | C-3 = 4 |

### 7-7. 키트 공급 (`IKitSource`)

로봇암 A는 한가할 때 `TryGetNext(out kit, out recipe)`를 호출해 다음에 만들 키트와 레시피를 받는다. 주문 관리가 구현하며, 로봇암 A는 주문 관리를 직접 참조하지 않는다.

- 만들 키트가 없으면(모든 주문이 보류되었거나 끝났으면) `false`이다. 이때 로봇암 A는 대기 자세로 기다리고 다시 호출한다. (FR-13 예외 처리)
- **`KitSequence`는 투입 순서이다.** 이 호출로 키트를 내줄 때 1, 2, 3, ...으로 부여한다. 시나리오의 "고정 누락 키트 순번"도 이 기준이다.
- 재고 소진으로 보류된 주문이 재개되면, 아직 만들지 않았거나 만들던 중이던 키트를 **새 `KitData`로 만들어** 내준다. 이미 만든 키트는 그대로 진행한다.
- 키트 코드로 레시피를 찾지 못하면 주문을 보류하고 `WarningRaised`를 보낸다. (FR-02 예외 처리)

### 7-8. 박스 공급 (`IBoxSupply`)

| 멤버 | 설명 |
|---|---|
| `PlaceBox(kit, size)` | 레시피의 박스 크기(L, M)에 맞는 빈 박스를 지그에 놓는다. 키트를 시작할 때 호출한다 |
| `RemoveBox()` | 지그의 박스를 치운다. 재고 소진으로 반쯤 담긴 박스를 제거할 때 쓴다(스크립트 처리, 로봇 동작 없음) |

- 재고 소진 흐름: 로봇암 A가 `RemoveBox()`를 호출하고 담겨 있던 구성품을 `IStock.Return`으로 되돌린다. 보류 주문이 재개되면 `PlaceBox`로 새 박스를 놓는다.

### 7-9. 설비별 구현 예정

| 구현 예정 | 구현·사용하는 인터페이스 | 발행하는 신호 | 구독하는 신호 |
|---|---|---|---|
| 재고 | `IStock` 구현 | `StockEmpty`, `StockRefilled` | - |
| 주문 관리 | `IKitSource`, `IOrderStatus` 구현 | `OrderHeld`, `OrderResumed`, `WarningRaised` | `KitReady`, `StockEmpty`, `StockRefilled`, `StationStateChanged` |
| 박스 공급 | `IBoxSupply` 구현 | - | - |
| 로봇암 A | `IKitSource`, `IStock`, `IBoxSupply`, `IArmTarget` 사용 | `KitReady`, `StationStateChanged`, `StationAlive` | - |
| 검사 | `IInspector` 사용 | `InspectionResult` | `AtInspection` |
| 컨베이어 | - | `AtInspection`, `AtPacking`, `AtConveyorEnd`, `WarningRaised` | `KitReady`, `InspectionResult`, `KitRejected`, `LidClosed` |
| 푸셔 | - | `KitRejected`, `RejectBinCleared` | `InspectionResult` |
| 로봇암 B | - | `LidClosed` | `AtPacking` |
| 라벨 | `KitData`의 병원명·로트번호 사용 | - | `LidClosed` |
| 하역 로봇 | - | `KitShipped`, `WarningRaised` | `AtConveyorEnd` |
| 대시보드 | `IStock`, `IOrderStatus` 조회 | - | 대부분의 신호, `StationAlive` |
| 로거 | `IProductionLog` 구현 | - | 대부분의 신호(`StationAlive` 제외) |
| 합성 데이터 생성기 | `RecipeSO` 사용 (`CellEvents` 미사용) | - | - |

- 합성 데이터 생성기(FR-08)는 공정과 같은 조건으로 박스를 채워 이미지와 정답 라벨(키트 코드, 정상·누락)을 저장하는 별도 도구이다. 공정 설비와 신호를 주고받지 않는다.

## 8. 확정이 필요한 항목

- [ ] 로봇암 A의 URDF 관절 이름 목록. 팀원의 포즈 설정(`arm_a_poses.yaml`)은 `joint_1` ~ `joint_6`(6개)을 쓰고 있으나 URDF와 같은지 확인이 필요하다. ROS-TCP-Connector는 통신 패키지일 뿐 관절 이름을 정하지 않는다
- [ ] 보류 주문의 부족한 칸과 보충 남은 시간을 대시보드에 전달하는 방식(7-2의 임시안을 구현하면서 조정)
- [ ] 로봇암 B의 `IArmTarget` 사용 여부(MVP는 자동 처리라 보류)
