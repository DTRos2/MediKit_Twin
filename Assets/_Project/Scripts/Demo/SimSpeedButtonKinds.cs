using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>배속 버튼이 어떤 종류로 보일지 정한다: 지금 배속과 같은 버튼만 켜진 것(Selected)으로 보인다. Unity 없이 시험할 수 있다.</summary>
    public static class SimSpeedButtonKinds
    {
        public static ButtonKind For(float buttonSpeed, float currentSpeed)
        {
            return Mathf.Approximately(buttonSpeed, currentSpeed) ? ButtonKind.Selected : ButtonKind.Secondary;
        }
    }
}
