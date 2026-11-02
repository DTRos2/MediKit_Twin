using System;
using System.IO;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 칸에 사람이 알려 준 자세가 있는지 찾고, 없을 때 그 이유를 설명한다. 저장소를 못 읽으면(형식이 틀리거나 읽기 오류) 알리고 없는 것으로 본다.
    /// 알려 준 자세를 쓰지 않도록 꺼 두었으면 source가 null이다. 씬 없이 시험한다.
    /// </summary>
    public sealed class TaughtPoseFinder
    {
        private readonly IPickPoseSource _source;
        private readonly Action<string> _logError;

        /// <param name="source">알려 준 자세를 읽는 쪽. 쓰지 않으면 null.</param>
        /// <param name="logError">읽기 오류를 알리는 쪽. 필요 없으면 null.</param>
        public TaughtPoseFinder(IPickPoseSource source, Action<string> logError)
        {
            _source = source;
            _logError = logError;
        }

        public bool TryFind(string slotId, out TaughtPickPose pose)
        {
            pose = null;
            if (_source == null || string.IsNullOrEmpty(slotId))
            {
                return false;
            }

            try
            {
                return _source.TryGet(slotId, out pose);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException)
            {
                _logError?.Invoke($"알려 준 자세 파일을 읽지 못해 이번에는 스스로 찾아 집는다 — {exception.Message}");
                pose = null;
                return false;
            }
        }

        /// <summary>알려 준 자세를 쓰지 않는 이유. 파일을 못 읽은 경우는 TryFind가 따로 알렸다.</summary>
        public string WhyNot(string slotId)
        {
            if (_source == null)
            {
                return "알려 준 자세 쓰기(Use Taught Poses)가 꺼져 있어";
            }

            return string.IsNullOrEmpty(slotId)
                ? "집을 물건의 칸 ID를 알 수 없어"
                : $"칸 {slotId}에 알려 준 자세가 없어";
        }
    }
}
