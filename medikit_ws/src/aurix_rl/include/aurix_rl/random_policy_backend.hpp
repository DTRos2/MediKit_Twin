#pragma once

#include <cstdint>
#include <random>
#include <string>
#include <vector>

#include "aurix_rl/policy_backend.hpp"

namespace aurix_rl
{

/// 균등 무작위 행동. 환경, 관측, 보상이 끝까지 도는지 학습 없이 확인하는 연기 시험용이다.
/// 시드가 같으면 같은 행동열이 나온다.
class RandomPolicyBackend : public PolicyBackend
{
public:
  explicit RandomPolicyBackend(std::uint64_t seed);

  std::string name() const override {return "random";}

  std::vector<float> select_action(
    const std::vector<float> & observation, std::size_t action_size) override;

private:
  std::mt19937_64 engine_;
  std::uniform_real_distribution<float> distribution_{-1.0f, 1.0f};
};

}  // namespace aurix_rl
