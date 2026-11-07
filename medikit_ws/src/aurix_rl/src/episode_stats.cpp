#include "aurix_rl/episode_stats.hpp"

#include <algorithm>

namespace aurix_rl
{

EpisodeStats::EpisodeStats(std::size_t window)
: window_(std::max<std::size_t>(window, 1)) {}

void EpisodeStats::record(float total_reward, bool success)
{
  entries_.push_back({total_reward, success});
  if (entries_.size() > window_) {
    entries_.pop_front();
  }
  ++total_;
}

float EpisodeStats::success_rate() const
{
  if (entries_.empty()) {
    return 0.0f;
  }
  const auto successes = std::count_if(
    entries_.begin(), entries_.end(), [](const Entry & e) {return e.success;});
  return static_cast<float>(successes) / static_cast<float>(entries_.size());
}

float EpisodeStats::mean_reward() const
{
  if (entries_.empty()) {
    return 0.0f;
  }
  float sum = 0.0f;
  for (const Entry & entry : entries_) {
    sum += entry.reward;
  }
  return sum / static_cast<float>(entries_.size());
}

}  // namespace aurix_rl
