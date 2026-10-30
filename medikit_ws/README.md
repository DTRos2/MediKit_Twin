# medikit_ws

ROS2 Humble 워크스페이스. 다른 PC의 Unity와 TCP로 연동해 로봇암 A를 구동하고 공정을 모니터링한다.
Unity ↔ ROS 중계는 직접 구현한 **C++ 엔드포인트**(`ros_tcp_endpoint_cpp`)가 맡는다.

| 항목 | 값 |
|---|---|
| OS / ROS | Ubuntu 22.04 / ROS2 Humble |
| 이 PC IP (같은 공유기) | `192.168.0.100` (Tailscale: `100.107.185.97`) |
| TCP 포트 | `7777` |
| 프로토콜 | Unity ROS-TCP-Connector v0.7.0 (ROS2) |

---

## 1. 구조

```
Unity PC                                  이 PC (ROS2 Humble)
┌────────────────────┐   TCP :7777   ┌──────────────────────────────┐
│ ROS-TCP-Connector  │ ◄───────────► │ ros_tcp_endpoint_cpp         │
│ (URDF Importer)    │               │   ▲ /arm_a/joint_command ▼   │
└────────────────────┘               │ aurix_arm  (arm_a_commander) │
                                     │ aurix_monitor (cell_monitor) │
                                     └──────────────────────────────┘
```

```
medikit_ws/src/
├── ROS-TCP-Endpoint/        Unity 공식 Python 엔드포인트 (main-ros2). 대체용으로 보관
├── ros_tcp_endpoint_cpp/    ★ C++ 엔드포인트 (아래 4장)
├── aurix_description/       로봇암 A URDF (6축, 자리표시용 형상)
├── aurix_arm/               로봇암 A 데모 노드 (필수)
├── aurix_rl/                강화학습 정책 노드, MoveIt 목표 노드 (C++, 선택)
├── aurix_monitor/           공정 모니터링 노드 (선택)
└── aurix_bringup/           launch 파일과 파라미터 묶음
```

| 패키지 | 빌드 | 내용 |
|---|---|---|
| `aurix_description` | ament_cmake | `urdf/arm_a.urdf` (j1~6), `meshes/` |
| `aurix_arm` | ament_python | `arm_a_commander.py` 자세 순서대로 최종 목표 각도 발행·도착 판정, `pose_table.py` yaml 로드, `config/arm_a_poses.yaml` |
| `aurix_rl` | ament_cmake (C++) | `rl_policy_node` Unity 관측 수신 → 정책 백엔드 → 행동 발행, 보상·성공률 로그 / `moveit_goal_node` 목표 자세 → MoveIt 계획 → 최종 관절 각도만 `/arm_a/joint_command`로 발행 |
| `aurix_monitor` | ament_python | `cell_monitor.py` — `/cell/events·station_state·kpi` 구독 → 로그, CSV 저장 |
| `aurix_bringup` | ament_python | `aurix.launch.py`, `endpoint_only.launch.py`, `config/params.yaml` |
| `ros_tcp_endpoint_cpp` | ament_cmake | Unity용 C++ TCP 엔드포인트 |

---

## 2. 빌드

새 터미널을 열 때마다 환경을 먼저 불러온다.

```bash
source /opt/ros/humble/setup.bash          # ROS2 기본 환경
cd ~/medikit_ws
colcon build --symlink-install             # 전체 빌드 (--symlink-install: 설정·launch 수정이 재빌드 없이 반영)
source install/setup.bash                  # 이 워크스페이스 환경
```

패키지 하나만 다시 빌드할 때:

```bash
colcon build --symlink-install --packages-select ros_tcp_endpoint_cpp
```

매번 입력하기 번거로우면 한 번만 등록한다.

```bash
echo "source /opt/ros/humble/setup.bash" >> ~/.bashrc
echo "source ~/medikit_ws/install/setup.bash" >> ~/.bashrc
```

---

## 3. 실행

### 3-1. 엔드포인트만 (송수신 테스트용)

```bash
ros2 launch aurix_bringup endpoint_only.launch.py
```

로그에 `Starting server on 0.0.0.0:7777`이 보이면 정상이다.

### 3-2. 데모 전체

```bash
ros2 launch aurix_bringup aurix.launch.py                  # 엔드포인트 + arm_a_commander
ros2 launch aurix_bringup aurix.launch.py monitor:=true    # + cell_monitor (CSV 저장)
```

### 3-3. launch 옵션

| 옵션 | 기본값 | 설명 |
|---|---|---|
| `cpp:=true/false` | `true` | `true`면 C++ 엔드포인트, `false`면 기존 Python 엔드포인트 |
| `monitor:=true/false` | `false` | `aurix.launch.py`에서 `cell_monitor` 실행 여부 |

### 3-4. 파라미터 (`aurix_bringup/config/params.yaml`)

| 노드 | 파라미터 | 값 | 설명 |
|---|---|---|---|
| `endpoint` | `ROS_IP` | `0.0.0.0` | 모든 네트워크 인터페이스에서 접속 허용 |
| `endpoint` | `ROS_TCP_PORT` | `7777` | Unity와 맞춰야 하는 포트 |
| `arm_a_commander` | `publish_rate` | `20.0` Hz | 관절 명령 발행 주기 |
| `arm_a_commander` | `tolerance` | `0.01` rad | 도착 판정 허용오차 |
| `arm_a_commander` | `wait_timeout` | `30.0` s | 도착 대기 제한, 실제 시간 기준 (초과 시 경고 후 다음 자세) |
| `cell_monitor` | `csv_path` | `/home/coc/medikit_ws/cell_log.csv` | 모니터 로그 저장 경로 |

로봇암 자세와 데모 순서는 `aurix_arm/config/arm_a_poses.yaml`에서 바꾼다 (`poses`, `sequence`).

---

## 4. Unity 연동

### 4-1. Unity PC 설정

`Robotics > ROS Settings`

| 항목 | 값 |
|---|---|
| Protocol | **ROS2** |
| ROS IP Address | `192.168.0.100` (같은 공유기) 또는 `100.107.185.97` (Tailscale) |
| ROS Port | **7777** |

Play를 누르면 연결 표시가 파란색으로 바뀌고, 서버 터미널에 `Connection from <Unity PC IP>`가 찍힌다.

### 4-2. 연결 확인

서버를 먼저 켠 상태에서 확인한다. **Unity PC에서** 실행해야 의미가 있다.

```powershell
# Windows PowerShell
Test-NetConnection 192.168.0.100 -Port 7777     # TcpTestSucceeded : True 면 정상
```
```bash
# Mac / Linux
nc -vz 192.168.0.100 7777                       # succeeded 면 정상
```

이 PC에서 서버가 떠 있는지만 볼 때:

```bash
ss -ltn | grep 7777                             # LISTEN 0.0.0.0:7777 이 보이면 정상
```

### 4-3. 토픽 통신 확인 (새 터미널)

```bash
# ROS → Unity: Unity가 /arm_a/joint_command 를 구독하고 있어야 받는다
ros2 topic pub -r 5 /arm_a/joint_command sensor_msgs/msg/JointState \
  "{name: [j1, j2, j3, j4, j5, j6], position: [0.5, 0.3, 0, 0, 0, 0]}"

# Unity → ROS: Unity가 /arm_a/joint_states 를 발행하면 여기서 보인다
ros2 topic echo /arm_a/joint_states

ros2 topic list                                 # 토픽 목록
```

### 4-4. 사용 토픽

| 토픽 | 방향 | 타입 | 설명 |
|---|---|---|---|
| `/arm_a/joint_command` | ROS → Unity | `sensor_msgs/JointState` | commander가 보내는 목표 관절각 |
| `/arm_a/joint_states` | Unity → ROS | `sensor_msgs/JointState` | 현재 관절각 (도착 판정에 사용) |
| `/cell/events` `/cell/station_state` `/cell/kpi` | Unity → ROS | `std_msgs/String` (가정) | 공정 모니터링. 실제 타입이 다르면 `cell_monitor.py` 수정 필요 |

---

## 5. C++ 엔드포인트 (`ros_tcp_endpoint_cpp`)

Unity 공식 Python 엔드포인트와 같은 프로토콜을 쓰는 C++ 구현이라 Unity 쪽은 수정할 필요가 없다.
메시지를 **직렬화된 바이트 그대로** 중계하므로 새 메시지 타입을 써도 서버 코드는 그대로다.

### 5-1. 파일 구성

| 파일 | 역할 | ROS 의존 |
|---|---|---|
| `protocol.hpp` | 프레임 송수신, JSON 파싱, 타입 이름 변환 | 없음 |
| `session` | 소켓 하나와 송신 큐 | 없음 |
| `tcp_server` | 접속 수락, 클라이언트별 수신 스레드 (콜백 방식) | 없음 |
| `command_handler` | `__이름` 시스템 명령을 핸들러로 분배 | 없음 |
| `topic_bridge` | 퍼블리셔·서브스크라이버 테이블, Unity↔ROS 중계 | rclcpp |
| `endpoint_server` | 위 부품을 조립하는 ROS 노드 | rclcpp |
| `main.cpp` | 진입점 | rclcpp |

CMake에서 ROS 비의존부는 `tcp_core`, ROS 연동부는 `ros_bridge` 정적 라이브러리로 나뉜다.

### 5-2. 지원하는 Unity 명령

| 명령 | 동작 |
|---|---|
| `__publish` | Unity가 보낼 토픽의 퍼블리셔 생성 (`queue_size`, `latch` 지원) |
| `__subscribe` | ROS 토픽을 구독해 Unity로 전달 |
| `__topic_list` | ROS 토픽 목록과 타입 응답 |
| 빈 destination | 키프얼라이브, 무시 |
| 기타 | Unity에 `__error` 로그 전달 |

### 5-3. 새 명령 추가 방법

`endpoint_server.cpp`의 `register_commands()`에 한 줄을 추가한다. 서버 본체는 수정하지 않는다.

```cpp
commands_.add("명령이름", [this](const CommandHandler::Params & p) {
  // p.at("키") 로 Unity가 보낸 JSON 값 접근
});
```

### 5-4. 제한 사항

- **서비스 미지원**: `__ros_service`, `__unity_service`, `__request`, `__response`는 Unity에 에러 로그를 돌려준다. rclcpp에 타입 없는 서비스 클라이언트가 없기 때문이다. 현재 구성은 토픽만 써서 영향이 없다.
- 여러 Unity가 접속하면 **가장 최근에 접속한 연결**로만 ROS 메시지를 보낸다 (원본과 동일).

---

## 6. 문제 해결

| 증상 | 원인과 해결 |
|---|---|
| `nc`가 `Connection refused` | 서버가 안 떠 있음. 3-1의 명령으로 서버부터 실행 |
| `Test-NetConnection: command not found` | Windows PowerShell 명령이다. Linux에서는 `nc -vz` 사용 |
| `bind() failed ... Address already in use` | 이전 서버가 남아 있음. `ps -eo pid,args \| grep default_server_endpoint`로 PID 확인 후 `kill <PID>` |
| 서버 로그에 접속 기록이 없음 | IP·포트 불일치 또는 네트워크 문제. Unity PC에서 4-2의 연결 확인 |
| `Not registered to publish topic ...` | Unity에서 `RegisterPublisher` 호출 전에 `Publish`함 |
| 토픽이 안 보임 | `ros2 topic list` 확인, 환경(`source`) 누락 여부 확인 |
| 포트를 바꾸고 싶음 | `aurix_bringup/config/params.yaml`의 `ROS_TCP_PORT`와 Unity의 ROS Port를 같이 변경 |

> 방화벽(`ufw`)은 이 PC에서 꺼져 있어 포트를 따로 열 필요가 없다.
> 연결이 안 되면 Unity PC 쪽 방화벽이나 공유기 설정을 확인한다.

---

## 7. 검증 현황

| 항목 | 상태 |
|---|---|
| 전체 패키지 빌드 (`colcon build`) | 통과 |
| C++ 엔드포인트 ↔ 가짜 Unity(Python 소켓) 통신 | 통과 (핸드셰이크, publish, subscribe, topic_list, 에러 응답) |
| 실제 Unity 연동 | **미확인** |
| `aurix_arm` / `aurix_monitor` 노드 실행 | **미확인** (빌드만 확인) |
| `arm_a.urdf` 형상 | 자리표시용. 실제 로봇 모델로 교체 필요 |
