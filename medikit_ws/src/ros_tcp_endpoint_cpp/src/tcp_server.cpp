#include "ros_tcp_endpoint_cpp/tcp_server.hpp"

#include <arpa/inet.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <poll.h>
#include <unistd.h>

#include <cstring>
#include <exception>
#include <stdexcept>
#include <utility>

namespace ros_tcp_endpoint_cpp
{

TcpServer::TcpServer(std::string ip, int port, Callbacks callbacks)
: ip_(std::move(ip)), port_(port), cb_(std::move(callbacks)) {}

TcpServer::~TcpServer()
{
  stop();
}

void TcpServer::start()
{
  listen_fd_ = ::socket(AF_INET, SOCK_STREAM, 0);
  if (listen_fd_ < 0) {throw std::runtime_error("socket() failed");}
  int one = 1;
  ::setsockopt(listen_fd_, SOL_SOCKET, SO_REUSEADDR, &one, sizeof(one));

  sockaddr_in addr{};
  addr.sin_family = AF_INET;
  addr.sin_port = htons(static_cast<uint16_t>(port_));
  if (::inet_pton(AF_INET, ip_.c_str(), &addr.sin_addr) != 1) {
    throw std::runtime_error("invalid ROS_IP: " + ip_);
  }
  if (::bind(listen_fd_, reinterpret_cast<sockaddr *>(&addr), sizeof(addr)) < 0) {
    throw std::runtime_error(
            "bind() failed on " + ip_ + ":" + std::to_string(port_) + ": " + std::strerror(errno));
  }
  if (::listen(listen_fd_, 10) < 0) {throw std::runtime_error("listen() failed");}

  running_ = true;
  accept_thread_ = std::thread(&TcpServer::accept_loop, this);
}

void TcpServer::stop()
{
  if (!running_.exchange(false)) {return;}
  if (accept_thread_.joinable()) {accept_thread_.join();}
  ::close(listen_fd_);

  std::list<ClientThread> clients;
  {
    std::lock_guard<std::mutex> lock(clients_mutex_);
    for (auto & weak : sessions_) {
      if (auto s = weak.lock()) {s->close();}
    }
    clients.swap(clients_);
  }
  for (auto & c : clients) {
    if (c.thread.joinable()) {c.thread.join();}
  }
}

void TcpServer::reap_finished()
{
  std::lock_guard<std::mutex> lock(clients_mutex_);
  for (auto it = clients_.begin(); it != clients_.end();) {
    if (it->done->load()) {
      it->thread.join();
      it = clients_.erase(it);
    } else {
      ++it;
    }
  }
  sessions_.remove_if([](const std::weak_ptr<Session> & w) {return w.expired();});
}

void TcpServer::accept_loop()
{
  while (running_) {
    pollfd pfd{listen_fd_, POLLIN, 0};
    if (::poll(&pfd, 1, 500) <= 0) {
      reap_finished();
      continue;
    }

    sockaddr_in peer{};
    socklen_t len = sizeof(peer);
    int fd = ::accept(listen_fd_, reinterpret_cast<sockaddr *>(&peer), &len);
    if (fd < 0) {continue;}
    char ip[INET_ADDRSTRLEN] = {0};
    ::inet_ntop(AF_INET, &peer.sin_addr, ip, sizeof(ip));
    int one = 1;
    ::setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, &one, sizeof(one));

    auto session = std::make_shared<Session>(fd);
    auto done = std::make_shared<std::atomic<bool>>(false);
    std::string peer_str(ip);
    std::lock_guard<std::mutex> lock(clients_mutex_);
    sessions_.push_back(session);
    clients_.push_back({std::thread([this, session, done, peer_str] {
          client_loop(session, peer_str);
          *done = true;
        }), done});
  }
}

void TcpServer::client_loop(const SessionPtr & session, const std::string & peer)
{
  session->start_writer();
  if (cb_.on_connect) {cb_.on_connect(session, peer);}

  std::string dest;
  Bytes payload;
  while (!session->closed() && read_frame(session->fd(), dest, payload)) {
    try {
      if (cb_.on_message) {cb_.on_message(session, dest, payload);}
    } catch (const std::exception & e) {
      if (cb_.on_error) {cb_.on_error("'" + dest + "' 처리 실패: " + e.what());}
    }
  }

  session->close();
  if (cb_.on_disconnect) {cb_.on_disconnect(session, peer);}
}

}  // namespace ros_tcp_endpoint_cpp
