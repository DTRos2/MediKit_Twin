using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MediKitTwin.Common;
using UnityEngine;
using Random = System.Random;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 검사 성능 측정(NFR-04, 5단계). 플레이 모드에서 InspectionSampleSpawner로 정상, 누락, 오구성 박스를 정해진 비율로 많이 만들어
    /// 검사 구역에 넣고, 판정 결과를 정답과 비교해 박스 정확도와 불량 재현율을 낸다. 학습에 쓰지 않은 실제 씬 이미지로 재는 것이 목적이다.
    /// 플레이 모드에서 컴포넌트를 우클릭해 'Run Benchmark'를 누른다. 결과는 콘솔과 프로젝트 폴더의 InspectionBenchmark/<시각>.csv에 남는다.
    /// 센서 무응답 박스는 CNN 경로와 무관하므로 포함하지 않는다. 키팅과 컨베이어를 구현하면 이 컴포넌트는 지운다.
    /// </summary>
    public sealed class InspectionBenchmark : MonoBehaviour
    {
        [Tooltip("샘플 박스를 만드는 스포너. 비워 두면 씬에서 찾는다.")]
        [SerializeField] private InspectionSampleSpawner _spawner;

        [Tooltip("만들 박스 수.")]
        [SerializeField, Min(1)] private int _boxCount = 200;

        [Tooltip("불량 박스 비율. 불량은 누락과 오구성이 절반씩이다.")]
        [SerializeField, Range(0f, 1f)] private float _defectRatio = 0.5f;

        [Tooltip("순서대로 돌려 가며 쓰는 키트 종류.")]
        [SerializeField] private List<string> _kitCodes = new List<string> { "EM", "BL", "DX" };

        [Tooltip("학습 데이터의 시드(20261102)와 다른 값을 쓴다. 같은 값이면 같은 박스 순서가 나온다. (NFR-06)")]
        [SerializeField] private int _seed = 20261103;

        [Tooltip("박스 하나의 결과를 기다리는 최대 시간(초, 실제 시간). 넘으면 측정을 멈춘다.")]
        [SerializeField, Min(1f)] private float _timeoutSeconds = 10f;

        private KitData _waitingKit;
        private KitData _finishedKit;
        private bool _running;

        [ContextMenu("Run Benchmark")]
        private void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 모드에서만 측정할 수 있다.", this);
                return;
            }

            if (_running)
            {
                Debug.LogWarning($"{name}: 이미 측정 중이다.", this);
                return;
            }

            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<InspectionSampleSpawner>();
            }

            if (_spawner == null || _kitCodes.Count == 0)
            {
                Debug.LogError($"{name}: InspectionSampleSpawner와 키트 종류가 있어야 한다.", this);
                return;
            }

            StartCoroutine(Measure());
        }

        private void OnEnable()
        {
            CellEvents.InspectionResult += OnInspectionResult;
        }

        private void OnDisable()
        {
            CellEvents.InspectionResult -= OnInspectionResult;
        }

        private void OnInspectionResult(KitData kit)
        {
            if (kit == _waitingKit)
            {
                _finishedKit = kit;
            }
        }

        private IEnumerator Measure()
        {
            _running = true;
            _spawner.LogResults = false;

            var overall = new BenchmarkTally();
            var byVariant = new Dictionary<SampleVariant, BenchmarkTally>
            {
                { SampleVariant.Normal, new BenchmarkTally() },
                { SampleVariant.Missing, new BenchmarkTally() },
                { SampleVariant.WrongItem, new BenchmarkTally() },
            };
            var csv = new StringBuilder("index,kit,variant,target_slot,true_defect,pred_defect,pred_slots,confidence,method,box_correct\n");
            var random = new Random(_seed);
            int ruleFallbacks = 0;
            int done = 0;

            Debug.Log($"[Benchmark] 시작: 박스 {_boxCount}개, 불량 비율 {_defectRatio:F2}, 시드 {_seed}", this);

            for (int i = 0; i < _boxCount; i++)
            {
                SampleVariant variant = PickVariant(random);
                string kitCode = _kitCodes[i % _kitCodes.Count];

                _finishedKit = null;
                if (!_spawner.TrySpawn(variant, kitCode, 0, out KitData kit, out int target))
                {
                    break;
                }

                _waitingKit = kit;
                float deadline = Time.unscaledTime + _timeoutSeconds;
                while (_finishedKit == null && Time.unscaledTime < deadline)
                {
                    yield return null;
                }

                if (_finishedKit == null)
                {
                    Debug.LogError($"[Benchmark] 박스 {i + 1}({kitCode}, {variant})의 결과가 {_timeoutSeconds:F0}초 안에 오지 않아 측정을 멈춘다.", this);
                    break;
                }

                InspectionVerdict verdict = kit.Verdict.Value;
                bool trueDefect = variant != SampleVariant.Normal;
                var trueSlots = trueDefect ? new List<int> { target } : new List<int>();

                bool correct = overall.Add(trueDefect, trueSlots, verdict.IsPass, verdict.MissingSlots);
                byVariant[variant].Add(trueDefect, trueSlots, verdict.IsPass, verdict.MissingSlots);
                if (verdict.Method != JudgeMethod.Cnn)
                {
                    ruleFallbacks++;
                }

                string predictedSlots = verdict.MissingSlots == null ? "unknown" : string.Join("|", verdict.MissingSlots);
                csv.AppendLine(string.Join(",",
                    i + 1, kitCode, variant, trueDefect ? target.ToString() : "-", trueDefect, !verdict.IsPass,
                    predictedSlots, verdict.Confidence.ToString("F3", CultureInfo.InvariantCulture), verdict.Method, correct));
                done++;

                // 판정이 틀린 박스는 바로 알려서, 끝나기를 기다리지 않고 원인을 볼 수 있게 한다.
                if (!correct)
                {
                    Debug.LogWarning($"[Benchmark] 틀린 박스 {i + 1}: {kitCode} {variant} 슬롯 {target} → {(verdict.IsPass ? "정상" : "불량")} (불일치 {predictedSlots}, 신뢰도 {verdict.Confidence:F2})", this);
                }

                yield return new WaitForSeconds(0.05f);
            }

            _waitingKit = null;
            _spawner.LogResults = true;
            _running = false;

            if (done == 0)
            {
                yield break;
            }

            string path = WriteCsv(csv.ToString());
            Debug.Log($"[Benchmark] 결과 ({done}/{_boxCount}) — {overall.Summary()}", this);
            Debug.Log($"[Benchmark] 정상 박스: 오경보율 {byVariant[SampleVariant.Normal].FalseAlarmRate:F3} / " +
                      $"누락: 재현율 {byVariant[SampleVariant.Missing].DefectRecall:F3}, 위치 일치 {byVariant[SampleVariant.Missing].SlotLocalization:F3} / " +
                      $"오구성: 재현율 {byVariant[SampleVariant.WrongItem].DefectRecall:F3}, 위치 일치 {byVariant[SampleVariant.WrongItem].SlotLocalization:F3} / " +
                      $"규칙 판정으로 대체된 박스 {ruleFallbacks}", this);
            Debug.Log($"[Benchmark] 박스별 결과를 저장했다: {path}", this);
        }

        // 불량 비율만큼 불량, 불량은 누락과 오구성을 절반씩.
        private SampleVariant PickVariant(Random random)
        {
            if (random.NextDouble() >= _defectRatio)
            {
                return SampleVariant.Normal;
            }

            return random.Next(2) == 0 ? SampleVariant.Missing : SampleVariant.WrongItem;
        }

        private static string WriteCsv(string content)
        {
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "InspectionBenchmark");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }
    }
}
