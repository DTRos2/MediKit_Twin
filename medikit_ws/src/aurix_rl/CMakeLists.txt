cmake_minimum_required(VERSION 3.8)
project(aurix_rl)

if(CMAKE_COMPILER_IS_GNUCXX OR CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wall -Wextra -Wpedantic)
endif()

find_package(ament_cmake REQUIRED)
find_package(rclcpp REQUIRED)
find_package(std_msgs REQUIRED)
find_package(sensor_msgs REQUIRED)
find_package(geometry_msgs REQUIRED)
find_package(moveit_ros_planning_interface QUIET)

# ROS 비의존: 정책 백엔드와 통계. 단위 시험과 다른 프로그램(학습기)에서도 그대로 쓴다.
add_library(aurix_rl_core STATIC
  src/policy_backend_factory.cpp
  src/random_policy_backend.cpp
  src/episode_stats.cpp)
target_include_directories(aurix_rl_core PUBLIC
  $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>)
target_compile_features(aurix_rl_core PUBLIC cxx_std_17)
set_target_properties(aurix_rl_core PROPERTIES POSITION_INDEPENDENT_CODE ON)

# ROS 연동: Unity와 주고받는 토픽 처리
add_library(aurix_rl_node STATIC src/rl_policy_node.cpp)
target_link_libraries(aurix_rl_node PUBLIC aurix_rl_core)
ament_target_dependencies(aurix_rl_node PUBLIC rclcpp std_msgs)

add_executable(rl_policy_node src/rl_policy_main.cpp)
target_link_libraries(rl_policy_node aurix_rl_node)

set(INSTALL_TARGETS rl_policy_node)

if(moveit_ros_planning_interface_FOUND)
  add_executable(moveit_goal_node src/moveit_goal_node.cpp src/moveit_goal_main.cpp)
  target_include_directories(moveit_goal_node PRIVATE include)
  target_compile_features(moveit_goal_node PRIVATE cxx_std_17)
  ament_target_dependencies(moveit_goal_node
    rclcpp sensor_msgs geometry_msgs moveit_ros_planning_interface)
  list(APPEND INSTALL_TARGETS moveit_goal_node)
else()
  message(STATUS "moveit_ros_planning_interface를 찾지 못해 moveit_goal_node는 빌드하지 않는다.")
endif()

install(TARGETS ${INSTALL_TARGETS} DESTINATION lib/${PROJECT_NAME})
install(DIRECTORY config launch DESTINATION share/${PROJECT_NAME})

if(BUILD_TESTING)
  find_package(ament_cmake_gtest REQUIRED)
  ament_add_gtest(test_policy_backend test/test_policy_backend.cpp)
  target_link_libraries(test_policy_backend aurix_rl_core)
  ament_add_gtest(test_episode_stats test/test_episode_stats.cpp)
  target_link_libraries(test_episode_stats aurix_rl_core)
endif()

ament_package()
