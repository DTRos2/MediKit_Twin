#include <gtest/gtest.h>

#include <algorithm>
#include <stdexcept>

#include "aurix_rl/policy_backend.hpp"

using aurix_rl::make_policy_backend;

TEST(PolicyBackend, RandomStaysInRangeAndHasRequestedSize)
{
  auto backend = make_policy_backend("random", 1);
  const std::vector<float> observation(10, 0.0f);
  for (int i = 0; i < 100; ++i) {
    const auto action = backend->select_action(observation, 7);
    ASSERT_EQ(action.size(), 7u);
    EXPECT_TRUE(std::all_of(action.begin(), action.end(), [](float v) {return v >= -1.0f && v <= 1.0f;}));
  }
}

TEST(PolicyBackend, RandomIsDeterministicForSameSeed)
{
  auto first = make_policy_backend("random", 42);
  auto second = make_policy_backend("random", 42);
  const std::vector<float> observation;
  for (int i = 0; i < 20; ++i) {
    EXPECT_EQ(first->select_action(observation, 6), second->select_action(observation, 6));
  }
}

TEST(PolicyBackend, DifferentSeedsGiveDifferentActions)
{
  auto first = make_policy_backend("random", 1);
  auto second = make_policy_backend("random", 2);
  const std::vector<float> observation;
  EXPECT_NE(first->select_action(observation, 6), second->select_action(observation, 6));
}

TEST(PolicyBackend, ZeroReturnsAllZeros)
{
  auto backend = make_policy_backend("zero", 0);
  const auto action = backend->select_action({1.0f, 2.0f}, 4);
  EXPECT_EQ(action, std::vector<float>(4, 0.0f));
}

TEST(PolicyBackend, UnknownKindThrows)
{
  EXPECT_THROW(make_policy_backend("nope", 0), std::invalid_argument);
}
