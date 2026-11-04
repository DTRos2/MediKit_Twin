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

- **`KitRejected`**: 푸셔가 불량 박스를 밀어내 불량 박스에 넣은 직후, 푸셔가 `CompleteTime`을 채운 뒤에 보낸다. 컨베이어는 이 신호로 검사 구역이 비었음을 알고 다음 박스를 보낸다. 컨베이어의 구역 센서 10초 미감지 경고(FR-05)는 이와 별도의 안전장치이다. `RejectBinCleared`는 불량 박스가 정원에 차서 비워질 때만 나오는 신호로 용도가 다르다. 밀려난 키트가 불량 박스 밖으로 떨어졌을 때도 컨베이어가 멈추지 않도록 `KitRejected`를 보낸다(7-12).
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
| `MissingSlots` | 레시피와 맞지 않는 슬롯 번호 목록(비어 있거나 다른 구성품이 든 슬롯). 필드 이름은 그대로 두며, 규칙 판정과 CNN 판정(슬롯별 분류) 모두 채운다. 슬롯별로 판정할 수 없으면 `null`이다. 규칙 판정에서 슬롯 센서가 응답하지 않을 때(FR-06 예외)와 CNN이 슬롯 크롭을 만들지 못할 때가 그렇다. 이때는 `IsPass=false`, `MissingSlots=null`로 보낸다. 빈 목록은 "불일치 슬롯 없음"이며 `IsPass`가 true일 때만 쓴다. 생성할 때 복사해 두므로 넘긴 리스트를 나중에 바꿔도 영향이 없다 |
| `Confidence` | 0~1. 규칙 판정은 항상 1 |
| `Method` | `JudgeMethod.Rule` 또는 `JudgeMethod.Cnn`. 대시보드의 판정 방식 표시와 생산 로그에 쓴다 |

- 로그에서 정답과 비교할 때는 `IsPass`를 먼저 비교하고, `MissingSlots`가 있으면 정답 슬롯 번호가 그 안에 있는지도 비교한다. 정답은 슬롯 번호 하나라 구성품 ID로 바꿀 필요가 없다.
- 지금 `KitData`의 정답은 누락(슬롯 번호)만 기록한다. 다른 구성품이 들어간 박스(오구성)를 공정에서 만드는 규칙은 키팅 쪽 작업이며 8장에 적어 둔다.
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
- 보충은 칸 단위로 하며 칸의 수량을 보충 수량(기본 5)으로 설정한다(더하지 않는다). 대기 시간과 수량은 `ScenarioConfigSO`가 정한다.
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

**검사 대상:** 누락뿐 아니라 레시피와 다른 구성품이 든 경우(오구성)도 불량이다. 두 경우 모두 해당 슬롯 번호를 `MissingSlots`에 넣는다.

**판정 방식 (FR-06, FR-07)**

| 판정기 | `Method` | 슬롯 내용을 아는 방법 | 역할 |
|---|---|---|---|
| 규칙 판정 | `Rule` | 슬롯 감지 센서가 만든 `KitData.DetectedComponentIds` | 비전 실패 시 대체 경로 |
| CNN 판정 | `Cnn` | 검사 카메라 이미지에서 슬롯 위치를 잘라 낸 크롭을 CNN이 분류 | 주 경로(Should) |

- **CNN 슬롯 크롭 분류.** 검사 카메라는 고정 시점이고 구성품은 정해진 슬롯 위치에 놓이므로, 레시피의 `SlotLocalPosition`을 카메라 이미지 좌표로 투영해 슬롯마다 크롭을 만든다. CNN은 크롭 하나를 "빈 슬롯"과 구성품 종류 전체 중 하나로 분류한다. 키트 종류가 늘어도 구성품 종류만 늘면 되고, 모델을 다시 학습할 필요가 없다.
- 판정은 규칙 판정과 같은 비교를 한다. 슬롯의 분류 결과가 레시피 항목의 `ComponentId`와 다르면 그 슬롯을 `MissingSlots`에 넣는다. `Confidence`는 슬롯별 분류 신뢰도 중 가장 낮은 값을 쓴다.
- 카메라 이미지나 모델을 쓸 수 없으면 `TryJudge`가 `false`이고, 호출하는 쪽이 규칙 판정기로 대체한다.
- 구현: `CnnSlotJudge`(비교 로직, Sentis에 의존하지 않아 EditMode 테스트 가능)와 `CnnInspector`(`MonoBehaviour`, Sentis 모델 로드)이다. 모델과 `classes.json`은 `Assets/_Project/Models/Inspection/`에 둔다. 입력 전처리(`SlotTensorizer`)는 학습의 PIL BILINEAR 축소와 같은 필터를 쓴다. 슬롯이 불일치로 판정되면 기대 구성품, 예측 라벨, 신뢰도를 로그로 남긴다.

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
| 보충 대기(초), 보충 수량 | 기본 30초, 5개 |

| 시나리오 | 누락 | 재고 |
|---|---|---|
| S2 누락 키트 불량 배출 | 10%, 고정 3번 | 기본 5 |
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
| 로봇암 A | `IKitSource`, `IStock`, `IBoxSupply`, `IArmTarget`, `IShelfItemLocator`, `IKitBoxLocator` 사용 | `KitReady`, `StationStateChanged`, `StationAlive` | - |
| 검사 | `IInspector` 사용 | `InspectionResult` | `AtInspection` |
| 컨베이어 | - | `AtInspection`, `AtPacking`, `AtConveyorEnd`, `WarningRaised` | `KitReady`, `InspectionResult`, `KitRejected`, `LidClosed` |
| 푸셔 | - | `KitRejected`, `RejectBinCleared` | `InspectionResult` |
| 로봇암 B | - | `LidClosed` | `AtPacking` |
| 라벨 | `KitData`의 병원명·로트번호 사용 | - | `LidClosed` |
| 하역 로봇 | - | `KitShipped`, `WarningRaised` | `AtConveyorEnd` |
| 대시보드 | `IStock`, `IOrderStatus` 조회 | - | 대부분의 신호, `StationAlive` |
| 로거 | `IProductionLog` 구현 | - | 대부분의 신호(`StationAlive` 제외) |
| 합성 데이터 생성기 | `RecipeSO` 사용 (`CellEvents` 미사용) | - | - |

- 합성 데이터 생성기(FR-08)는 공정과 같은 조건으로 박스를 채워 슬롯 크롭 이미지와 정답 라벨(슬롯의 구성품 ID, 빈 슬롯은 `empty`)을 저장하는 별도 도구이다. 정상, 누락(구성품 1개 제거), 오구성(구성품 1개를 다른 것으로 교체) 박스를 모두 만든다. 공정 설비와 신호를 주고받지 않는다.

### 7-10. 키팅 흐름 (FR-03, FR-04)

로봇암 A가 키트 1개를 만드는 순서와, 그 순서를 이루는 접점이다. 순서(`KittingProcess`)는 아래 인터페이스로만 협력자를 다루므로 로봇이나 선반 구현을 바꿔도 순서는 그대로다.

1. `IKitSource.TryGetNext`로 키트와 레시피를 받는다(`OrderSession`이 주문 파일을 읽어 만든다).
2. `IMissingRule`이 빠뜨릴 슬롯을 정하고, 그 답을 `KitData.SetMissingAnswer`로 기록한다. 레시피 순서대로 계획(`KittingPlan`)을 세운다.
3. `IBoxSupply.PlaceBox`로 박스를 놓고, `IKitBoxLocator`로 슬롯 위치를 얻는다.
4. 항목마다 `IStock.GetQuantity`가 0이면 `TryTake`로 `StockEmpty`를 내보내고 멈춘다. 아니면 `IShelfItemLocator`로 물건 위치를 얻어 `IItemTransfer`가 집고 놓는다. 재고는 로봇이 쥐는 순간에 줄인다.
5. 모두 놓으면 `KitReady`를 알린다. 중간에 멈추면 꺼낸 구성품을 `IStock.Return`으로 되돌리고 `IBoxSupply.RemoveBox`로 박스를 치운다.

| 접점 | 하는 일 | 구현 |
|---|---|---|
| `IShelfItemLocator` | 선반 칸에서 집을 물건의 자리와 원본을 알려 준다 | `ShelfStockPresenter` (실제 일은 `ShelfItemLocator`) |
| `IKitBoxLocator` | 박스 로컬 슬롯 좌표를 월드 자리로 바꿔 준다 | `KitBoxSupply` |
| `IItemTransfer` | 구성품 1개를 집고 놓는 로봇 동작 | `ArmItemTransfer` (이동 순서) |
| `IItemHolder` | 그리퍼로 물건을 쥐고 놓는 방식(실패 이유 `LastFailureReason`) | `ScriptAttachHolder`(복제본 부착), `PhysicsGraspHolder`(물리 파지) |
| `IMissingRule` | 빠뜨릴 슬롯을 정한다 | `RandomMissingRule` |
| `IToolCameraView` | 도구 끝 카메라의 화면, 자세, 시야 모델, 마지막 인식 결과를 읽는다 | `ToolCameraFeed` |
| `IItemPoseEstimator` | 카메라로 선반 물건의 자세를 알아낸다 | `SimulatedItemPoseEstimator`(시뮬레이션), 실제 영상 인식기는 같은 접점으로 바꿔 끼운다 |
| `IGraspRefiner` | 물건 앞에서 쥐는 자리를 고친다 | `ObservationGraspRefiner`(`ArmPerception`을 거쳐 `ShelfGraspSequence`에 끼워진다) |
| `IObstacleQuery` | 월드의 상자가 장애물과 겹치는지 묻는다 | `PhysicsObstacleQuery` |

- 쥐고 가는 물건은 선반 원본이 아니라 그 복제본이다. 재고가 줄면 원본이 사라지기 때문이다.
- 쥐는 방식은 `ArmItemTransfer`의 Holder에 연결한 컴포넌트로 정한다. `ScriptAttachHolder`는 복제본을 도구 끝점에 붙이는 스크립트 부착 방식(SRS 2.5)이고, `PhysicsGraspHolder`는 복제본에 콜라이더와 `Graspable`을 붙여 `JointGripper`가 고정 조인트로 쥐게 한다. 물리 파지는 복제본을 고정해 둔 채 손가락을 닫아 손가락이 물건에 막혀 멈추게 하고(`IGripContact`로 확인), 붙은 뒤에야 물리로 풀어 들고 간다. 들고 가는 동안 물건은 손가락 외의 충돌체와 부딪히지 않는다(`CollisionIsolation`). 닫은 뒤 시간 안에 쥐지 못했거나 손가락이 끝까지 닫혔다면 실패(`GraspFailed`)로 알리고 이유를 `IItemHolder.LastFailureReason`에 남긴다. `Scene_Cell`은 물리 파지를 쓴다.
- 팔이 선반·박스와 겹치지 않는 자세 찾기: 물리 접촉을 끄지 않고 겹치는 자세로 가지 않는다(`CollisionAvoidanceSettings`의 `Require Clear`, 기본 켬). 자세 몇 군데만이 아니라 지금 자세에서 접근·집기·후퇴로 가는 길 전체를 관절 보간으로 따진다(`ArmCollisionChecker.PathCollides`, `ArmClearanceProbe.IsClear(GraspTargets)`). 스스로 찾는 집기와 박스 놓기는 도구 방향(`GraspOrientationSearcher`의 `isClearAlong`)과 서는 자리(뒤로 물러나기·옆으로 비키기)를 바꿔 가며 겹치지 않는 조합을 찾고, 알려 준 자세는 서는 자리에 도착한 뒤 길이 겹치면 알려 준 방향 가까이에서 방향을 바꿔 찾는다(`TaughtClearancePlanner`). 그래도 없으면 `TransferFailure.CollisionRisk`로 끝내고, `ArmItemTransfer.Pick`이 스스로 찾는 집기로 넘어가 서는 자리까지 새로 찾는다. 방향 후보를 하나씩 따지는 반복은 `OrientationSearchLoop`가 맡고 두 곳이 함께 쓴다.
- 도구 끝 카메라(`ToolCameraFeed`)는 로봇이 준비되면 `ArmItemTransfer`가 도구 링크에 단다. 자리는 `ToolCameraMounter`가 도구 링크 몸통의 충돌체로 몸통 크기를 재(`ToolBodyBounds`) 손 몸통 옆면에 정하고(`ToolCameraPlacement`, 옆면·앞뒤 위치·바라볼 거리는 `ToolCameraSettings`), 몸통을 재지 못하면 설정한 위치로 단다. 카메라 자리에는 눈에 보이는 작은 카메라 본체(`ToolCameraHousing`: 상자와 렌즈, 충돌체 없음, `_showCameraBody`)를 함께 달아 어디에 붙었는지 알 수 있다. 기본 자리는 그리퍼 가운데(`ToolCameraMount.HandCenter`: 손 몸통 옆·위아래 한가운데의 앞끝, 도구 끝점과 같은 쪽을 똑바로 봄, `_forwardFraction` 1)이고, 손 몸통 옆면(`HandRightSide`/`HandLeftSide`)도 고를 수 있다. 로봇 몸이 카메라 화면을 가리지 않도록, 이 카메라가 그릴 때만 로봇 몸의 렌더러를 그리지 않는다(`OwnBodyRenderers`로 모아 `RendererMuter`가 `forceRenderingOff`로 끄고 `ToolCameraFeed`가 이 카메라를 직접 그리는 순간(`camera.Render()`)에만 끈다. 레이어와 물리는 건드리지 않는다. `ToolCameraSettings`의 `_hideOwnBody`). 선반 물건 앞(접근 자리)에 닿으면 `IItemPoseEstimator`로 물건을 찾아, 요청한 자세와 다르면 접근·집기·후퇴 자리를 같은 만큼 옮겨 쥔다(`GraspCorrection`: 잡음 수준 이하는 무시, 한 번에 옮기는 한계 있음). 물건을 못 찾았을 때 집지 않고 멈출지는 설정(`RecognitionSettings`)이 정한다. 시뮬레이션 인식기는 영상을 분석하지 않고, 물건의 실제 위치가 카메라 시야 안에 있고 가려지지 않았는지 보고 센서 잡음을 섞는다. 기본은 카메라 화면을 그리지 않고(`ToolCameraSettings._renderImage` 꺼짐) 자세와 시야만 쓴다. 인식한 물건은 메인 화면의 그 자리에 초록 네모와 칸 ID만 표시한다(`RecognitionMarkerOverlay`가 `IToolCameraView.LastObservation`을 읽고, 얼마나 보여 줄지는 `RecognitionMarkTracker`가 정한다. 켜져 있지 않으면 `RecognitionMarkerBootstrap`이 만든다). `_renderImage`를 켜면 카메라 화면(RenderTexture)을 그린다.
- 장애물 회피: 팔 링크의 충돌체로 링크별 상자(`LinkVolume`)를 만들고, 관절 각도를 `KinematicChain.ForwardLinks`로 링크 위치에 놓아 장애물과 겹치는지 본다(`ArmCollisionChecker`). 도구 방향을 찾을 때(`GraspOrientationSearcher`)와 서는 자리를 가늠할 때(`StandPlacementSearcher`, 후보 자리에 서 있다고 가정한 팔 베이스 자세로 따짐) 닿는 방향 가운데 겹치지 않는 쪽을 우선한다. 겹치지 않는 방향이 없으면 `ClearancePolicy`에 따라 경고를 남기고 가장 덜 무리한 방향으로 가거나(기본) 실패로 끝낸다. 알려 준 자세로 갈 때는 알려 준 자세를 그대로 믿고, 카메라로 고친 자리가 새로 겹칠 때만 고치지 않는다.
- 로봇암 A는 바퀴 달린 베이스(`MobileBaseDriver`) 위에 있고, 키팅할 때 베이스가 물건 앞까지 이동한 뒤 팔이 집고, 쥔 채 박스 곁으로 이동해 놓는다. 서는 자리는 `ArmBaseStandPlanner`가 정하고(선반 물건은 칸 밖 방향으로 `Pick Standoff`, 박스는 `Place Standoff`; `ArmBaseApproachConfig`), 이동은 `IArmBasePositioner`(`MobileArmBasePositioner`)가 맡는다. 베이스가 돌면 팔 베이스가 루트에서 어긋나므로 `IArmBaseLocator`로 팔 베이스 오차를 재서 보정 이동을 되풀이한다. 이동 모듈을 연결하지 않으면 제자리에서 닿는 곳만 집는 고정 팔로 동작한다.
- 선반 칸은 위가 막히고 앞(+Z)만 열려 있으므로 선반 물건은 `ShelfPickItem.ApproachDirection`(칸 밖으로 나가는 방향)에서 수평으로 들어가 쥐고 뒤로 뺀다. 박스 슬롯은 위에서 내려가 놓는다.
- 키팅 진행은 `IKittingObserver`로만 알리고, `KittingStatusTracker`가 모아 `IKittingStatus`(만드는 중인 키트, 항목별 단계, 결과 횟수)로 읽힌다. 화면은 이 현황만 읽는다.
- 주문은 SRS FR-01에 따라 `StreamingAssets/Orders`의 CSV 파일로 읽는 것이 시나리오이다. 패널에서 CSV를 직접 넣는 기능은 시험용 편의이며 SRS의 화면(UI-01, UI-02)에는 없다.
- 설비 상태 통지는 `StationStateReporter`가 맡는다. 상태가 바뀔 때만 `StationStateChanged`를, 1초마다 `StationAlive`를 보낸다.
- 주문은 `OrderSession`이 받는다. 주문 파일을 실행 중에 바꿔도 `IKitSource`, `IOrderStatus` 참조를 다시 잡지 않아도 된다. 로트번호는 이어서 붙이고 `KitSequence`는 파일마다 1부터 센다.
- 박스 크기(L, M)별 모델은 아직 반영하지 않는다(템플릿이 하나). 키팅이 끝나면(`KitReady`) `KitBoxDispatcher`가 `IBoxSupply.TakeBox()`로 지그의 박스를 떼어 컨베이어에 태운다. 박스는 `KitBoxCarrier`(피벗이 박스 바닥 중앙인 받침 + 컨베이어 에셋의 `Parcel`)에 얹혀 가고, `PackingStop`이 팩킹 구간에서 세운다(팔 B가 맞춰진 자리 `Sensor_Packing/PackingStopPoint`는 벨트 중심선보다 팔 쪽으로 약 26cm 치우쳐 있어서, 구간을 멈춘 뒤 박스를 그 자리로 옆으로 미끄러뜨린다). 선 뒤 `AtPacking`을 보내고 `LidClosed`를 받으면 출발시키며, `ConveyorEndNotifier`가 마지막 구간에서 `AtConveyorEnd`를 보낸다. 컨베이어 에셋은 뒤에 이어진 구간이 없으면 박스를 구간 가운데에서 세우므로, 끝에 하역 받침대가 있는 씬에서는 `ConveyorEndRunout`이 막힌 박스를 구간 속도로 받침대의 자리(`AgvPick_Slot`) 위까지 옮겨 받침대 윗면에 내려앉히고 컨베이어에서 푼 뒤(`KitBoxParcel.ReleaseToPhysics`) 알려 주며, `ConveyorEndNotifier`는 그때 `AtConveyorEnd`를 보낸다(받침대에 앞 박스가 있으면 구간 가운데에서 기다린다). 받침대 연결은 `Tools/MediKit/Apply AGV Pick Layout`이 한다. 검사 구역(`AtInspection`~`InspectionResult`)은 아직 구현되지 않아 박스가 멈추지 않고 지나간다.

### 7-11. 검사 구역과 슬롯 감지 센서 (FR-06)

- `InspectionStation`이 `AtInspection`을 받으면 판정 순서(`KitInspection`)를 실행해 `KitData.Verdict`를 채우고 `InspectionResult`를 발행한다. 주 판정기(CNN)가 연결되어 있으면 먼저 시도하고, `TryJudge`가 `false`이면 `INSPECTION_FALLBACK` 경고를 남기고 규칙 판정으로 대체한다.
- 규칙 판정은 슬롯 센서(`SlotSensor`, 슬롯마다 트리거 콜라이더)를 읽어 `KitData.DetectedComponentIds`를 채운 뒤 `RuleInspector`로 레시피와 비교한다. 센서는 박스 프리팹의 `SlotSensorArray` 아래에 둔다.
- 구성품 오브젝트에는 `KitItemLabel`(구성품 ID)를 붙인다. 선반 물건의 복제본은 콜라이더가 없어 물리 트리거 이벤트가 오지 않으므로, 센서는 읽을 때 구성품의 위치가 트리거 콜라이더 부피 안인지 직접 확인한다.
- 센서가 없거나 응답하지 않으면(`SLOT_SENSOR_NO_RESPONSE` 경고) 불량이고 `MissingSlots`는 `null`이다. 레시피를 찾지 못해도(`INSPECTION_RECIPE_MISSING`) 불량으로 본다.
- `InspectionSampleSpawner`(Demo)는 키팅과 컨베이어를 구현하기 전까지 단독 확인용이다. 레시피대로 채운 샘플 박스(정상, 누락, 오구성, 센서 무응답)를 `Sensor_Inspection` 자리에 만들고 `AtInspection`을 보낸다. 구현하면 지운다.
- 검사 카메라(`InspectionCameraRig`)는 구역 위에 고정되어 박스 중심 위 `InspectionCameraSettings`의 높이에서 아래를 보며 찍고, `SlotCropper`가 학습 데이터 생성기와 같은 계산으로 슬롯별 크롭을 자른다. 카메라에서만 그림자를 끄고, 설정 위치와 어긋나면 경고한다. `Save Captures`를 켜면 `InspectionCaptures/<시각>/<로트번호>/`에 PNG로 저장한다. CNN 판정(4단계)은 이 크롭을 입력으로 쓴다.
- CNN 판정기는 박스를 찍는 방법과 레시피 조회를 `IStationBoundInspector.Bind`로 `InspectionStation`에서 받는다(Awake에서 한 번). `PF_InspectionStation`의 Primary Inspector에 `CnnInspector`가 연결되어 있으며, Model과 Classes가 비었거나 모델을 읽지 못하면 오류를 남기고 규칙 판정으로 대체한다.
- 학습 이미지의 조명 색은 흰색부터 씬의 따뜻한 조명(`Scene_Cell`의 Directional Light 색)까지 흔든다(`SyntheticRandomization`의 `Light Warmth Deviation`). 씬의 조명 색이나 세기를 크게 바꾸면 크롭 색이 학습 범위를 벗어나 오분류하므로, 바꿨다면 데이터셋을 다시 만들고 재학습한다.
- 검사 대상 박스는 `InspectionStation`에서 수평 거리가 `Box Search Radius` 안인 가장 가까운 `SlotSensorArray`이다. 높이는 따지지 않는다.

### 7-12. 푸셔와 불량 박스 (FR-09)

- `Pusher`는 `InspectionResult`가 불량이면 박스가 푸셔 앞의 감지 영역에 오기를 기다렸다가(최대 30초, 넘으면 `PUSHER_BOX_NOT_FOUND` 경고 후 그 키트는 건너뜀) 헤드를 앞으로 밀었다가 되돌린다. 밀기 방향은 `Pusher` 오브젝트의 로컬 +X이다. 박스는 컨베이어를 가로질러 가장자리를 넘어 물리로 불량 박스에 떨어진다.
- 박스는 감지 영역을 물리로 훑어 찾는다. 충돌체에서 위로 올라가며 이름이 `KitBox`로 시작하는 오브젝트를 박스로 본다. 감지 영역은 컨베이어 윗면과 레일에 닿지 않고 박스에만 닿도록 얇다(박스 중심 높이 ±4cm). 검사 구역, 샘플 생성기, 컨베이어를 직접 참조하지 않는다.
- 밀기 직전에 박스에 `Rigidbody`(비키네마틱)와 `RejectedKit`(어느 `KitData`인지) 표시를 붙인다. `Rigidbody`가 이미 있으면 키네마틱을 끄고 쓴다. 컨베이어가 박스를 `MoveTowards`로 옮기는 방식(`Parcel`)을 쓰면 컨베이어 쪽 처리는 FR-05에서 한다.
- `RejectBin`은 `LoadZone`(트리거 콜라이더)에 `RejectedKit`이 붙은 박스가 들어오면 `KitData.CompleteTime`을 채우고 `KitRejected`를 보낸다. 적재 수(`RejectBinTally`)가 정원(기본 5)에 차면 1초 뒤 박스를 모두 지우고 `RejectBinCleared`를 보낸다. 누적 불량 수는 비워도 유지된다.
- 푸셔가 민 뒤 5초 안에 불량 박스에 들어오지 않으면 `PUSHER_KIT_OUTSIDE` 경고를 보내고 박스를 지운다. 누적 불량 수에는 포함하고 적재 수는 올리지 않으며, `KitRejected`는 보낸다(SRS FR-09 예외).
- 헤드는 키네마틱 `Rigidbody`로 물리 스텝마다 움직인다. 기본값은 전진 1.05m, 미는 속도 1.0m/s, 복귀 1.2m/s이다. 이 값은 `Tools > MediKit > Measure Pusher Layout`으로 잰 배치(컨베이어 가장자리에서 불량 박스 안쪽 벽까지 수평 틈 0.149m, 컨베이어 윗면과 벽 윗면의 높이 차 약 0.40m, 가장자리에서 필요한 속도 약 0.69m/s)에서 정했다.
- 헤드 바닥 높이는 컨베이어 본체 충돌체 윗면(0.85m)보다 1.5cm 위이다. 컨베이어 패키지의 `CenterPoint`(0.91m)는 실제 윗면보다 0.06m 높아서 기준으로 쓰지 않는다. `Conv_Inspection_04`의 가이드 레일 두 개는 푸시 길을 막지 않도록 컨베이어 윗면보다 낮춰 두었다(씬의 인스턴스 값).
- 검사 구간 컨베이어(`Conv_Inspection_01 ~ 06`)는 `InspectionConveyorLine`(`PF_InspectionStation`에 붙임)이 움직인다. 박스(`KitBoxParcel` + 컨베이어 패키지의 `Parcel`)를 01 입구에 올리면 패키지가 02 중심으로 옮기고, 02 중심에서 멈춘 뒤 이 컴포넌트가 `AtInspection`을 보낸다. `InspectionResult`가 나오면 정상 박스는 03, 04를 지나 06 중심에서 `End Hold Seconds`(3초) 멈췄다가 지워지고, 불량 박스는 04 중심에서 멈춰 푸셔를 기다린다. 한 번에 박스 하나만 시험한다.
- 멈추는 방식: 박스가 칸 중심에서 `Stop Radius`(8cm) 안에 들어오면 그 칸의 `Speed`를 0으로 하고 박스를 중심에 맞춘 뒤, 박스가 칸을 떠날 때(`ParcelExited`) `Speed`를 되돌린다. 이 방식은 컨베이어 패키지 코드를 고치지 않는다.
- 푸셔는 박스가 자리 가까이(`Spot Tolerance` 6cm)에서 0.3초 움직이지 않을 때 밀기 시작한다. 밀기 직전에 박스의 `IBoxConveyorRelease.ReleaseToPhysics()`(`KitBoxParcel` 구현)를 불러 컨베이어에서 풀고 `Parcel`을 꺼서, 컨베이어 입구 영역에 닿은 박스를 패키지가 다시 붙잡지 않게 한다. 푸셔는 컨베이어 패키지를 참조하지 않는다.
- `InspectionSampleSpawner`는 씬에 `InspectionConveyorLine`이 있으면 박스를 컨베이어에 올리고 `AtInspection`을 직접 보내지 않는다. 컨베이어가 없으면(또는 `Use Conveyor`를 끄면) 박스를 검사 자리에 바로 놓고 `AtInspection`을 보낸다. 이때 불량 판정 1초 뒤 박스를 푸셔 앞 자리(`Pusher.BoxSpotWorld`)로 옮긴다.
- `PF_Pusher`와 `PF_RejectBin`은 다시 만들지 않고 `Tools > MediKit > Setup Pusher Prefab`, `Add Load Zone To RejectBin Prefab`으로 컴포넌트를 더한다. 빌더의 "Rebuild … Prefabs"는 `LoadZone`을 지우므로 다시 만든 뒤에는 이 메뉴를 한 번 더 실행한다.

### 7-13. 하역 받침대 (FR-12)

- 컨베이어 끝에는 하역 로봇(`UnloaderRobot`, 에셋 `Warcool.WarehouseRobot`)이 박스를 집는 받침대가 있다. `Unloader` 어셈블리는 에셋 어셈블리를 참조한다(CONVENTION 8장 예외).
- `AgvPickPlatform`(받침대에 붙임)은 `AtConveyorEnd`를 받으면 `Settle Seconds`(0.3초) 기다린 뒤 칸 위치 주변을 물리로 훑어 그 키트의 박스(`IKitCarrier`)를 찾는다. 박스는 받침(`KitBoxCarrier`)에서 떼어 `CargoBox`, `KitCargo`(어느 키트인지), 키네마틱 `Rigidbody`를 붙이고 `ShelfStorage`의 칸(열 0, 칸 `Level` = 2)에 `PutBox`로 등록한다. 빈 받침은 지운다. 박스를 칸 위치로 `Max Snap`(2cm)보다 많이 옮기게 되면 박스 원점 가정이 어긋났다고 경고한다.
- 칸 번호가 곧 로봇 팔 높이이다(에셋의 고정 애니메이션). 칸 2번이 `Load_Level_2`이며 벨트 윗면 높이에 닿는다.
- `KitCargo`는 `IKitCarrier`를 구현한다. 받침이 사라진 뒤에도 박스가 받침대에 남아 있는지 컨베이어 끝(`ConveyorEndRunout`)이 알아보게 하려는 것이다(앞 박스가 있으면 뒤 박스는 구간 가운데에서 기다린다).
- 받침대와 칸, 구역, 컴포넌트 연결은 `Tools/MediKit/Apply AGV Pick Layout`이 한다.
- 하역장(`UnloadingDock`)에는 도크 바닥판 전체를 덮는 단(`DockPlace_Body`, 윗면 0.218m)이 있고, 로봇은 단 바깥 -x쪽 바닥에서 +x를 보고 서서 월드 -x면 가장자리에 박스를 내려놓는다. 칸은 `Unload_Level_0`(박스 원점 높이 0.268m)이다. 로봇 팔 높이가 칸 번호로 고정되어 바닥에는 놓을 수 없어서 단이 필요하다.
- 키트 종류마다 구역(열)이 하나이다. 구역은 -x면 가장자리를 따라 z 방향으로 0.6m 간격으로 놓이고, 구역마다 칸(`DockPlace_Slot_n`), 로봇이 서야 할 자리(`DockPlace_DockPoint_n`: 칸에서 -x로 팔 도달 거리 0.394m), 감지 구역(`DockPlace_Zone_n`)이 있다. 박스 중심은 단 가장자리에서 0.18m 안쪽이고 로봇 중심은 가장자리에서 0.214m 바깥이다. 구역 번호와 키트 코드의 대응은 `UnloadingDockStock`의 `Kit Codes`(기본 EM, BL, DX) 순서이며 `ColumnFor(kitCode)`로 찾는다.
- `UnloadingDockStock`은 가장자리 칸에 박스가 놓이면 `Shift Delay Seconds`(1초) 뒤 그 구역의 박스를 모두 +x로 `Pitch`(0.14m)만큼 밀어 가장자리 칸을 비운다. 밀려난 박스는 `DockPlace_Stock` 아래에 쌓이고, 구역마다 `Capacity Per Zone`(10)을 넘으면 가장 오래된(가장 먼) 박스를 치우고 `DOCK_ZONE_FULL` 경고를 보낸다.
- 만들고 맞추기는 `Tools/MediKit/Setup Unloading Dock Platform`이 하며 `AGVPath`는 건드리지 않는다. 경로 끝을 구역의 `DockPoint`에 맞추는 것은 사람이 한다.
- `AgvUnloadSequence`(`UnloaderRobot`에 붙임)는 `AgvPickPlatform.BoxRegistered`로 받은 박스를 등록 순서대로 하나씩 배달한다. 받침대 `DockPoint`로 이동·정렬 → 집기(`RobotCargoInteractor.RequestInteract`, Load) → 뒤로 물러남 → `AGVPath`를 키트 구역 가까이까지 이동 → 구역의 `DockPoint`로 이동·정렬 → 내려놓기(Unload) → `KitData.CompleteTime`을 `SimClock.Now`로 채우고 `KitShipped` 발행 → 뒤로 물러남 → 같은 경로를 되짚어 받침대 `DockPoint`로 복귀한다.
- 이동 규칙: `DockPoint`는 받침대나 단 가장자리에서 팔 도달 거리밖에 안 떨어져 있어(로봇 중심에서 가장자리까지 0.214m) 제자리에서 돌면 로봇과 든 박스가 물체를 파고든다. 그래서 자리에 들어갈 때는 자리에서 `Staging Distance`(0.5m) 뒤(스테이징)에서 방향을 돌린 뒤 똑바로 전진하고, 나올 때는 `IReversibleMobileBase.ReverseTo`로 똑바로 후진해 스테이징까지 물러난 뒤 돈다. `IMobileBase.DriveTo`는 목표 쪽으로 제자리에서 돌고 나서 전진하므로 이런 자리에서는 쓰지 않는다. `MobileBaseDriver`가 `IReversibleMobileBase`를 구현한다(등이 앞이라고 보고 같은 구동 판단을 쓴다).
- 경로: `AGVPath`는 하나이고 구역은 여럿이라 경로 끝이 목표 자리가 아니다. `AgvRoute.WaypointsBefore`가 목표 `DockPoint`에서 경로 위 가장 가까운 지점을 찾아 그 앞의 웨이포인트까지만 지나게 하고(끝까지 가서 되돌아오지 않는다), 되돌아올 때는 같은 웨이포인트를 거꾸로 지난다. 경로를 사람이 고쳐도 같은 규칙이 적용된다.
- 웨이포인트 읽기: `AGVPath`의 자식에는 노란 레인(`AGV_Strip_n`)도 있어서, 이름이 `Waypoint Name Prefix`(기본 `AGV_WP`)로 시작하는 자식만 순서대로 웨이포인트로 읽는다.
- 겹침 회피: 로봇은 사람이 정한 웨이포인트를 그대로 따른다. 다만 로봇 몸(`Footprint`: 구역 감지용 트리거 박스의 크기)이 물체(트리거와 윗면 `Ignore Below`(5cm) 아래의 바닥·레인은 제외)와 `Clearance Margin`(3cm) 안으로 겹치는 점은 `AgvClearance.PushOut`으로 물체 밖으로 겹침이 가장 작은 쪽으로 최소한만 옮긴다. 경로를 `Clearance Sample Step`(0.15m) 간격으로 훑어 겹치는 곳마다 옮긴 점을 경로에 넣는다. 옮긴 거리가 1cm를 넘으면 Console에 경고를 한 번 남긴다. 스테이징과 자리에 들어가는 마지막 구간은 일부러 물체 가까이 가므로 옮기지 않는다. 웨이포인트를 물체에서 로봇 반폭 이상 띄워 두면 경로 그대로 간다.
- 집기와 내려놓기는 에셋의 고정 애니메이션이라, 로봇이 구역에 들어와 정렬되고(위치 5cm, 방향 5도) 내려놓을 칸이 비기 전에는 시작되지 않는다. 시퀀스는 시작될 때까지 `Interact Timeout Seconds`(10초) 동안 요청을 되풀이한다. 이동은 `IMobileBase`(`MobileBaseDriver`)로만 지시하고 한 지점당 `Drive Timeout Seconds`(60초)를 준다.
- 실패하면 `WarningRaised`로 알리고 그 박스를 건너뛴다(`KitShipped`는 보내지 않는다): `UNLOADER_DRIVE_TIMEOUT`(도착 못 함), `UNLOADER_LOAD_FAILED`(집기 실패), `UNLOADER_UNLOAD_FAILED`(내려놓기 실패), `UNLOADER_NO_DOCK_ZONE`(키트 코드에 맞는 구역이 없어 첫 구역에 내려놓음, 로봇 자리가 연결되지 않았으면 건너뜀).
- 연결은 `Tools/MediKit/Setup Unloader Sequence`가 한다. 이동 컴포넌트는 집기 컴포넌트와 같은 오브젝트에 붙인다(정렬 판정이 그 오브젝트의 위치와 방향을 본다). 먼저 `Apply AGV Pick Layout`과 `Setup Unloading Dock Platform`을 실행해 둔다.
- 에셋 `GridRobotController`의 키 입력 3곳을 `#if ENABLE_LEGACY_INPUT_MANAGER`로 감쌌다. `RobotCargoInteractor`가 집기·내리기가 끝날 때마다 `controlsLocked`를 풀어 이 컨트롤러가 `UnityEngine.Input`을 읽게 되고, 이 프로젝트는 새 Input System만 써서 매 프레임 예외가 나기 때문이다.

## 8. 확정이 필요한 항목

- [x] 로봇암 A의 URDF 관절 이름은 FR5 URDF(`fr5_gripper.urdf`)의 `j1` ~ `j6`(6개)이다. `arm_a_poses.yaml`, `RosArmTarget`, ROS 쪽 URDF를 이 이름에 맞췄다
- [ ] 보류 주문의 부족한 칸과 보충 남은 시간을 대시보드에 전달하는 방식(7-2의 임시안을 구현하면서 조정)
- [ ] 로봇암 B의 `IArmTarget` 사용 여부(MVP는 자동 처리라 보류)
- [ ] 오구성 박스를 공정에서 만드는 규칙(키팅 쪽, `IMissingRule`과 별개). 시연에 오구성을 보여 주려면 필요하며, `KitData`에 오구성 정답을 기록하는 방법도 함께 정한다
- [ ] SRS(docx) 갱신: FR-07 "정상·누락 2클래스"를 슬롯별 분류로, FR-08 라벨을 슬롯별 구성품 ID로, NFR-04 지표를 누락·오구성 모두 포함하도록 바꾼다. 90% 기준은 다시 정한다
