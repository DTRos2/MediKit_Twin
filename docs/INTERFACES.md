# 공통 인터페이스 정의서

> 근거: SRS v1.1 (3장 기능 요구사항, 4.4 통신 인터페이스, 6장 데이터 요구사항)
> 코드: `Assets/_Project/Scripts/Common/` (네임스페이스 `MediKitTwin.Common`)

## 1. 의존 규칙

- 설비끼리 직접 참조하지 않고 `CellEvents`로만 신호를 주고받는다.
- `Common`은 ROS를 알지 못한다. ROS 코드(`Ros/` 폴더)가 `Common`을 사용하는 한 방향만 허용한다.
- 시간은 모두 시뮬레이션 시간(배속 반영)이다. 예외는 ROS 토픽 발행 주기(실제 시간 20Hz)이다.

## 2. CellEvents

| 신호 | 인자 | 발행 | 구독(예) | 요구사항 |
|---|---|---|---|---|
| `KitReady` | `KitData` | 로봇암 A | 컨베이어 | FR-03 |
| `AtInspection` | `KitData` | 컨베이어 | 검사 | FR-05 |
| `InspectionResult` | `KitData`, `InspectionVerdict` | 검사 | 컨베이어, 푸셔, 대시보드 | FR-06, FR-07 |
| `RejectBinCleared` | - | 푸셔 | 대시보드 | FR-09 |
| `AtPacking` | `KitData` | 컨베이어 | 로봇암 B | FR-05 |
| `LidClosed` | `KitData` | 로봇암 B(MVP는 자동 처리) | 컨베이어, 라벨 | FR-10, FR-11 |
| `AtConveyorEnd` | `KitData` | 컨베이어 | 하역 로봇 | FR-05 |
| `KitShipped` | `KitData` | 하역 로봇 | 대시보드 | FR-12 |
| `StockEmpty` | 선반 칸 ID, 주문 순번 | 재고 | 주문 관리, 대시보드 | FR-13 |
| `StockRefilled` | 선반 칸 ID | 재고 | 주문 관리, 대시보드 | FR-13 |
| `StationStateChanged` | `StationId`, `StationState` | 각 설비 | 대시보드 | FR-14 |

`StationStateChanged`는 SRS 4.4의 신호 10개에 없다. 대시보드의 설비 상태 표시(FR-14)에 필요해서 추가했으며, 합의되면 SRS 4.4와 개정 이력에 반영한다.

## 3. StationState

| 값 | 대시보드 표시 | 의미 |
|---|---|---|
| `Idle` | 대기 | 작업이 없다 |
| `Running` | 가동 | 작업 중이다 |
| `Fault` | 정지 | 목표 자세에 3초 안에 도달하지 못하는 등 오류 |

- "응답 없음"은 설비가 보내는 값이 아니다. 대시보드가 신호를 10초 넘게 받지 못할 때 표시한다. (FR-14)

## 4. KitData

주문 1건은 수량만큼 키트를 만든다. 누락, 검사, 출하는 키트 단위로, 보류와 재개는 주문 단위로 처리한다.

| 필드 | 설명 |
|---|---|
| `KitSequence`, `OrderSequence`, `LotNumber`, `HospitalName`, `KitCode` | 생성할 때 정해지며 바뀌지 않는다 |
| `DetectedComponentIds` | 슬롯 감지 센서가 만든 박스 내용 목록 |
| `Verdict` | 판정 결과 (판정 전에는 null) |
| `StartTime`, `CompleteTime` | 시뮬레이션 시간(초) |
| 누락 정답 | `SetMissingAnswer`로 기록(FR-04), `GetMissingAnswerForEvaluation()`으로 읽음 |

- **검사 판정 코드(FR-06, FR-07)는 `GetMissingAnswerForEvaluation()`을 호출하지 않는다.** 판정이 끝난 뒤 로그 비교에만 쓴다.

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

## 7. 확정이 필요한 항목

- [ ] 로봇암 A의 URDF 관절 이름 목록 (URDF 확정 후 기재)
- [ ] `StationStateChanged` 추가를 SRS 4.4에 반영할지
- [ ] 주문 보류·재개와 대기 키트 수를 대시보드가 어떻게 받을지 (`StockEmpty`/`StockRefilled`와 주문 큐 직접 조회 중 선택)
- [ ] 설비의 "응답 없음" 판단을 위해 설비가 주기적으로 상태를 다시 알려야 하는지
