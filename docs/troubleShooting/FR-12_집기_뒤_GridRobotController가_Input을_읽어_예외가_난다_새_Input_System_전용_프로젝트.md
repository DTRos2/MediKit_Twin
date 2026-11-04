# FR-12 하역 로봇: 집기·내려놓기가 끝난 뒤 GridRobotController가 키 입력을 읽어 예외가 난다 (새 Input System 전용 프로젝트)

> 작성일: 2026-11-04 · 브랜치: `feature/dhj-agv-unloading-dock`
> 범위: 에셋 `Warehouse_Robot_Controller`의 `GridRobotController`, `RobotCargoInteractor`와 이 프로젝트의 입력 설정.
> 표시: **확정**(코드나 설정 파일로 확인) · **추정**(코드나 정황으로 본 것) · **미검증**(아직 확인하지 않음)
> **예외를 직접 본 것이 아니다.** 코드를 읽고 일어날 것을 알아 미리 막았다. 패치 전 상태를 에디터에서 재현해 보지 않았다(미검증).
> **최종 확인 (2026-11-04):** 패치를 적용한 상태에서 작성자가 에디터 Play로 집기와 하역, 복귀가 여러 번 정상 동작하는 것을 확인했다(확정). 패치 전 예외 자체는 재현하지 않았으므로 아래 "미검증"은 그대로 둔다.

## 0. 한 줄 요약

`RobotCargoInteractor`는 집기·내려놓기가 끝날 때마다 `GridRobotController.controlsLocked`를 **풀고**(`LockRobot(false)`), 풀린 컨트롤러는 `Update`에서 `UnityEngine.Input.GetKey`로 W, A, S, D를 읽는다. 이 프로젝트는 새 Input System만 쓰므로(`activeInputHandler: 1`) 그 호출은 매 프레임 예외를 낸다(추정). 키 입력 3곳을 `#if ENABLE_LEGACY_INPUT_MANAGER`로 감싸 막았다.

## 1. 증상 (예상)

- 첫 집기나 내려놓기가 끝난 뒤부터 Console에 `UnityEngine.Input`을 읽을 수 없다는 `InvalidOperationException`이 **매 프레임** 쌓일 것으로 본다(추정, 직접 본 적 없음).
- 컨트롤러가 W, A, S, D에 반응해 하역 로봇이 키보드로 움직일 수도 있다(`MobileBaseDriver`의 이동과 겹침).
- 시퀀스는 집기와 내려놓기를 여러 번 되풀이하므로 첫 배달 뒤부터 계속 쌓인다.

## 2. 원인

### 2-1. 컨트롤러가 잠금 해제되면 키 입력을 읽는다 (확정: 코드)

- 씬 구성 도구(`CellSceneBuilder`, `ArmAMobileBaseBuilder`, `AgvPlatformSetup`)는 하역 로봇의 `GridRobotController.controlsLocked`를 켜 둔다. 잠기면 `Update`가 입력을 읽지 않고 애니메이션만 갱신한다.
- 그런데 `RobotCargoInteractor.LockRobot(true)`는 집기·내려놓기를 시작할 때, `LockRobot(false)`는 `OnCargoAnimationFinished`에서 `gridController.controlsLocked`를 바꾼다. **끝나면 잠금이 풀린다.**
- 풀린 컨트롤러는 `ReadMoveInput`(W, S), `HandleIdleInput`(A, D), `UpdateRotation`(A, D)에서 `Input.GetKey`를 부른다.

### 2-2. 이 프로젝트는 옛 Input을 쓰지 않는다 (확정: 설정)

- `ProjectSettings`의 `activeInputHandler`가 1(새 Input System만)이다. 이 설정에서 `UnityEngine.Input`을 부르면 예외가 난다(Unity 동작, 이 프로젝트에서 직접 재현하지는 않았다).
- 같은 이유로 에셋의 `RobotCargoInteractor`(E키)는 이미 `#if ENABLE_LEGACY_INPUT_MANAGER`로 감싸 두었다(이전 작업). `GridRobotController`는 잠금 상태에서만 안전해서 놓쳤다.

## 3. 고친 것

- `GridRobotController`의 `Input` 호출 3곳(`ReadMoveInput`, `HandleIdleInput`, `UpdateRotation`)을 `#if ENABLE_LEGACY_INPUT_MANAGER`로 감쌌다. 옛 Input이 꺼져 있으면 `moveInputDirection = 0`으로 두어 키 입력이 없는 것과 같다. 하역 로봇은 코드(`MobileBaseDriver`)가 움직이므로 키 입력은 필요 없다.
- 수정한 곳에 "MediKit Twin 수정" 주석과 이유를 남겼다. 에셋 스크립트는 전용 어셈블리(`Warcool.WarehouseRobot`)로 분리되어 있어 에셋 폴더를 고쳐도 다른 코드에 영향이 없다.
- **쓰지 않은 방법:** 시퀀스가 매 프레임 `controlsLocked`를 다시 켜는 방법. 애니메이션 이벤트가 `Update` 뒤에 잠금을 풀기 때문에 막을 수는 있지만, 다른 컴포넌트가 만든 상태를 매 프레임 덮어쓰는 임시방편이라 고르지 않았다.
- **건드리지 않은 곳:** `WarehouseCameraController`도 `Input.mouseScrollDelta`를 쓰지만, 씬과 프리팹 어디에도 쓰이지 않는 것을 확인했다(확정: 씬과 프리팹에서 스크립트 GUID 검색, 사용처 0).

## 4. 확인

| 항목 | 결과 |
|---|---|
| 패치 뒤 Play에서 집기·하역 시퀀스가 여러 번 정상 동작하는가 | **확정**(작성자가 Play에서 하역과 복귀까지 확인) |
| 패치 전 실제로 예외가 나는가 | **미검증**(재현하지 않았다. 코드와 설정으로만 판단했다) |
| 에셋 스크립트의 `Input.` 호출이 남아 있는 곳 | **확정**: `GridRobotController` 3곳(패치함), `RobotCargoInteractor` 1곳(이미 패치), `WarehouseCameraController` 1곳(미사용) |

## 5. 재발 방지

- 에셋 `Warehouse_Robot_Controller`를 업데이트하면 위 패치가 지워질 수 있다. 소스에서 `MediKit Twin 수정`을 검색해 두 파일(`GridRobotController`, `RobotCargoInteractor`)의 패치를 다시 확인한다.
- 에셋의 다른 스크립트를 씬에 새로 쓸 때는 `Input.` 호출이 있는지 먼저 검색한다. 이 프로젝트는 새 Input System만 쓴다.
- 에셋 코드를 직접 고친 사실과 이유는 이 문서와 [INTERFACES.md](../INTERFACES.md) 7-13에 적어 두었다.
