using System;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// classes.json(학습 스크립트가 내보낸 출력 번호 → 라벨)을 읽는다. 모델 출력 N x 클래스 수의 인덱스가 이 순서이다.
    /// 입력 규격이 Unity 쪽 상수와 다르면 모델과 전처리가 어긋난 것이므로 읽지 못한 것으로 본다.
    /// </summary>
    public sealed class SlotClassMap
    {
        [Serializable]
        private sealed class InputSpec
        {
            public int width;
            public int height;
            public int channels;
        }

        [Serializable]
        private sealed class Document
        {
            public string[] classes;
            public InputSpec input;
        }

        private readonly string[] _labels;

        private SlotClassMap(string[] labels)
        {
            _labels = labels;
        }

        public int Count => _labels.Length;

        public string LabelAt(int index) => _labels[index];

        public static bool TryParse(string json, out SlotClassMap map)
        {
            map = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            Document document;
            try
            {
                document = JsonUtility.FromJson<Document>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (document == null || document.classes == null || document.classes.Length < 2 || document.input == null
                || document.input.width != SlotTensorizer.Width || document.input.height != SlotTensorizer.Height
                || document.input.channels != 3)
            {
                return false;
            }

            map = new SlotClassMap(document.classes);
            return true;
        }
    }
}
