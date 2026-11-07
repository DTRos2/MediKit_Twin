using System.Collections.Generic;
using System.Linq;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 검사 성능(NFR-04) 집계. 박스마다 정답(불량 여부와 틀린 슬롯)과 판정을 넣으면 박스 정확도, 불량 재현율,
    /// 정상 박스 오경보율, 불량 슬롯 위치까지 맞춘 비율을 계산한다. 장면과 무관한 계산이라 따로 두어 테스트한다.
    /// </summary>
    public sealed class BenchmarkTally
    {
        public const float TargetRate = 0.90f;

        private int _truePositive;
        private int _falseNegative;
        private int _trueNegative;
        private int _falsePositive;
        private int _slotMatched;

        public int Boxes => _truePositive + _falseNegative + _trueNegative + _falsePositive;

        public int TrueDefects => _truePositive + _falseNegative;

        public int FalseAlarms => _falsePositive;

        public int MissedDefects => _falseNegative;

        public float BoxAccuracy => Boxes == 0 ? 0f : (float)(_truePositive + _trueNegative) / Boxes;

        public float DefectRecall => TrueDefects == 0 ? 0f : (float)_truePositive / TrueDefects;

        public float FalseAlarmRate => _trueNegative + _falsePositive == 0 ? 0f : (float)_falsePositive / (_trueNegative + _falsePositive);

        /// <summary>불량을 불량으로 잡은 박스 중 틀린 슬롯 번호까지 정확히 맞춘 비율.</summary>
        public float SlotLocalization => _truePositive == 0 ? 0f : (float)_slotMatched / _truePositive;

        public bool Passed => Boxes > 0 && BoxAccuracy >= TargetRate && DefectRecall >= TargetRate;

        /// <summary>박스 하나를 넣고 박스 단위 판정(정상/불량)이 맞았는지 돌려준다. predictedSlots가 null이면 위치를 알 수 없다는 뜻이다.</summary>
        public bool Add(bool trueDefect, IReadOnlyList<int> trueSlots, bool predictedPass, IReadOnlyList<int> predictedSlots)
        {
            bool predictedDefect = !predictedPass;
            if (trueDefect && predictedDefect)
            {
                _truePositive++;
                if (predictedSlots != null && trueSlots != null && predictedSlots.OrderBy(x => x).SequenceEqual(trueSlots.OrderBy(x => x)))
                {
                    _slotMatched++;
                }
            }
            else if (trueDefect)
            {
                _falseNegative++;
            }
            else if (predictedDefect)
            {
                _falsePositive++;
            }
            else
            {
                _trueNegative++;
            }

            return trueDefect == predictedDefect;
        }

        public string Summary()
        {
            return $"박스 {Boxes}개 (불량 {TrueDefects}) / 박스 정확도 {BoxAccuracy:F3} / 불량 재현율 {DefectRecall:F3} (놓친 불량 {MissedDefects}) / " +
                   $"정상 오경보율 {FalseAlarmRate:F3} (오경보 {FalseAlarms}) / 불량 슬롯 위치 일치 {SlotLocalization:F3} / " +
                   $"NFR-04(각 {TargetRate:F2} 이상) {(Passed ? "통과" : "미달")}";
        }
    }
}
