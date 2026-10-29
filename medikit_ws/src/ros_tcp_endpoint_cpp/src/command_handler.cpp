#include "ros_tcp_endpoint_cpp/command_handler.hpp"

#include <utility>

namespace ros_tcp_endpoint_cpp
{

void CommandHandler::add(const std::string & name, Handler handler)
{
  handlers_[name] = std::move(handler);
}

bool CommandHandler::dispatch(const std::string & dest, const Bytes & payload) const
{
  auto it = handlers_.find(dest.substr(2));
  if (it == handlers_.end()) {return false;}
  it->second(parse_flat_json(std::string(payload.begin(), payload.end())));
  return true;
}

}  // namespace ros_tcp_endpoint_cpp
