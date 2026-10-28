#pragma once

#include <atomic>
#include <functional>
#include <list>
#include <memory>
#include <mutex>
#include <string>
#include <thread>

#include "ros_tcp_endpoint_cpp/protocol.hpp"
#include "ros_tcp_endpoint_cpp/session.hpp"

namespace ros_tcp_endpoint_cpp
{

// 접속 수락과 클라이언트별 수신 스레드만 관리한다. ROS 를 알지 못한다.
class TcpServer
{
public:
  using SessionPtr = std::shared_ptr<Session>;

  struct Callbacks
  {
    std::function<void(const SessionPtr &, const std::string & peer)> on_connect;
    std::function<void(const SessionPtr &, const std::string & dest, const Bytes & payload)>
    on_message;
    std::function<void(const SessionPtr &, const std::string & peer)> on_disconnect;
    std::function<void(const std::string &)> on_error;  // 메시지 처리 중 예외 등
  };

  TcpServer(std::string ip, int port, Callbacks callbacks);
  ~TcpServer();

  // bind/listen 실패 시 std::runtime_error
  void start();
  void stop();

  const std::string & ip() const {return ip_;}
  int port() const {return port_;}

private:
  struct ClientThread
  {
    std::thread thread;
    std::shared_ptr<std::atomic<bool>> done;
  };

  void accept_loop();
  void client_loop(const SessionPtr & session, const std::string & peer);
  void reap_finished();

  std::string ip_;
  int port_;
  Callbacks cb_;
  int listen_fd_{-1};
  std::atomic<bool> running_{false};
  std::thread accept_thread_;

  std::mutex clients_mutex_;
  std::list<ClientThread> clients_;
  std::list<std::weak_ptr<Session>> sessions_;
};

}  // namespace ros_tcp_endpoint_cpp
