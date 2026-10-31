using System;
using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암 베이스를 월드의 한 지점(바닥 평면)으로 옮기는 동작. 이송 순서(ArmItemTransfer)는 베이스가 어떻게 굴러가는지 모르고
    /// 이 계약만 부른다. done에는 도착했으면 true, 시간 안에 닿지 못했으면 false를 넘긴다.
    /// </summary>
    public interface IArmBasePositioner
    {
        IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done);

        /// <summary>도착한 뒤 베이스가 바라볼 방향(월드 Y축 기준 도)까지 맞춘다. 작업 구역을 정면으로 보고 서서 첫 관절이 한쪽으로 치우치지 않게 할 때 쓴다.</summary>
        IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done);

        /// <summary>마지막 이동이 실패한 이유를 사람이 읽는 문장으로 돌려준다. 성공했거나 아직 움직이지 않았으면 빈 문자열이다.</summary>
        string LastFailureReason { get; }
    }
}
