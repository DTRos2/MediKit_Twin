#include "ros_tcp_endpoint_cpp/session.hpp"

#include <unistd.h>

#include <utility>

namespace ros_tcp_endpoint_cpp
{

Session::Session(int fd)
: fd_(fd) {}

Session::~Session()
{
  close();
  if (writer_.joinable()) {writer_.join();}
  ::close(fd_);
}

void Session::start_writer()
{
  writer_ = std::thread(&Session::writer_loop, this);
}

void Session::push(Bytes data)
{
  {
    std::lock_guard<std::mutex> lock(mutex_);
    queue_.push_back(std::move(data));
  }
  cv_.notify_one();
}

void Session::close()
{
  halt_ = true;
  ::shutdown(fd_, SHUT_RDWR);  // 블록된 recv/send 를 깨운다
  cv_.notify_all();
}

void Session::writer_loop()
{
  while (!halt_) {
    Bytes item;
    {
      std::unique_lock<std::mutex> lock(mutex_);
      cv_.wait(lock, [this] {return halt_ || !queue_.empty();});
      if (halt_) {break;}
      item = std::move(queue_.front());
      queue_.pop_front();
    }
    if (!send_all(fd_, item.data(), item.size())) {
      halt_ = true;
      break;
    }
  }
}

}  // namespace ros_tcp_endpoint_cpp
