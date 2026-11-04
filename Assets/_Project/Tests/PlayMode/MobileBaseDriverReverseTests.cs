#if UNITY_EDITOR
using System.Collections;
using MediKitTwin.Mobile;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// MobileBaseDriver.ReverseTo가 방향을 바꾸지 않고 뒤로 물러나는지 본다. 하역 로봇이 받침대 앞처럼 제자리에서 돌 수 없는 자리에서 먼저 물러난 뒤 돌기 위한 이동이다.
    /// 씬 없이 빈 오브젝트에 드라이버만 붙여 본다.
    /// </summary>
    public sealed class MobileBaseDriverReverseTests
    {
        private const float TimeoutSeconds = 10f;
        private const float PositionToleranceM = 0.03f;
        private const float YawToleranceDeg = 0.6f;

        private GameObject _robot;
        private MobileBaseDriver _driver;
        private bool _arrived;

        [SetUp]
        public void SetUp()
        {
            _robot = new GameObject("TestMobileBase");
            _robot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f)); // +x를 본다
            _driver = _robot.AddComponent<MobileBaseDriver>();
            _arrived = false;
            _driver.Arrived += () => _arrived = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_robot);
        }

        [UnityTest]
        public IEnumerator ReverseTo_BacksUpToThePointBehind_WithoutTurning()
        {
            _driver.ReverseTo(new Vector3(-0.5f, 0f, 0f));

            yield return WaitForArrival();

            Assert.That(_arrived, Is.True, "시간 안에 도착하지 못했다");
            Assert.That(_robot.transform.position.x, Is.EqualTo(-0.5f).Within(PositionToleranceM));
            Assert.That(_robot.transform.position.z, Is.EqualTo(0f).Within(PositionToleranceM));
            Assert.That(Mathf.DeltaAngle(_robot.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(YawToleranceDeg), "뒤로 물러나는 동안 방향이 바뀌었다");
        }

        [UnityTest]
        public IEnumerator ReverseTo_NeverMovesForward_OnTheWayToThePointBehind()
        {
            _driver.ReverseTo(new Vector3(-0.5f, 0f, 0f));

            float furthestForward = 0f;
            float deadline = Time.time + TimeoutSeconds;
            while (!_arrived && Time.time < deadline)
            {
                furthestForward = Mathf.Max(furthestForward, _robot.transform.position.x);
                yield return null;
            }

            Assert.That(furthestForward, Is.LessThanOrEqualTo(PositionToleranceM), "뒤로 물러나기 전에 앞으로 나아갔다");
        }

        [UnityTest]
        public IEnumerator DriveTo_StillTurnsFirstAndMovesForward()
        {
            // 후진 이동을 더한 뒤에도 기존 이동(DriveTo)은 그대로 목표 쪽으로 돌고 전진해야 한다.
            _driver.DriveTo(new Vector3(0f, 0f, 1f));

            yield return WaitForArrival();

            Assert.That(_arrived, Is.True);
            Assert.That(_robot.transform.position.z, Is.EqualTo(1f).Within(PositionToleranceM));
            Assert.That(Mathf.DeltaAngle(_robot.transform.eulerAngles.y, 0f), Is.EqualTo(0f).Within(5f), "목표를 향해 돌아서지 않았다");
        }

        private IEnumerator WaitForArrival()
        {
            float deadline = Time.time + TimeoutSeconds;
            while (!_arrived && Time.time < deadline)
            {
                yield return null;
            }
        }
    }
}
#endif
