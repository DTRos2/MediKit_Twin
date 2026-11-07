#pragma once

#include <string>
#include <vector>

#include "aurix_rl/policy_backend.hpp"

namespace aurix_rl
{

/// 항상 0(정지)을 돌려준다. 통신 연결과 관측 수신만 확인하고 싶을 때 쓴다.
class ZeroPolicyBackend : public PolicyBackend
{
public:
  std::string name() const override {return "zero";}

  std::vector<float> select_action(
    const std::vector<float> & /*observation*/, std::size_t action_size) override
  {
    return std::vector<float>(action_size, 0.0f);
  }
};

}  // namespace aurix_rl
