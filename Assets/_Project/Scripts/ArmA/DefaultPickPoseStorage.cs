using System.IO;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세 파일의 기본 위치를 정해 저장소를 만든다. Unity 메인 스레드에서만 부른다.
    /// 에디터는 프로젝트 안 파일(저장소에 함께 올리는 원본)을 읽고 쓴다.
    /// 빌드는 사용자 데이터 폴더의 파일을 먼저 읽고 거기에 쓴다. 그 파일이 아직 없으면 빌드에 실린 기본 파일(StreamingAssets)을 읽는다.
    /// 프로젝트 안 파일을 빌드에 싣는 일은 에디터의 PickPoseBuildPostprocessor가 맡는다.
    /// </summary>
    public static class DefaultPickPoseStorage
    {
        /// <summary>프로젝트 안 자세 파일. 에디터가 읽고 쓰며, 빌드할 때 StreamingAssets로 복사해 싣는다.</summary>
        public static string ProjectFilePath => Path.Combine(Application.dataPath, "_Project", "Config", FilePickPoseStorage.FileName);

        public static IPickPoseStorage Create()
        {
#if UNITY_EDITOR
            return new FilePickPoseStorage(ProjectFilePath);
#else
            return new FallbackPickPoseStorage(
                new FilePickPoseStorage(Path.Combine(Application.persistentDataPath, FilePickPoseStorage.FileName)),
                new FilePickPoseStorage(Path.Combine(Application.streamingAssetsPath, FilePickPoseStorage.FileName)));
#endif
        }
    }
}
