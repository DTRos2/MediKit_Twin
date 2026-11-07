#include <memory>

#include "rclcpp/rclcpp.hpp"

#include "moveit_goal_node.hpp"

int main(int argc, char ** argv)
{
  rclcpp::init(argc, argv);

  auto node = std::make_shared<aurix_rl::MoveItGoalNode>(rclcpp::NodeOptions());
  node->initialize();

  // MoveGroupInterface가 내부에서 서비스 응답을 기다리므로 단일 스레드 spin으로는 멈춘다.
  rclcpp::executors::MultiThreadedExecutor executor;
  executor.add_node(node);
  executor.spin();

  rclcpp::shutdown();
  return 0;
}
