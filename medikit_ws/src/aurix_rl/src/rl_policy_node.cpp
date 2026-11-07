#include "aurix_rl/rl_policy_node.hpp"

#include <utility>

namespace aurix_rl
{

RlPolicyNode::RlPolicyNode(const rclcpp::NodeOptions & options)
: rclcpp::Node("rl_policy_node", options)
{
  const std::string policy = declare_parameter<std::string>("policy", "random");
  const auto seed = static_cast<std::uint64_t>(declare_parameter<int>("seed", 7));
  backend_ = make_policy_backend(policy, seed);
  declare_and_wire();
}

RlPolicyNode::RlPolicyNode(std::unique_ptr<PolicyBackend> backend, const rclcpp::NodeOptions & options)
: rclcpp::Node("rl_policy_node", options), backend_(std::move(backend))
{
  declare_and_wire();
}

void RlPolicyNode::declare_and_wire()
{
  const auto observation_topic = declare_parameter<std::string>("observation_topic", "/rl/observation");
  const auto action_topic = declare_parameter<std::string>("action_topic", "/rl/action");
  const auto reward_topic = declare_parameter<std::string>("reward_topic", "/rl/step_reward");
  const auto episode_topic = declare_parameter<std::string>("episode_topic", "/rl/episode_result");

  // 행동 크기 = 관절 수 + (그리퍼가 있으면 1). Unity 환경의 ActionSize와 같아야 한다.
  action_size_ = static_cast<std::size_t>(declare_parameter<int>("action_size", 7));
  log_interval_episodes_ = static_cast<std::size_t>(declare_parameter<int>("log_interval_episodes", 10));
  stats_ = std::make_unique<EpisodeStats>(
    static_cast<std::size_t>(declare_parameter<int>("stats_window", 50)));

  action_publisher_ = create_publisher<std_msgs::msg::Float32MultiArray>(action_topic, 10);
  observation_subscription_ = create_subscription<std_msgs::msg::Float32MultiArray>(
    observation_topic, 10,
    [this](std_msgs::msg::Float32MultiArray::SharedPtr message) {on_observation(message);});
  reward_subscription_ = create_subscription<std_msgs::msg::Float32>(
    reward_topic, 10, [this](std_msgs::msg::Float32::SharedPtr message) {on_step_reward(message);});
  episode_subscription_ = create_subscription<std_msgs::msg::Float32MultiArray>(
    episode_topic, 10,
    [this](std_msgs::msg::Float32MultiArray::SharedPtr message) {on_episode_result(message);});

  RCLCPP_INFO(
    get_logger(), "정책 '%s' 시작 (행동 %zu개): %s -> %s",
    backend_->name().c_str(), action_size_, observation_topic.c_str(), action_topic.c_str());
}

void RlPolicyNode::on_observation(const std_msgs::msg::Float32MultiArray::SharedPtr message)
{
  std_msgs::msg::Float32MultiArray action;
  action.data = backend_->select_action(message->data, action_size_);
  action_publisher_->publish(action);
}

void RlPolicyNode::on_step_reward(const std_msgs::msg::Float32::SharedPtr message)
{
  backend_->on_step_reward(message->data);
}

void RlPolicyNode::on_episode_result(const std_msgs::msg::Float32MultiArray::SharedPtr message)
{
  // 인자는 [누적 보상, 성공(1/0)]. 형식이 다르면 무시한다.
  if (message->data.size() < 2) {
    RCLCPP_WARN(get_logger(), "에피소드 결과 형식이 다르다 (값 %zu개)", message->data.size());
    return;
  }

  const float total_reward = message->data[0];
  const bool success = message->data[1] > 0.5f;
  backend_->on_episode_finished(total_reward, success);
  stats_->record(total_reward, success);

  if (log_interval_episodes_ > 0 && stats_->total_episodes() % log_interval_episodes_ == 0) {
    RCLCPP_INFO(
      get_logger(), "에피소드 %zu: 최근 성공률 %.0f%%, 평균 보상 %.2f",
      stats_->total_episodes(), stats_->success_rate() * 100.0f, stats_->mean_reward());
  }
}

}  // namespace aurix_rl
