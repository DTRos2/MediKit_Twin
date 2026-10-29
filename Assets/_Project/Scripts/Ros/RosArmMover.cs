using System;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace MediKitTwin.Ros
{
    /// <summary>
    /// 이동 지시를 /arm_a/goal로 보내고 /arm_a/result로 도착·실패를 받는다.
    /// Unity 식별자(선반 칸 ID, 슬롯 번호)를 ROS 자세 이름으로 바꾸는 일은 여기서만 한다.
    /// 자세 이름은 medikit_ws의 arm_a_poses.yaml과 같아야 한다.
    /// </summary>
    public class RosArmMover : MonoBehaviour
    {
        [SerializeField] private string _goalTopic = "/arm_a/goal";
        [SerializeField] private string _resultTopic = "/arm_a/result";

        private ROSConnection _ros;

        /// <summary>이동 결과. 인자는 (자세 이름, 도착 여부). 도착하면 true, timeout이면 false.</summary>
        public event Action<string, bool> MoveFinished;

        private void OnEnable()
        {
            _ros = ROSConnection.GetOrCreateInstance();
            _ros.RegisterPublisher<StringMsg>(_goalTopic);
            _ros.Subscribe<StringMsg>(_resultTopic, OnResult);
        }

        private void OnDisable()
        {
            _ros?.Unsubscribe(_resultTopic);
        }

        /// <summary>선반 칸 ID(A-1)로 이동 지시. 하이픈을 빼고 SHELF_를 붙인다.</summary>
        public void MoveToShelf(string shelfSlotId)
        {
            MoveToPose(ToShelfPose(shelfSlotId));
        }

        /// <summary>박스 슬롯 번호(1부터)로 이동 지시.</summary>
        public void MoveToSlot(int slotNumber)
        {
            MoveToPose(ToSlotPose(slotNumber));
        }

        public void MoveToPose(string poseName)
        {
            var payload = new PoseJson { pose = poseName };
            _ros.Publish(_goalTopic, new StringMsg(JsonUtility.ToJson(payload)));
        }

        public static string ToShelfPose(string shelfSlotId) => "SHELF_" + shelfSlotId.Replace("-", "");

        public static string ToSlotPose(int slotNumber) => "SLOT_" + slotNumber;

        private void OnResult(StringMsg msg)
        {
            var result = JsonUtility.FromJson<ResultJson>(msg.data);
            MoveFinished?.Invoke(result.pose, result.status == "arrived");
        }

        [Serializable]
        private class PoseJson
        {
            public string pose;
        }

        [Serializable]
        private class ResultJson
        {
            public string pose;
            public string status;
        }
    }
}
