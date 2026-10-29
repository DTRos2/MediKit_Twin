cmake_minimum_required(VERSION 3.8)
project(ros_tcp_endpoint_cpp)

if(CMAKE_COMPILER_IS_GNUCXX OR CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wall -Wextra -Wpedantic)
endif()

find_package(ament_cmake REQUIRED)
find_package(rclcpp REQUIRED)
find_package(Threads REQUIRED)

# ROS 비의존: 소켓 세션, TCP 서버, 명령 분배
add_library(tcp_core STATIC
  src/session.cpp
  src/tcp_server.cpp
  src/command_handler.cpp)
target_include_directories(tcp_core PUBLIC include)
target_compile_features(tcp_core PUBLIC cxx_std_17)
target_link_libraries(tcp_core PUBLIC Threads::Threads)
set_target_properties(tcp_core PROPERTIES POSITION_INDEPENDENT_CODE ON)

# ROS 연동: 토픽 중계와 노드 조립
add_library(ros_bridge STATIC
  src/topic_bridge.cpp
  src/endpoint_server.cpp)
target_link_libraries(ros_bridge PUBLIC tcp_core)
ament_target_dependencies(ros_bridge PUBLIC rclcpp)

add_executable(default_server_endpoint src/main.cpp)
target_link_libraries(default_server_endpoint ros_bridge)

install(TARGETS default_server_endpoint DESTINATION lib/${PROJECT_NAME})

ament_package()
