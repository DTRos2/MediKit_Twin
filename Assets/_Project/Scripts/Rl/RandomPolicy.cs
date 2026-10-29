using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>
    /// 무작위 정책. 환경, 관측, 보상이 끝까지 도는지 학습 없이 확인하는 연기 시험(smoke test)용이다.
    /// 시드를 받는 System.Random이라 같은 시드면 같은 행동열이 나온다.
    /// </summary>
    public class RandomPolicy : MonoBehaviour, IPolicy
    {
        [SerializeField] private int _seed = 7;

        private System.Random _random;

        private void Awake()
        {
            _random = new System.Random(_seed);
        }

        public void SelectAction(float[] observation, float[] action)
        {
            for (int i = 0; i < action.Length; i++)
            {
                action[i] = (float)(_random.NextDouble() * 2.0 - 1.0);
            }
        }
    }
}
