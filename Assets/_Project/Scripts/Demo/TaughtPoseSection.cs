using System;
using System.IO;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 패널 안에서 "지금 자세를 선반 칸의 집는 자세로 저장"하는 구간. 사람이 ArmA 자리와 관절을 맞춘 뒤 칸 ID를 적고 저장하면,
    /// 팔 베이스 자리·루트 방향·도구 끝점 자세·관절 각도가 파일에 남고, 자동 키팅이 그 칸을 집을 때 탐색 없이 그 자세를 쓴다.
    /// 저장·삭제·찾기는 IPickPoseLibrary, 값을 읽어 자세로 묶는 일은 TaughtPickPose가 하고 여기서는 화면과 순서만 맡는다.
    /// "불러오기"는 저장된 자세를 찾아 알맞은지 확인한 뒤 onLoad로 넘긴다. 베이스를 놓고 관절을 보내는 일은 로봇을 조작하는 패널이 한다.
    /// ArmA(굴러가는 베이스)가 있는 로봇에서만 만들어진다(서는 자리를 알아야 하기 때문이다).
    /// </summary>
    public sealed class TaughtPoseSection
    {
        private const float SlotLabelWidth = 24f;
        private const float SlotFieldWidth = 60f;
        private const int SlotIdMaxLength = 8;

        private readonly IArmState _state;
        private readonly ArmBaseControlSection _baseSection;
        private readonly IPickPoseLibrary _library;
        private readonly Action<TaughtPickPose> _onLoad;
        private readonly string _label;

        // 관절 수는 팔이 준비된 뒤에야 확정되므로(패널은 Awake에서 만들어진다) 저장할 때 맞춰 만든다.
        private float[] _jointsRad;

        private string _slotId = "A-1";
        private string _message = string.Empty;
        private string _savedText = string.Empty;

        private TaughtPoseSection(
            IArmState state, ArmBaseControlSection baseSection, IPickPoseLibrary library, Action<TaughtPickPose> onLoad, string label)
        {
            _state = state;
            _baseSection = baseSection;
            _library = library;
            _onLoad = onLoad;
            _label = label;
            RefreshSavedText();
        }

        /// <summary>로봇이 IArmState이고 ArmA 구간이 있어야 만든다. 아니면 null이다.</summary>
        /// <param name="onLoad">불러오기를 눌렀을 때 저장된 자세를 로봇에 적용하는 쪽. 관절 수가 이 로봇과 같은 자세만 넘어온다.</param>
        public static TaughtPoseSection TryCreate(
            MonoBehaviour robot, ArmBaseControlSection baseSection, IPickPoseLibrary library, Action<TaughtPickPose> onLoad)
        {
            if (!(robot is IArmState state) || baseSection == null || library == null || onLoad == null || baseSection.ArmBasePosition == null)
            {
                return null;
            }

            return new TaughtPoseSection(state, baseSection, library, onLoad, robot.name);
        }

        public void Draw()
        {
            GUILayout.Space(6f);
            GUILayout.Label("지금 자세를 선반 칸의 집는 자세로 저장 (ArmA 자리·방향 + 도구 + 관절)");

            GUILayout.BeginHorizontal();
            GUILayout.Label("칸", GUILayout.Width(SlotLabelWidth));
            _slotId = GUILayout.TextField(_slotId, SlotIdMaxLength, GUILayout.Width(SlotFieldWidth));

            if (GUILayout.Button("지금 자세 저장"))
            {
                Save();
            }

            if (GUILayout.Button("불러오기"))
            {
                Load();
            }

            if (GUILayout.Button("삭제"))
            {
                Remove();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(_savedText);
            if (_message.Length > 0)
            {
                GUILayout.Label(_message);
            }
        }

        private void Save()
        {
            string slot = PickSlotKey.Normalize(_slotId);
            if (slot.Length == 0)
            {
                _message = "칸 ID를 적는다 (예: A-1).";
                return;
            }

            Vector3? armBase = _baseSection.ArmBasePosition;
            if (!_state.IsReady || armBase == null)
            {
                _message = "로봇이 준비되지 않아 저장하지 못했다.";
                return;
            }

            EnsureJointBuffer();
            _state.GetJointPositions(_jointsRad);
            Vector3 root = _baseSection.RootPosition;
            TaughtPickPose pose = TaughtPickPose.Capture(
                slot, root, _baseSection.RootYawDeg, armBase.Value, _state.ToolPose, _jointsRad);

            if (TryFileAction(() => _library.Save(pose)))
            {
                _message = $"{slot} 저장: ArmA x {root.x:0.00} z {root.z:0.00}, 방향 {_baseSection.RootYawDeg:0.0}° (팔 베이스 x {armBase.Value.x:0.00} z {armBase.Value.z:0.00})";
                Debug.Log($"{_label}: 칸 {slot}의 집는 자세를 저장했다 — {_message}");
            }
        }

        private void EnsureJointBuffer()
        {
            int count = _state.JointNames.Count;
            if (_jointsRad == null || _jointsRad.Length != count)
            {
                _jointsRad = new float[count];
            }
        }

        // 저장된 자세를 찾아 로봇에 적용하도록 넘긴다. 로봇이 준비되지 않았거나 관절 수가 다르면 아무것도 움직이지 않는다.
        private void Load()
        {
            string slot = PickSlotKey.Normalize(_slotId);
            if (slot.Length == 0)
            {
                _message = "칸 ID를 적는다 (예: A-1).";
                return;
            }

            if (!_state.IsReady)
            {
                _message = "로봇이 준비되지 않아 불러오지 못했다.";
                return;
            }

            TaughtPickPose pose = null;
            bool isFound = false;
            if (!TryFileAction(() => isFound = _library.TryGet(slot, out pose)))
            {
                return;
            }

            if (!isFound)
            {
                _message = $"{slot}에 저장된 자세가 없다.";
                return;
            }

            int jointCount = pose.JointsRad().Length;
            if (jointCount != _state.JointNames.Count)
            {
                _message = $"{slot}에 저장된 관절 수({jointCount})가 이 로봇({_state.JointNames.Count})과 달라 불러오지 못했다.";
                return;
            }

            _onLoad(pose);
            _message = $"{slot} 불러옴: ArmA x {pose.RootPosition.x:0.00} z {pose.RootPosition.z:0.00}, 방향 {pose.RootYawDeg:0.0}°";
        }

        private void Remove()
        {
            string slot = PickSlotKey.Normalize(_slotId);
            bool isRemoved = false;
            if (TryFileAction(() => isRemoved = _library.Remove(slot)))
            {
                _message = isRemoved ? $"{slot} 삭제" : $"{slot}에 저장된 자세가 없다.";
            }
        }

        // 파일을 읽고 쓰다 실패하면(형식이 깨짐, 쓰기 권한 없음) 패널에 이유를 보여 준다. 성공하면 저장된 칸 목록도 새로 만든다.
        private bool TryFileAction(Action action)
        {
            try
            {
                action();
                RefreshSavedText();
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                _message = $"저장 파일을 다루지 못했다: {exception.Message}";
                Debug.LogError($"{_label}: {_message}");
                return false;
            }
        }

        private void RefreshSavedText()
        {
            try
            {
                var ids = _library.SlotIds();
                _savedText = ids.Count == 0 ? "저장된 칸: 없음" : $"저장된 칸({ids.Count}): {string.Join(", ", ids)}";
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException)
            {
                _savedText = $"저장 파일을 읽지 못했다: {exception.Message}";
            }
        }
    }
}
