using System;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using Unity.InferenceEngine;
using UnityEngine;

namespace MediKitTwin.Inspection.Cnn
{
    /// <summary>
    /// CNN 판정기(FR-07). InspectionStation의 Primary Inspector에 연결한다.
    /// 모델이나 classes.json을 읽지 못하면 TryJudge가 false이고, 검사 구역이 규칙 판정으로 대체한다.
    /// </summary>
    public sealed class CnnInspector : MonoBehaviour, IInspector, IStationBoundInspector
    {
        [Tooltip("slot_cnn.onnx (Assets/_Project/Models).")]
        [SerializeField] private ModelAsset _model;

        [Tooltip("classes.json (같은 학습 결과 폴더의 것). 모델 출력 번호와 라벨의 대응이다.")]
        [SerializeField] private TextAsset _classes;

        private CnnSlotJudge _judge;
        private SentisSlotClassifier _classifier;

        public JudgeMethod Method => JudgeMethod.Cnn;

        public void Bind(Func<string, RecipeSO> findRecipe, ISlotCropSource cropSource)
        {
            _classifier?.Dispose();
            _classifier = null;
            _judge = null;

            if (_model == null || _classes == null)
            {
                Debug.LogError($"{name}: Model(slot_cnn.onnx)과 Classes(classes.json)를 지정해야 한다. 규칙 판정으로 대체한다.", this);
                return;
            }

            if (!SlotClassMap.TryParse(_classes.text, out SlotClassMap map))
            {
                Debug.LogError($"{name}: classes.json을 읽지 못했다(형식이나 입력 크기 128x72 불일치). 규칙 판정으로 대체한다.", this);
                return;
            }

            try
            {
                _classifier = new SentisSlotClassifier(_model, map);
                _judge = new CnnSlotJudge(findRecipe, cropSource, _classifier);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                Debug.LogError($"{name}: 모델을 불러오지 못했다. 규칙 판정으로 대체한다.", this);
            }
        }

        public bool TryJudge(KitData kit, out InspectionVerdict verdict)
        {
            verdict = default;
            return _judge != null && _judge.TryJudge(kit, out verdict);
        }

        private void OnDestroy()
        {
            _classifier?.Dispose();
        }
    }
}
