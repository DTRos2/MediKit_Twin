#include "ros_tcp_endpoint_cpp/endpoint_server.hpp"

#include <algorithm>
#include <utility>

namespace ros_tcp_endpoint_cpp
{

EndpointServer::EndpointServer(const std::string & node_name)
: rclcpp::Node(node_name),
  bridge_(*this, [this](const std::string & t, const Bytes & d) {send_to_unity(t, d);})
{
  const auto ip = declare_parameter<std::string>("ROS_IP", "0.0.0.0");
  const auto port = static_cast<int>(declare_parameter<int64_t>("ROS_TCP_PORT", 10000));

  TcpServer::Callbacks cb;
  cb.on_connect = [this](const auto & s, const auto & p) {on_connect(s, p);};
  cb.on_message = [this](const auto & s, const auto & d, const auto & p) {on_message(s, d, p);};
  cb.on_disconnect = [this](const auto & s, const auto & p) {on_disconnect(s, p);};
  cb.on_error = [this](const std::string & e) {
      RCLCPP_ERROR(get_logger(), "%s", e.c_str());
      send_log("__error", e);
    };
  server_ = std::make_unique<TcpServer>(ip, port, std::move(cb));
  register_commands();
}

EndpointServer::~EndpointServer()
{
  stop();
}

void EndpointServer::start()
{
  server_->start();
  RCLCPP_INFO(get_logger(), "Starting server on %s:%d", server_->ip().c_str(), server_->port());
}

void EndpointServer::stop()
{
  server_->stop();
  std::lock_guard<std::mutex> lock(active_mutex_);
  active_.reset();
}

void EndpointServer::register_commands()
{
  commands_.add("publish", [this](const CommandHandler::Params & p) {
      const auto & topic = p.at("topic");
      const auto & type = p.at("message_name");
      if (topic.empty()) {send_log("__error", "Can't publish to a blank topic name!"); return;}
      size_t depth = p.count("queue_size") ? std::max<long>(1, std::stol(p.at("queue_size"))) : 10;
      bridge_.add_publisher(topic, type, depth, p.count("latch") && p.at("latch") == "true");
      RCLCPP_INFO(get_logger(), "RegisterPublisher(%s, %s) OK", topic.c_str(), type.c_str());
    });

  commands_.add("subscribe", [this](const CommandHandler::Params & p) {
      const auto & topic = p.at("topic");
      const auto & type = p.at("message_name");
      if (topic.empty()) {send_log("__error", "Can't subscribe to a blank topic name!"); return;}
      bridge_.add_subscriber(topic, type);
      RCLCPP_INFO(get_logger(), "RegisterSubscriber(%s, %s) OK", topic.c_str(), type.c_str());
    });

  commands_.add("topic_list", [this](const CommandHandler::Params &) {
      if (auto s = active_session()) {
        s->push(make_command("__topic_list", bridge_.topic_list_json()));
      }
    });

  auto unsupported = [this](const std::string & name) {
      return [this, name](const CommandHandler::Params &) {
               const std::string msg = "Services are not supported by the C++ endpoint (" + name + ")";
               RCLCPP_ERROR(get_logger(), "%s", msg.c_str());
               send_log("__error", msg);
             };
    };
  commands_.add("ros_service", unsupported("ros_service"));
  commands_.add("unity_service", unsupported("unity_service"));
  commands_.add("request", [this](const CommandHandler::Params &) {pending_service_ = true;});
  commands_.add("response", [this](const CommandHandler::Params &) {pending_service_ = true;});
}

void EndpointServer::on_connect(const TcpServer::SessionPtr & session, const std::string & peer)
{
  RCLCPP_INFO(get_logger(), "Connection from %s", peer.c_str());
  // 핸드셰이크: 프로토콜 버전과 메타데이터(JSON 문자열 안의 JSON)
  session->push(make_command(
      "__handshake",
      "{\"version\": \"v0.7.0\", \"metadata\": \"{\\\"protocol\\\": \\\"ROS2\\\"}\"}"));
  std::lock_guard<std::mutex> lock(active_mutex_);
  active_ = session;
}

void EndpointServer::on_disconnect(const TcpServer::SessionPtr & session, const std::string & peer)
{
  {
    std::lock_guard<std::mutex> lock(active_mutex_);
    if (active_ == session) {active_.reset();}
  }
  RCLCPP_INFO(get_logger(), "Disconnected from %s", peer.c_str());
}

void EndpointServer::on_message(
  const TcpServer::SessionPtr &, const std::string & dest, const Bytes & payload)
{
  if (pending_service_.exchange(false)) {
    const std::string msg =
      "Services are not supported by the C++ endpoint (message to '" + dest + "' dropped)";
    RCLCPP_ERROR(get_logger(), "%s", msg.c_str());
    send_log("__error", msg);
    return;
  }
  if (dest.empty()) {return;}  // keepalive

  if (CommandHandler::is_command(dest)) {
    if (!commands_.dispatch(dest, payload)) {
      send_log("__error", "Don't understand SysCommand.'" + dest + "'");
    }
    return;
  }

  if (!bridge_.publish(dest, payload)) {
    const std::string msg = "Not registered to publish topic '" + dest + "'!";
    RCLCPP_ERROR(get_logger(), "%s", msg.c_str());
    send_log("__error", msg);
  }
}

TcpServer::SessionPtr EndpointServer::active_session()
{
  std::lock_guard<std::mutex> lock(active_mutex_);
  return active_;
}

void EndpointServer::send_to_unity(const std::string & topic, const Bytes & data)
{
  if (auto s = active_session()) {s->push(make_frame(topic, data.data(), data.size()));}
}

void EndpointServer::send_log(const std::string & cmd, const std::string & text)
{
  if (auto s = active_session()) {
    s->push(make_command(cmd, "{\"text\": \"" + json_escape(text) + "\"}"));
  }
}

}  // namespace ros_tcp_endpoint_cpp
