#include <gtest/gtest.h>

#include "aurix_rl/episode_stats.hpp"

using aurix_rl::EpisodeStats;

TEST(EpisodeStats, EmptyIsZero)
{
  EpisodeStats stats(10);
  EXPECT_EQ(stats.total_episodes(), 0u);
  EXPECT_FLOAT_EQ(stats.success_rate(), 0.0f);
  EXPECT_FLOAT_EQ(stats.mean_reward(), 0.0f);
}

TEST(EpisodeStats, ComputesRateAndMean)
{
  EpisodeStats stats(10);
  stats.record(10.0f, true);
  stats.record(-2.0f, false);
  stats.record(4.0f, true);
  stats.record(0.0f, false);
  EXPECT_EQ(stats.total_episodes(), 4u);
  EXPECT_FLOAT_EQ(stats.success_rate(), 0.5f);
  EXPECT_FLOAT_EQ(stats.mean_reward(), 3.0f);
}

TEST(EpisodeStats, WindowDropsOldestButKeepsTotal)
{
  EpisodeStats stats(2);
  stats.record(1.0f, false);
  stats.record(2.0f, true);
  stats.record(3.0f, true);
  EXPECT_EQ(stats.total_episodes(), 3u);
  EXPECT_FLOAT_EQ(stats.success_rate(), 1.0f);
  EXPECT_FLOAT_EQ(stats.mean_reward(), 2.5f);
}
