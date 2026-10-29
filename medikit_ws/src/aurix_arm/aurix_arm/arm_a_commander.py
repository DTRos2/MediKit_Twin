"""자세 순서대로 /arm_a/joint_command를 발행하고 /arm_a/joint_states로 도착을 판정."""
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
        self.declare_parameter('wait_timeout', 10.0)
        self.declare_parameter('move_duration', 2.0)

        self.table = PoseTable(self.get_parameter('poses_file').value)
        self.tol = self.get_parameter('tolerance').value
        self.timeout = self.get_parameter('wait_timeout').value
        self.duration = self.get_parameter('move_duration').value
        rate = self.get_parameter('publish_rate').value

        self.pub = self.create_publisher(JointState, '/arm_a/joint_command', 10)
        self.create_subscription(JointState, '/arm_a/joint_states', self.on_state, 10)
        self.current = None

        self.idx = 0
        self.start = list(self.table.poses[self.table.sequence[0]])
        self.goal = self.start
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
        self.start = self.current if self.current is not None else self.goal
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
        t = self.elapsed()
        cmd = self.table.interpolate(self.start, self.goal, t / self.duration)

        msg = JointState()
        msg.header.stamp = self.get_clock().now().to_msg()
        msg.name = self.table.joint_names
        msg.position = cmd
        self.pub.publish(msg)

        if t >= self.duration:
            if self.arrived():
                self.next_pose()
            elif t > self.duration + self.timeout:
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
