#pragma once

#include <functional>
#include <map>
#include <string>

#include "ros_tcp_endpoint_cpp/protocol.hpp"

namespace ros_tcp_endpoint_cpp
{

// "__이름" 시스템 명령을 이름별 핸들러로 분배한다. 새 명령은 add() 만 하면 된다.
class CommandHandler
{
public:
  using Params = std::map<std::string, std::string>;
  using Handler = std::function<void(const Params &)>;

  void add(const std::string & name, Handler handler);

  // dest 가 "__" 로 시작하면 처리하고 true. 등록되지 않은 명령이면 false.
  // 핸들러나 JSON 파싱 오류는 예외로 전달된다.
  bool dispatch(const std::string & dest, const Bytes & payload) const;

  static bool is_command(const std::string & dest) {return dest.rfind("__", 0) == 0;}

private:
  std::map<std::string, Handler> handlers_;
};

}  // namespace ros_tcp_endpoint_cpp
