"""/cell/events, /cell/station_state, /cell/kpi 구독 → 로그 출력, CSV 저장."""
import csv
import os

import rclpy
from rclpy.node import Node
from std_msgs.msg import String


class CellMonitor(Node):
    TOPICS = ('events', 'station_state', 'kpi')

    def __init__(self):
        super().__init__('cell_monitor')
        self.declare_parameter('csv_path', os.path.expanduser('~/meditkit_ws/cell_log.csv'))
        path = self.get_parameter('csv_path').value
        os.makedirs(os.path.dirname(path) or '.', exist_ok=True)
        self.file = open(path, 'a', newline='')
        self.writer = csv.writer(self.file)
        if self.file.tell() == 0:
            self.writer.writerow(['time_sec', 'topic', 'data'])

        for name in self.TOPICS:
            self.create_subscription(
                String, f'/cell/{name}',
                lambda msg, n=name: self.on_msg(n, msg), 10)
        self.get_logger().info(f'CSV 저장 경로: {path}')

    def on_msg(self, topic, msg):
        now = self.get_clock().now().nanoseconds * 1e-9
        self.get_logger().info(f'[{topic}] {msg.data}')
        self.writer.writerow([f'{now:.3f}', topic, msg.data])
        self.file.flush()

    def destroy_node(self):
        self.file.close()
        super().destroy_node()


def main(args=None):
    rclpy.init(args=args)
    node = CellMonitor()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.try_shutdown()


if __name__ == '__main__':
    main()
