#include <memory>

#include "rclcpp/rclcpp.hpp"
#include "ros_tcp_endpoint_cpp/endpoint_server.hpp"

int main(int argc, char ** argv)
{
  rclcpp::init(argc, argv);
  auto node = std::make_shared<ros_tcp_endpoint_cpp::EndpointServer>("endpoint");
  node->start();

  rclcpp::executors::MultiThreadedExecutor executor;
  executor.add_node(node);
  executor.spin();

  node->stop();
  rclcpp::shutdown();
  return 0;
}
