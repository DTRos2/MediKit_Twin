#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 물건을 쥔 순간의 그리퍼를 도구 축에 맞춰 세 방향에서 찍는다. 수치(GraspHoldRecorder)만으로는 패드가 물건을 감쌌는지, 물건이 패드 위나 앞으로 치우쳤는지
    /// 헷갈리므로 눈으로 가려 보려는 것이다. 도구 축은 x가 월드 아래쪽, y가 손가락이 닫히는 방향, z가 물건 쪽으로 들어가는 방향이라
    ///  - side: 손가락 틈을 옆에서 본다(위아래·깊이 어긋남이 보인다).
    ///  - otherSide: 반대쪽 옆에서 본다.
    ///  - top: 위에서 내려다본다(좌우·깊이 어긋남이 보인다).
    /// 환경 변수 MEDIKIT_E2E_PHOTO_DIR에 폴더를 주면 그 폴더에 저장한다. 정하지 않으면 켜지지 않는다.
    /// </summary>
    public sealed class GraspPhotographer
    {
        public const string DirectoryVariable = "MEDIKIT_E2E_PHOTO_DIR";
        public const float DefaultViewDistanceM = 0.25f;

        private readonly string _directory;
        private readonly OffscreenCapture _capture;
        private readonly float _viewDistanceM;
        private int _shotCount;

        public GraspPhotographer(string directory, OffscreenCapture capture, float viewDistanceM = DefaultViewDistanceM)
        {
            _directory = directory;
            _capture = capture;
            _viewDistanceM = viewDistanceM;
        }

        /// <summary>환경 변수가 정해져 있으면 그 폴더에 저장하는 촬영기를, 아니면 null을 돌려준다.</summary>
        public static GraspPhotographer FromEnvironment()
        {
            string directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            return string.IsNullOrEmpty(directory) ? null : new GraspPhotographer(directory, new OffscreenCapture());
        }

        /// <param name="label">파일 이름에 붙일 때(예: grasp, later).</param>
        /// <param name="focus">화면 한가운데에 둘 월드 점. 보통 도구 끝점과 물건 중심의 가운데다.</param>
        public void Shoot(string label, Pose tool, Vector3 focus)
        {
            Vector3 toolX = tool.rotation * Vector3.right;
            Vector3 toolY = tool.rotation * Vector3.up;
            Vector3 toolZ = tool.rotation * Vector3.forward;
            Vector3 worldUpAlongTool = -toolX;
            int index = _shotCount++;

            Save(index, label, "side", focus + toolY * _viewDistanceM, focus, worldUpAlongTool);
            Save(index, label, "otherSide", focus - toolY * _viewDistanceM, focus, worldUpAlongTool);
            Save(index, label, "top", focus - toolX * _viewDistanceM, focus, toolZ);
        }

        private void Save(int index, string label, string view, Vector3 eye, Vector3 focus, Vector3 up)
        {
            string path = Path.Combine(_directory, $"{index:00}_{label}_{view}.png");
            _capture.Save(path, eye, focus, up);
        }
    }
}
#endif
