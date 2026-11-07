#include "aurix_rl/random_policy_backend.hpp"

namespace aurix_rl
{

RandomPolicyBackend::RandomPolicyBackend(std::uint64_t seed)
: engine_(seed) {}

std::vector<float> RandomPolicyBackend::select_action(
  const std::vector<float> & /*observation*/, std::size_t action_size)
{
  std::vector<float> action(action_size);
  for (float & value : action) {
    value = distribution_(engine_);
  }
  return action;
}

}  // namespace aurix_rl
