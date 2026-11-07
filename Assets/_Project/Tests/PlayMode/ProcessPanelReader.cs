#if UNITY_EDITOR
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 열린 씬의 공정 현황 패널에서 화면에 적힌 글을 읽는다. 패널이 실제로 무엇을 보여 주는지(값을 센 객체가 아니라 화면)를
    /// 여러 시험이 같은 방법으로 보게 한 곳에 둔다.
    /// </summary>
    internal static class ProcessPanelReader
    {
        /// <summary>이름이 tileName인 숫자 칸의 값 글.</summary>
        public static string TileValue(ProcessStatusPanelPresenter presenter, string tileName)
        {
            foreach (Transform child in presenter.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == tileName)
                {
                    return child.Find("Value").GetComponent<Text>().text;
                }
            }

            Assert.Fail($"숫자 칸 '{tileName}'이 패널에 없다");
            return null;
        }

        /// <summary>이름이 objectName인 글자 오브젝트의 글.</summary>
        public static string TextOf(ProcessStatusPanelPresenter presenter, string objectName)
        {
            foreach (Text text in presenter.GetComponentsInChildren<Text>(true))
            {
                if (text.transform.name == objectName)
                {
                    return text.text;
                }
            }

            Assert.Fail($"패널에 '{objectName}' 글자가 없다");
            return null;
        }
    }
}
#endif
