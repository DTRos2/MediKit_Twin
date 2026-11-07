using MediKitTwin.Dashboard;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 한 행: 왼쪽에 행 글자와 키트 종류, 오른쪽에 칸마다 재고 타일. 행 색은 키트 종류 색(KitBadgePalette)을 쓴다.
    /// 어느 행인지(_letter)만 알고, 키트 종류는 밖에서 받는다. 칸 타일은 이 오브젝트 아래에서 찾으므로 계층에서 타일을 더하거나 빼면 따라온다.
    /// </summary>
    public sealed class ShelfRowItem : MonoBehaviour
    {
        [Tooltip("이 행의 글자(A, B, C). 칸 ID의 첫 글자와 같다.")]
        [SerializeField] private string _letter = "A";
        [SerializeField] private Text _letterText;
        [SerializeField] private Text _kitCodeText;

        private ShelfSlotTile[] _tiles;

        public char Letter => string.IsNullOrEmpty(_letter) ? '\0' : _letter[0];

        /// <summary>
        /// 이 행의 칸 타일들. 부모(패널 화면)의 OnEnable이 자식의 Awake보다 먼저 돌므로, Awake에서 채우지 않고 처음 부를 때 찾는다.
        /// </summary>
        public ShelfSlotTile[] Tiles => _tiles ?? (_tiles = GetComponentsInChildren<ShelfSlotTile>(true));

        /// <summary>칸 타일에 칠할 행 색띠. ShowKit이 정한 키트 종류를 따른다.</summary>
        public Color StripColor { get; private set; } = DashboardPalette.Default.Track;

        private void Awake()
        {
            _letterText.text = _letter;
        }

        /// <summary>이 행에 놓이는 키트 종류를 알린다. 모르는 행은 빈 글자를 넘기고, 글자만 중립색으로 적는다.</summary>
        public void ShowKit(string kitCode)
        {
            DashboardPalette palette = DashboardPalette.Default;
            _kitCodeText.text = kitCode;
            _letterText.color = KitBadgePalette.Ink(kitCode, palette);
            StripColor = KitBadgePalette.Fill(kitCode, palette);
        }
    }
}
