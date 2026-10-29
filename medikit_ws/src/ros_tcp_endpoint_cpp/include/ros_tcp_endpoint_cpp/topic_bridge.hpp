#pragma once

#include <functional>
#include <map>
#include <mutex>
#include <string>

#include "rclcpp/generic_publisher.hpp"
#include "rclcpp/generic_subscription.hpp"
#include "rclcpp/rclcpp.hpp"
#include "ros_tcp_endpoint_cpp/protocol.hpp"

namespace ros_tcp_endpoint_cpp
{

// Unity 에서 지정한 타입의 토픽을 직렬화된 바이트 그대로 ROS 와 중계한다.
// TCP 를 알지 못한다. ROS → Unity 방향은 SendFn 으로 넘긴다.
class TopicBridge
{
public:
  using SendFn = std::function<void(const std::string & topic, const Bytes & data)>;

  TopicBridge(rclcpp::Node & node, SendFn send_to_unity);

  // unity_type 은 "pkg/Type" 형식. 이미 등록된 토픽은 새 설정으로 교체한다.
  void add_publisher(
    const std::string & topic, const std::string & unity_type, size_t depth, bool latch);
  void add_subscriber(const std::string & topic, const std::string & unity_type);

  // Unity → ROS. 등록된 퍼블리셔가 없으면 false.
  bool publish(const std::string & topic, const Bytes & data);

  // {"topics": [...], "types": [...]} 형태의 JSON
  std::string topic_list_json();

private:
  rclcpp::Node & node_;
  SendFn send_;
  std::mutex mutex_;
  std::map<std::string, rclcpp::GenericPublisher::SharedPtr> publishers_;
  std::map<std::string, rclcpp::GenericSubscription::SharedPtr> subscribers_;
  std::map<std::string, std::string> types_;  // topic -> Unity 형식 타입 이름
};

}  // namespace ros_tcp_endpoint_cpp
