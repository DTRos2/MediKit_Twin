#include "moveit_goal_node.hpp"

namespace aurix_rl
{

MoveItGoalNode::MoveItGoalNode(const rclcpp::NodeOptions & options)
: rclcpp::Node("moveit_goal_node", options)
{
  // 로봇마다 MoveIt 설정의 계획 그룹 이름이 다르다 (FR5: arm, UR3: ur_manipulator 등).
  planning_group_ = declare_parameter<std::string>("planning_group", "arm");
  end_effector_link_ = declare_parameter<std::string>("end_effector_link", "");
  planning_time_s_ = declare_parameter<double>("planning_time", 5.0);
  goal_topic_ = declare_parameter<std::string>("goal_topic", "/arm_a/goal_pose");
  command_topic_ = declare_parameter<std::string>("joint_command_topic", "/arm_a/joint_command");
}

void MoveItGoalNode::initialize()
{
  group_ = std::make_shared<moveit::planning_interface::MoveGroupInterface>(
    shared_from_this(), planning_group_);
  group_->setPlanningTime(planning_time_s_);

  command_publisher_ = create_publisher<sensor_msgs::msg::JointState>(command_topic_, 10);
  goal_subscription_ = create_subscription<geometry_msgs::msg::PoseStamped>(
    goal_topic_, 10,
    [this](geometry_msgs::msg::PoseStamped::SharedPtr goal) {on_goal(goal);});

  RCLCPP_INFO(
    get_logger(), "MoveIt 목표 노드 시작: 그룹 '%s', %s -> %s",
    planning_group_.c_str(), goal_topic_.c_str(), command_topic_.c_str());
}

void MoveItGoalNode::on_goal(const geometry_msgs::msg::PoseStamped::SharedPtr goal)
{
  if (end_effector_link_.empty()) {
    group_->setPoseTarget(*goal);
  } else {
    group_->setPoseTarget(*goal, end_effector_link_);
  }

  moveit::planning_interface::MoveGroupInterface::Plan plan;
  const bool planned = group_->plan(plan) == moveit::core::MoveItErrorCode::SUCCESS;
  if (!planned || plan.trajectory_.joint_trajectory.points.empty()) {
    RCLCPP_WARN(get_logger(), "계획에 실패했다. 목표가 닿을 수 없거나 충돌이 있다.");
    return;
  }

  const auto & trajectory = plan.trajectory_.joint_trajectory;
  sensor_msgs::msg::JointState command;
  command.header.stamp = now();
  command.name = trajectory.joint_names;
  command.position = trajectory.points.back().positions;
  command_publisher_->publish(command);
}

}  // namespace aurix_rl
