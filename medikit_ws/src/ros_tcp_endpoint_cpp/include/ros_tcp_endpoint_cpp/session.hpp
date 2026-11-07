#pragma once

#include <atomic>
#include <condition_variable>
#include <deque>
#include <mutex>
#include <thread>

#include "ros_tcp_endpoint_cpp/protocol.hpp"

namespace ros_tcp_endpoint_cpp
{

// 연결된 소켓 하나와 송신 큐. ROS 를 알지 못한다.
class Session
{
public:
  explicit Session(int fd);
  ~Session();

  Session(const Session &) = delete;
  Session & operator=(const Session &) = delete;

  void start_writer();
  void push(Bytes data);
  void close();
  bool closed() const {return halt_;}
  int fd() const {return fd_;}

private:
  void writer_loop();

  int fd_;
  std::atomic<bool> halt_{false};
  std::mutex mutex_;
  std::condition_variable cv_;
  std::deque<Bytes> queue_;
  std::thread writer_;
};

}  // namespace ros_tcp_endpoint_cpp
