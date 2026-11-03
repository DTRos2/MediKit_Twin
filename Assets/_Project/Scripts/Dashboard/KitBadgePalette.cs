using System;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 키트 종류(EM, DX, BL)에 정해진 색. 주문 줄의 배지와 선반 행의 띠와 행 글자가 같은 색을 쓰도록 한 곳에 둔다.
    /// 모르는 종류는 중립색이다. 대소문자는 가리지 않는다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class KitBadgePalette
    {
        private const string Em = "EM";
        private const string Dx = "DX";
        private const string Bl = "BL";

        /// <summary>배지 바탕이나 행 띠에 쓰는 색.</summary>
        public static Color Fill(string kitCode, DashboardPalette palette)
        {
            switch (Normalize(kitCode))
            {
                case Em: return palette.KitEm;
                case Dx: return palette.KitDx;
                case Bl: return palette.KitBl;
                default: return palette.Track;
            }
        }

        /// <summary>행 글자처럼 흰 바탕 위에서 읽히도록 더 짙게 한 색.</summary>
        public static Color Ink(string kitCode, DashboardPalette palette)
        {
            switch (Normalize(kitCode))
            {
                case Em: return palette.KitEmInk;
                case Dx: return palette.KitDxInk;
                case Bl: return palette.KitBlInk;
                default: return palette.Muted;
            }
        }

        private static string Normalize(string kitCode)
        {
            return (kitCode ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
