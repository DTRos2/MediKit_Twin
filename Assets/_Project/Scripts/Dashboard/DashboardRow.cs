using System;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 위젯을 가로로 늘어놓는 줄(using으로 쓴다). GUILayout.BeginHorizontal과 EndHorizontal을 짝지어 주므로
    /// 중간에 return하거나 예외가 나도 그룹이 열린 채 남지 않는다. OnGUI 안에서만 부른다.
    /// </summary>
    public readonly struct DashboardRow : IDisposable
    {
        public static DashboardRow Begin()
        {
            GUILayout.BeginHorizontal();
            return new DashboardRow();
        }

        public void Dispose()
        {
            GUILayout.EndHorizontal();
        }
    }
}
