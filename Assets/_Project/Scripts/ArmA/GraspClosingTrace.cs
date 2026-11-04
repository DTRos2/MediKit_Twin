using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 그리퍼를 닫는 동안 손가락과 쥘 물건이 얼마나 가까워졌는지 물리 스텝마다 재어 두었다가, 쥐지 못했을 때 이유 글에 붙인다.
    /// "손가락이 물건에 닿지 않고 끝까지 닫혔다"만으로는 물건이 손가락 쪽으로 오지 않았는지(자세가 어긋남), 손가락이 물건을 지나쳤는지(충돌이 안 됨),
    /// 물건이 손가락 사이에 있었는데 얇아서 판정이 놓쳤는지 가를 수 없어서, 닫는 동안의 가장 가까운 거리와 그때의 열림, 물건의 충돌 설정을 남긴다.
    /// 거리는 충돌체를 감싸는 축 정렬 상자(Bounds) 사이의 거리다. 기울어진 충돌체는 실제보다 가깝게 나오지만 어긋남을 가르기에는 충분하다.
    /// </summary>
    public sealed class GraspClosingTrace
    {
        private readonly List<float> _closestM = new List<float>();
        private readonly List<float> _openingAtClosest = new List<float>();
        private readonly List<string> _fingerNames = new List<string>();
        private int _sampleCount;
        private float _finalOpening = 1f;

        /// <summary>새 쥐기를 시작하기 전에 비운다.</summary>
        public void Reset()
        {
            _closestM.Clear();
            _openingAtClosest.Clear();
            _fingerNames.Clear();
            _sampleCount = 0;
            _finalOpening = 1f;
        }

        /// <summary>물리 스텝 하나마다 부른다. opening은 그리퍼의 지금 열림(1이 열림, 0이 닫힘).</summary>
        public void Sample(IReadOnlyList<Collider> fingers, Collider[] itemColliders, float opening)
        {
            Bounds item;
            if (fingers == null || !TryMergeBounds(itemColliders, out item))
            {
                return;
            }

            _sampleCount++;
            _finalOpening = opening;
            EnsureSlots(fingers);
            for (int i = 0; i < fingers.Count; i++)
            {
                if (fingers[i] == null)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(item.SqrDistance(fingers[i].bounds.ClosestPoint(item.center)));
                if (distance < _closestM[i])
                {
                    _closestM[i] = distance;
                    _openingAtClosest[i] = opening;
                }
            }
        }

        /// <summary>닫는 동안의 거리 기록과 지금 물건·손가락의 충돌 설정을 사람이 읽는 글로 만든다.</summary>
        public string Describe(IReadOnlyList<Collider> fingers, Collider[] itemColliders)
        {
            var text = new StringBuilder();
            AppendInvariant(text, $"닫는 동안 {_sampleCount}번 재었고 마지막 열림 {_finalOpening:0.00}.");

            for (int i = 0; i < _closestM.Count; i++)
            {
                AppendInvariant(text,
                    $" 손가락 {_fingerNames[i]}: 물건에 가장 가까웠던 거리 {_closestM[i] * 100f:0.0}cm(열림 {_openingAtClosest[i]:0.00}).");
            }

            AppendItem(text, itemColliders);
            AppendFingers(text, fingers, itemColliders);
            return text.ToString();
        }

        // 숫자는 어느 나라 설정에서도 소수점이 점이도록 고정 문화권으로 글을 만든다.
        private static void AppendInvariant(StringBuilder text, FormattableString value)
        {
            text.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private void EnsureSlots(IReadOnlyList<Collider> fingers)
        {
            while (_closestM.Count < fingers.Count)
            {
                Collider finger = fingers[_closestM.Count];
                _closestM.Add(float.MaxValue);
                _openingAtClosest.Add(1f);
                _fingerNames.Add(finger != null ? finger.name : "(없음)");
            }
        }

        private static void AppendItem(StringBuilder text, Collider[] itemColliders)
        {
            if (!TryMergeBounds(itemColliders, out Bounds item))
            {
                text.Append(" 물건에 충돌체가 없다.");
                return;
            }

            AppendInvariant(text,
                $" 물건 상자: 중심 ({item.center.x:0.000}, {item.center.y:0.000}, {item.center.z:0.000}) 크기 ({item.size.x * 100f:0.0}, {item.size.y * 100f:0.0}, {item.size.z * 100f:0.0})cm.");

            foreach (Collider collider in itemColliders)
            {
                if (collider == null)
                {
                    continue;
                }

                AppendInvariant(text,
                    $" 물건 충돌체 {collider.GetType().Name}: 레이어 {LayerMask.LayerToName(collider.gameObject.layer)}, 켜짐 {collider.enabled}, 제외 레이어 {collider.excludeLayers.value}.");
            }
        }

        private static void AppendFingers(StringBuilder text, IReadOnlyList<Collider> fingers, Collider[] itemColliders)
        {
            if (fingers == null || !TryMergeBounds(itemColliders, out Bounds item))
            {
                return;
            }

            int itemLayer = FirstLayer(itemColliders);
            for (int i = 0; i < fingers.Count; i++)
            {
                Collider finger = fingers[i];
                if (finger == null)
                {
                    continue;
                }

                Bounds b = finger.bounds;
                bool collidesByLayer = itemLayer >= 0 && !Physics.GetIgnoreLayerCollision(itemLayer, finger.gameObject.layer);
                AppendInvariant(text, $" 손가락 {finger.name} 지금: 중심 ({b.center.x:0.000}, {b.center.y:0.000}, {b.center.z:0.000}) 크기 ({b.size.x * 100f:0.0}, {b.size.y * 100f:0.0}, {b.size.z * 100f:0.0})cm,");
                AppendInvariant(text, $" 물건 상자와 겹침 {b.Intersects(item)}, 레이어 {LayerMask.LayerToName(finger.gameObject.layer)},");
                AppendInvariant(text, $" 레이어끼리 충돌 {collidesByLayer}, 제외 레이어 {finger.excludeLayers.value}.");
            }
        }

        private static int FirstLayer(Collider[] colliders)
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    return collider.gameObject.layer;
                }
            }

            return -1;
        }

        // 곧 지워질 옛 충돌체(null)는 건너뛰고 남은 충돌체의 상자를 하나로 합친다.
        private static bool TryMergeBounds(Collider[] colliders, out Bounds merged)
        {
            merged = default;
            bool hasBounds = false;
            if (colliders == null)
            {
                return false;
            }

            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    merged = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    merged.Encapsulate(collider.bounds);
                }
            }

            return hasBounds;
        }
    }
}
