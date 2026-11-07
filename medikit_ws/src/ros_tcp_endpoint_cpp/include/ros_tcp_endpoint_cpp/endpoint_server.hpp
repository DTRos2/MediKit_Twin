#pragma once

#include <atomic>
#include <memory>
#include <mutex>
#include <string>

#include "rclcpp/rclcpp.hpp"
#include "ros_tcp_endpoint_cpp/command_handler.hpp"
#include "ros_tcp_endpoint_cpp/tcp_server.hpp"
#include "ros_tcp_endpoint_cpp/topic_bridge.hpp"

namespace ros_tcp_endpoint_cpp
{

// TcpServer · CommandHandler · TopicBridge 를 조립하는 ROS 노드.
class EndpointServer : public rclcpp::Node
{
public:
  explicit EndpointServer(const std::string & node_name = "endpoint");
  ~EndpointServer() override;

  void start();
  void stop();

private:
  void register_commands();
  void on_connect(const TcpServer::SessionPtr & session, const std::string & peer);
  void on_message(const TcpServer::SessionPtr & session, const std::string & dest,
    const Bytes & payload);
  void on_disconnect(const TcpServer::SessionPtr & session, const std::string & peer);

  void send_to_unity(const std::string & topic, const Bytes & data);
  void send_log(const std::string & cmd, const std::string & text);
  TcpServer::SessionPtr active_session();

  TopicBridge bridge_;
  CommandHandler commands_;
  std::unique_ptr<TcpServer> server_;

  std::mutex active_mutex_;
  TcpServer::SessionPtr active_;  // 가장 최근에 연결된 세션이 송신 대상

  // __request / __response 직후에 오는 메시지 한 개는 서비스 메시지
  std::atomic<bool> pending_service_{false};
};

}  // namespace ros_tcp_endpoint_cpp
