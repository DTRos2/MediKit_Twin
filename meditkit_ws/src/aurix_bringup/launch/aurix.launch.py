import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition, UnlessCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    params = os.path.join(get_package_share_directory('aurix_bringup'), 'config', 'params.yaml')
    return LaunchDescription([
        DeclareLaunchArgument('cpp', default_value='true'),
        DeclareLaunchArgument('monitor', default_value='false'),
        Node(
            package='ros_tcp_endpoint',
            executable='default_server_endpoint',
            name='endpoint',
            parameters=[params],
            output='screen',
            condition=UnlessCondition(LaunchConfiguration('cpp')),
        ),
        Node(
            package='ros_tcp_endpoint_cpp',
            executable='default_server_endpoint',
            name='endpoint',
            parameters=[params],
            output='screen',
            condition=IfCondition(LaunchConfiguration('cpp')),
        ),
        Node(
            package='aurix_arm',
            executable='arm_a_commander',
            name='arm_a_commander',
            parameters=[params],
            output='screen',
        ),
        Node(
            package='aurix_monitor',
            executable='cell_monitor',
            name='cell_monitor',
            parameters=[params],
            output='screen',
            condition=IfCondition(LaunchConfiguration('monitor')),
        ),
    ])
