using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>Unity의 로그 종류(LogType)를 로그 줄의 무게(LogSeverity)로 바꾼다. 예외와 단정 실패는 오류로 본다.</summary>
    public static class LogSeverityMap
    {
        public static LogSeverity From(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return LogSeverity.Warning;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return LogSeverity.Error;
                default: return LogSeverity.Info;
            }
        }
    }
}
