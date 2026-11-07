using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로그 한 줄: 시간, 분류 태그, 글. 줄 오브젝트는 편집기에서 미리 놓아 두고 실행 중에는 내용만 바꾼다.
    /// 글이 길면 줄바꿈하고 줄 높이는 레이아웃이 따라 키운다. 경고와 오류 줄은 바탕색을 살짝 달리해서 훑어볼 때 눈에 띄게 한다.
    /// </summary>
    public sealed class LogRowItem : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Text _time;
        [SerializeField] private UiTag _tag;
        [SerializeField] private Text _message;

        public void Show(LogRowEntry entry)
        {
            _background.color = BackgroundOf(entry.Line.Severity, DashboardPalette.Default);
            _time.text = KittingLogPanelTexts.Elapsed(entry.Line.Seconds);
            _tag.Show(entry.Tag.Name, entry.Tag.Tone);
            _message.text = entry.Line.Text;
        }

        private static Color BackgroundOf(LogSeverity severity, DashboardPalette palette)
        {
            switch (severity)
            {
                case LogSeverity.Error: return palette.DangerSoft;
                case LogSeverity.Warning: return palette.WarningSoft;
                default: return palette.Field;
            }
        }
    }
}
