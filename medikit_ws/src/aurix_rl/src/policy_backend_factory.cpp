#include <memory>
#include <stdexcept>
#include <string>

#include "aurix_rl/policy_backend.hpp"
#include "aurix_rl/random_policy_backend.hpp"
#include "aurix_rl/zero_policy_backend.hpp"

namespace aurix_rl
{

std::unique_ptr<PolicyBackend> make_policy_backend(const std::string & kind, std::uint64_t seed)
{
  if (kind == "random") {
    return std::make_unique<RandomPolicyBackend>(seed);
  }
  if (kind == "zero") {
    return std::make_unique<ZeroPolicyBackend>();
  }
  throw std::invalid_argument("알 수 없는 정책 백엔드: '" + kind + "' (random, zero 중 하나)");
}

}  // namespace aurix_rl
