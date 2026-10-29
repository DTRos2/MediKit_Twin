#pragma once

#include <memory>
#include <string>

#include "geometry_msgs/msg/pose_stamped.hpp"
#include "moveit/move_group_interface/move_group_interface.h"
#include "rclcpp/rclcpp.hpp"
#include "sensor_msgs/msg/joint_state.hpp"

namespace aurix_rl
{

/// 목표 자세(PoseStamped)를 받아 MoveIt으로 충돌 없는 경로를 계획하고, 마지막 지점의 관절 각도만
/// /arm_a/joint_command(JointState)로 보낸다. 목표까지 움직이는 동작은 Unity의 ArmController가
/// 맡는다는 기존 규칙(INTERFACES.md 5장)을 그대로 따른다. 학습한 정책과 비교할 기준선(baseline)으로 쓴다.
/// MoveGroupInterface는 shared_from_this가 필요해서 생성 직후 initialize()를 따로 부른다.
class MoveItGoalNode : public rclcpp::Node
{
public:
  explicit MoveItGoalNode(const rclcpp::NodeOptions & options);

  void initialize();

private:
  void on_goal(const geometry_msgs::msg::PoseStamped::SharedPtr goal);

  std::string planning_group_;
  std::string end_effector_link_;
  double planning_time_s_{5.0};
  std::string goal_topic_;
  std::string command_topic_;

  std::shared_ptr<moveit::planning_interface::MoveGroupInterface> group_;
  rclcpp::Subscription<geometry_msgs::msg::PoseStamped>::SharedPtr goal_subscription_;
  rclcpp::Publisher<sensor_msgs::msg::JointState>::SharedPtr command_publisher_;
};

}  // namespace aurix_rl
