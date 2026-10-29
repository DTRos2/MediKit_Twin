#pragma once

#include <cstddef>
#include <deque>

namespace aurix_rl
{

/// 최근 N개 에피소드의 성공률과 평균 보상. 학습이 진행되는지 로그로 확인하는 데 쓴다.
class EpisodeStats
{
public:
  explicit EpisodeStats(std::size_t window);

  void record(float total_reward, bool success);

  std::size_t total_episodes() const {return total_;}

  /// 창 안의 에피소드가 없으면 0.
  float success_rate() const;
  float mean_reward() const;

private:
  struct Entry
  {
    float reward;
    bool success;
  };

  std::size_t window_;
  std::size_t total_{0};
  std::deque<Entry> entries_;
};

}  // namespace aurix_rl
