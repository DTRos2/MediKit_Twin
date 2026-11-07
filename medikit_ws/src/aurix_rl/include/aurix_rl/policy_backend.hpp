#pragma once

#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <vector>

namespace aurix_rl
{

/// 관측을 받아 행동을 고르는 정책의 공통 인터페이스.
/// 무작위, 정지, 학습된 모델(ONNX, LibTorch)을 같은 방식으로 끼우기 위한 확장점이며
/// ROS를 모른다. 노드는 어떤 백엔드가 들어 있는지 신경 쓰지 않는다.
class PolicyBackend
{
public:
  virtual ~PolicyBackend() = default;

  virtual std::string name() const = 0;

  /// action_size개의 행동을 -1~1 범위로 돌려준다. 관측 길이는 백엔드가 검증한다.
  virtual std::vector<float> select_action(
    const std::vector<float> & observation, std::size_t action_size) = 0;

  /// 행동 하나의 결과로 받은 보상. 온라인으로 학습하는 백엔드만 필요하다.
  virtual void on_step_reward(float /*reward*/) {}

  /// 에피소드가 끝났을 때 누적 보상과 성공 여부.
  virtual void on_episode_finished(float /*total_reward*/, bool /*success*/) {}
};

/// kind: "random" 또는 "zero". 알 수 없는 이름이면 std::invalid_argument를 던진다.
std::unique_ptr<PolicyBackend> make_policy_backend(const std::string & kind, std::uint64_t seed);

}  // namespace aurix_rl
