using System.IO;
using MediKitTwin.ArmA;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 빌드가 끝나면 프로젝트 안 알려 준 자세 파일(Assets/_Project/Config/TaughtPickPoses.json)을 빌드의 StreamingAssets에 복사해 싣는다.
    /// 빌드에는 에디터처럼 프로젝트 안 파일이 없어서, 이 파일을 싣지 않으면 로봇이 알려 준 자세 없이 스스로 찾다가 집지 못한다.
    /// 원본은 프로젝트 안 파일 하나뿐이다(StreamingAssets 아래에 복사본을 따로 두지 않아 어긋나지 않는다).
    /// 사용자 데이터 폴더에 자세 파일이 생기면 빌드는 그쪽을 먼저 읽는다(DefaultPickPoseStorage). 윈도 독립 실행 빌드만 다룬다.
    /// </summary>
    public sealed class PickPoseBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64)
            {
                return;
            }

            string source = DefaultPickPoseStorage.ProjectFilePath;
            if (!File.Exists(source))
            {
                Debug.LogWarning($"알려 준 자세 파일이 없어 빌드에 싣지 못했다 — {source}");
                return;
            }

            string target = StreamingAssetsTarget(report.summary.outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(source, target, true);
            Debug.Log($"알려 준 자세 파일을 빌드에 실었다 — {target}");
        }

        /// <summary>빌드 실행 파일 경로(…/이름.exe)로 StreamingAssets 아래 자세 파일의 자리를 구한다. 윈도 빌드는 실행 파일 옆의 "이름_Data" 폴더에 데이터를 둔다.</summary>
        internal static string StreamingAssetsTarget(string executablePath)
        {
            string directory = Path.GetDirectoryName(executablePath);
            string dataFolder = Path.GetFileNameWithoutExtension(executablePath) + "_Data";
            return Path.Combine(directory, dataFolder, "StreamingAssets", FilePickPoseStorage.FileName);
        }
    }
}
