#include "ros_tcp_endpoint_cpp/topic_bridge.hpp"

#include <cstring>
#include <utility>

namespace ros_tcp_endpoint_cpp
{

TopicBridge::TopicBridge(rclcpp::Node & node, SendFn send_to_unity)
: node_(node), send_(std::move(send_to_unity)) {}

void TopicBridge::add_publisher(
  const std::string & topic, const std::string & unity_type, size_t depth, bool latch)
{
  rclcpp::QoS qos(depth);
  if (latch) {qos.transient_local();}
  auto pub = node_.create_generic_publisher(topic, to_ros_type(unity_type), qos);

  std::lock_guard<std::mutex> lock(mutex_);
  publishers_[topic] = pub;
  types_[topic] = unity_type;
}

void TopicBridge::add_subscriber(const std::string & topic, const std::string & unity_type)
{
  auto sub = node_.create_generic_subscription(
    topic, to_ros_type(unity_type), rclcpp::QoS(10),
    [this, topic](std::shared_ptr<rclcpp::SerializedMessage> msg) {
      const auto & raw = msg->get_rcl_serialized_message();
      send_(topic, Bytes(raw.buffer, raw.buffer + raw.buffer_length));
    });

  std::lock_guard<std::mutex> lock(mutex_);
  subscribers_[topic] = sub;
  types_[topic] = unity_type;
}

bool TopicBridge::publish(const std::string & topic, const Bytes & data)
{
  rclcpp::GenericPublisher::SharedPtr pub;
  {
    std::lock_guard<std::mutex> lock(mutex_);
    auto it = publishers_.find(topic);
    if (it == publishers_.end()) {return false;}
    pub = it->second;
  }
  rclcpp::SerializedMessage serialized(data.size());
  auto & raw = serialized.get_rcl_serialized_message();
  std::memcpy(raw.buffer, data.data(), data.size());
  raw.buffer_length = data.size();
  pub->publish(serialized);
  return true;
}

std::string TopicBridge::topic_list_json()
{
  std::string topics, types;
  auto graph = node_.get_topic_names_and_types();
  std::lock_guard<std::mutex> lock(mutex_);
  for (const auto & [topic, ros_types] : graph) {
    if (ros_types.empty()) {continue;}
    auto reg = types_.find(topic);
    const std::string type = reg != types_.end() ? reg->second : to_unity_type(ros_types[0]);
    if (!topics.empty()) {topics += ", "; types += ", ";}
    topics += "\"" + json_escape(topic) + "\"";
    types += "\"" + json_escape(type) + "\"";
  }
  return "{\"topics\": [" + topics + "], \"types\": [" + types + "]}";
}

}  // namespace ros_tcp_endpoint_cpp
