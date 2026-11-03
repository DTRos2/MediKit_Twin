using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

namespace MediKitTwin.Inspection.Cnn
{
    /// <summary>
    /// ONNX 모델(slot_cnn.onnx)로 슬롯 크롭을 분류한다. 입력 input(N x 3 x 72 x 128), 출력 logits(N x 클래스 수).
    /// 모델은 작아서 CPU 백엔드를 쓴다. Dispose로 워커를 해제한다.
    /// </summary>
    public sealed class SentisSlotClassifier : ISlotClassifier, IDisposable
    {
        private readonly Worker _worker;
        private readonly SlotClassMap _classes;

        public SentisSlotClassifier(ModelAsset model, SlotClassMap classes)
        {
            _classes = classes ?? throw new ArgumentNullException(nameof(classes));
            _worker = new Worker(ModelLoader.Load(model), BackendType.CPU);
        }

        public bool TryClassify(IReadOnlyList<Texture2D> crops, out SlotPrediction[] predictions)
        {
            predictions = null;
            if (crops == null || crops.Count == 0)
            {
                return false;
            }

            try
            {
                var data = new float[crops.Count * SlotTensorizer.FloatsPerImage];
                for (int i = 0; i < crops.Count; i++)
                {
                    SlotTensorizer.Write(crops[i], data, i * SlotTensorizer.FloatsPerImage);
                }

                using var input = new Tensor<float>(new TensorShape(crops.Count, SlotTensorizer.Channels, SlotTensorizer.Height, SlotTensorizer.Width), data);
                _worker.Schedule(input);

                var output = _worker.PeekOutput("logits") as Tensor<float>;
                if (output == null)
                {
                    return false;
                }

                using var readable = output.ReadbackAndClone() as Tensor<float>;
                float[] logits = readable.DownloadToArray();
                int classCount = _classes.Count;
                if (logits.Length != crops.Count * classCount)
                {
                    Debug.LogError($"모델 출력 크기({logits.Length})가 클래스 수 {classCount} x 크롭 {crops.Count}와 다르다. classes.json과 모델이 맞는지 확인한다.");
                    return false;
                }

                var result = new SlotPrediction[crops.Count];
                for (int i = 0; i < crops.Count; i++)
                {
                    result[i] = Argmax(logits, i * classCount, classCount);
                }

                predictions = result;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        // softmax 확률이 가장 큰 클래스와 그 확률.
        private SlotPrediction Argmax(float[] logits, int offset, int count)
        {
            int best = 0;
            for (int k = 1; k < count; k++)
            {
                if (logits[offset + k] > logits[offset + best])
                {
                    best = k;
                }
            }

            float sum = 0f;
            for (int k = 0; k < count; k++)
            {
                sum += Mathf.Exp(logits[offset + k] - logits[offset + best]);
            }

            return new SlotPrediction(_classes.LabelAt(best), 1f / sum);
        }

        public void Dispose()
        {
            _worker.Dispose();
        }
    }
}
