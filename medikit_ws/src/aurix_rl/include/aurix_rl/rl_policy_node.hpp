#pragma once

#include <memory>
#include <string>

#include "rclcpp/rclcpp.hpp"
#include "std_msgs/msg/float32.hpp"
#include "std_msgs/msg/float32_multi_array.hpp"

#include "aurix_rl/episode_stats.hpp"
#include "aurix_rl/policy_backend.hpp"

namespace aurix_rl
{

/// Unity(RosPolicy)가 발행한 관측을 받아 정책 백엔드로 행동을 골라 돌려준다.
/// 보상과 에피소드 결과도 받아 백엔드에 넘기고 성공률을 로그로 남긴다.
/// 토픽 이름과 정책 종류는 파라미터라 FR5, UR3 어느 로봇에도 코드 수정 없이 쓴다.
class RlPolicyNode : public rclcpp::Node
{
public:
  explicit RlPolicyNode(const rclcpp::NodeOptions & options = rclcpp::NodeOptions());

  /// 시험이나 다른 정책(학습된 모델)을 직접 끼울 때 쓴다.
  RlPolicyNode(std::unique_ptr<PolicyBackend> backend, const rclcpp::NodeOptions & options);

private:
  void declare_and_wire();
  void on_observation(const std_msgs::msg::Float32MultiArray::SharedPtr message);
  void on_step_reward(const std_msgs::msg::Float32::SharedPtr message);
  void on_episode_result(const std_msgs::msg::Float32MultiArray::SharedPtr message);

  std::unique_ptr<PolicyBackend> backend_;
  std::unique_ptr<EpisodeStats> stats_;
  std::size_t action_size_{0};
  std::size_t log_interval_episodes_{10};

  rclcpp::Publisher<std_msgs::msg::Float32MultiArray>::SharedPtr action_publisher_;
  rclcpp::Subscription<std_msgs::msg::Float32MultiArray>::SharedPtr observation_subscription_;
  rclcpp::Subscription<std_msgs::msg::Float32>::SharedPtr reward_subscription_;
  rclcpp::Subscription<std_msgs::msg::Float32MultiArray>::SharedPtr episode_subscription_;
};

}  // namespace aurix_rl
