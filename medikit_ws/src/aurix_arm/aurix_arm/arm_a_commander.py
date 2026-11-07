"""자세 순서대로 최종 목표 각도를 /arm_a/joint_command로 발행하고 /arm_a/joint_states로 도착을 판정.

보간 궤적은 보내지 않는다. 목표까지의 이동은 Unity가 배속에 맞춰 처리한다.
"""
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import JointState
from ament_index_python.packages import get_package_share_directory
import os

from aurix_arm.pose_table import PoseTable


class ArmACommander(Node):
    def __init__(self):
        super().__init__('arm_a_commander')
        default_cfg = os.path.join(
            get_package_share_directory('aurix_arm'), 'config', 'arm_a_poses.yaml')
        self.declare_parameter('poses_file', default_cfg)
        self.declare_parameter('publish_rate', 20.0)
        self.declare_parameter('tolerance', 0.01)
        # ROS는 Unity 시뮬레이션 시간을 알 수 없으므로 실제 시간 기준이다. 1배속 동작 시간보다 충분히 길게 둔다.
        self.declare_parameter('wait_timeout', 30.0)

        self.table = PoseTable(self.get_parameter('poses_file').value)
        self.tol = self.get_parameter('tolerance').value
        self.timeout = self.get_parameter('wait_timeout').value
        rate = self.get_parameter('publish_rate').value

        self.pub = self.create_publisher(JointState, '/arm_a/joint_command', 10)
        self.create_subscription(JointState, '/arm_a/joint_states', self.on_state, 10)
        self.current = None

        self.idx = 0
        self.goal = list(self.table.poses[self.table.sequence[0]])
        self.phase = 'idle'
        self.phase_t0 = self.get_clock().now()
        self.create_timer(1.0 / rate, self.tick)

    def on_state(self, msg):
        pos = dict(zip(msg.name, msg.position))
        if all(n in pos for n in self.table.joint_names):
            self.current = [pos[n] for n in self.table.joint_names]

    def elapsed(self):
        return (self.get_clock().now() - self.phase_t0).nanoseconds * 1e-9

    def next_pose(self):
        name = self.table.sequence[self.idx % len(self.table.sequence)]
        self.idx += 1
        self.goal = self.table.poses[name]
        self.phase = 'moving'
        self.phase_t0 = self.get_clock().now()
        self.get_logger().info(f'목표 자세: {name}')

    def arrived(self):
        return (self.current is not None and
                max(abs(c - g) for c, g in zip(self.current, self.goal)) < self.tol)

    def tick(self):
        if self.phase == 'idle':
            self.next_pose()

        msg = JointState()
        msg.header.stamp = self.get_clock().now().to_msg()
        msg.name = self.table.joint_names
        msg.position = self.goal
        self.pub.publish(msg)

        if self.arrived():
            self.next_pose()
        elif self.elapsed() > self.timeout:
            self.get_logger().warn('도착 대기 시간 초과, 다음 자세로 진행')
            self.next_pose()


def main(args=None):
    rclpy.init(args=args)
    node = ArmACommander()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.try_shutdown()


if __name__ == '__main__':
    main()
