#if UNITY_EDITOR
using System;
using System.Collections;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 셀 씬(Scene_Cell)을 열고 기다리는 시험 공통 동작. 컨베이어 시험과 전체 공정 시험이 같은 순서를 쓰도록 한 곳에 둔다.
    /// </summary>
    internal static class CellTestScene
    {
        /// <summary>씬을 열고 첫 프레임들이 돌 때까지 기다린다.</summary>
        public static IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        /// <summary>씬에서 인터페이스 T를 구현한 첫 컴포넌트. 없으면 null이다.</summary>
        public static T FindOne<T>() where T : class
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour is T found)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>조건이 참이 되거나 시뮬레이션 시간으로 timeoutSeconds가 지날 때까지 기다린다.</summary>
        public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds)
        {
            float deadline = Time.time + timeoutSeconds;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }
        }

        /// <summary>씬에서 T 컴포넌트를 모두 끈다. 시험이 보려는 일과 상관없는 설비를 빼는 데 쓴다.</summary>
        public static void DisableAll<T>() where T : Behaviour
        {
            foreach (T behaviour in Object.FindObjectsByType<T>())
            {
                behaviour.enabled = false;
            }
        }

        /// <summary>
        /// 그래픽이 없는 배치 모드에서는 도구 카메라가 화면을 그릴 때마다 RenderTexture 오류를 남긴다.
        /// 시험이 끝난 뒤에도 씬이 남아 있어서, 다음 시험이 시작하기 전 틈에 나온 오류가 그 시험의 실패로 잡힌다. 끝날 때 그리기를 멈춘다.
        /// </summary>
        public static IEnumerator StopToolCameraFeeds()
        {
            foreach (ToolCameraFeed feed in Object.FindObjectsByType<ToolCameraFeed>())
            {
                feed.enabled = false;
            }

            yield return null;
        }
    }
}
#endif
