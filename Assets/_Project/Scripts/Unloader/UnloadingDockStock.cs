using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;
using Warcool.WarehouseRobot;

namespace MediKitTwin.Unloader
{
    /// <summary>
    /// 하역장(UnloadingDock) 받침 위에 놓인 박스를 키트 종류별 구역(줄)으로 관리한다. (FR-12)
    /// 구역(열)마다 칸이 하나이고, 로봇은 그 칸(가장자리)에만 박스를 내려놓을 수 있다. 박스가 놓이면 잠시 뒤 그 구역의 박스를 모두
    /// 받침 안쪽(밀기 방향)으로 한 칸(Pitch)씩 밀어 가장자리 칸을 비운다. 줄이 정원을 넘으면 가장 오래된(가장 먼) 박스를 치우고 경고한다.
    /// 구역 번호는 KitCodes의 순서이다. 박스를 어느 구역에 놓을지는 키트 코드(ColumnFor)로 정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnloadingDockStock : MonoBehaviour
    {
        public const string FullWarningCode = "DOCK_ZONE_FULL";

        // 로봇 팔 높이가 칸 번호로 정해지고 하역은 가장 낮은 칸(0)만 쓴다. 구역마다 칸은 이것 하나이다.
        private const int EdgeLevel = 0;

        [Tooltip("구역(열)마다 칸 하나를 가진 ShelfStorage.")]
        [SerializeField] private ShelfStorage _storage;

        [Tooltip("구역 번호 순서의 ShelfInteractionZone. 로봇이 서야 할 자리(dockPoint)를 여기서 읽는다.")]
        [SerializeField] private ShelfInteractionZone[] _zones;

        [Tooltip("가장자리에서 밀려난 박스를 모아 두는 오브젝트. 비균일 스케일이 아니어야 박스가 찌그러지지 않는다.")]
        [SerializeField] private Transform _stockRoot;

        [Tooltip("구역 번호 순서의 키트 코드(대문자). 같은 코드의 키트는 같은 구역에 놓는다.")]
        [SerializeField] private string[] _kitCodes = { "EM", "BL", "DX" };

        [Tooltip("박스를 미는 월드 방향. 가장자리에서 받침 안쪽을 향한다.")]
        [SerializeField] private Vector3 _shiftDirection = Vector3.right;

        [Tooltip("박스 한 칸의 간격(m). 박스가 미는 방향으로 차지하는 길이(0.098)보다 커야 겹치지 않는다.")]
        [SerializeField, Min(0.01f)] private float _pitchM = 0.14f;

        [Tooltip("구역마다 둘 수 있는 박스 수. 넘으면 가장 오래된 박스를 치운다.")]
        [SerializeField, Min(1)] private int _capacityPerZone = 10;

        [Tooltip("박스를 내려놓은 뒤 밀기 시작하기까지 기다리는 시뮬레이션 시간(초). 로봇 팔이 빠져나올 시간이다.")]
        [SerializeField, Min(0f)] private float _shiftDelaySeconds = 1f;

        [Tooltip("박스를 미는 속도(m/s).")]
        [SerializeField, Min(0.01f)] private float _shiftSpeedMps = 0.2f;

        private List<Transform>[] _lines;
        private bool[] _shifting;

        public ShelfStorage Storage => _storage;

        public int ZoneCount => _kitCodes != null ? _kitCodes.Length : 0;

        private void Awake()
        {
            if (_storage == null || _stockRoot == null || _kitCodes == null || _kitCodes.Length == 0)
            {
                Debug.LogError($"{name}: Storage, Stock Root, Kit Codes를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _lines = new List<Transform>[_kitCodes.Length];
            _shifting = new bool[_kitCodes.Length];
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i] = new List<Transform>();
            }
        }

        private void Update()
        {
            for (int column = 0; column < _lines.Length; column++)
            {
                if (!_shifting[column] && _storage.GetBox(column, EdgeLevel) != null)
                {
                    StartCoroutine(ShiftLine(column));
                }
            }
        }

        /// <summary>키트 코드(대소문자 무시)에 맞는 구역 번호. 없으면 -1.</summary>
        public int ColumnFor(string kitCode)
        {
            if (string.IsNullOrEmpty(kitCode))
            {
                return -1;
            }

            for (int i = 0; i < _kitCodes.Length; i++)
            {
                if (string.Equals(_kitCodes[i], kitCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>구역의 ShelfInteractionZone(로봇 자리 dockPoint를 가진다). 범위 밖이면 null.</summary>
        public ShelfInteractionZone ZoneFor(int column)
        {
            return _zones != null && column >= 0 && column < _zones.Length ? _zones[column] : null;
        }

        private IEnumerator ShiftLine(int column)
        {
            _shifting[column] = true;

            float waited = 0f;
            while (waited < _shiftDelaySeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            CargoBox placed = _storage.GetBox(column, EdgeLevel);
            if (placed == null)
            {
                // 기다리는 동안 로봇이 박스를 도로 집어 갔다.
                _shifting[column] = false;
                yield break;
            }

            // 칸에서 빼 모아 두는 곳으로 옮긴다. 월드 위치는 그대로라 박스가 움직여 보이지 않는다.
            _storage.RemoveBox(column, EdgeLevel);
            placed.transform.SetParent(_stockRoot, true);

            List<Transform> line = _lines[column];
            line.RemoveAll(box => box == null);
            line.Insert(0, placed.transform);

            if (line.Count > _capacityPerZone)
            {
                Transform oldest = line[line.Count - 1];
                line.RemoveAt(line.Count - 1);
                Destroy(oldest.gameObject);
                CellEvents.RaiseWarning(FullWarningCode, $"하역장 구역 {_kitCodes[column]}이 가득 차 가장 오래된 박스를 치웠다.");
            }

            Vector3 step = _shiftDirection.normalized * _pitchM;
            var targets = new Vector3[line.Count];
            for (int i = 0; i < line.Count; i++)
            {
                targets[i] = line[i].position + step;
            }

            bool moving = true;
            while (moving)
            {
                moving = false;
                for (int i = 0; i < line.Count; i++)
                {
                    if (line[i] == null)
                    {
                        continue;
                    }

                    line[i].position = Vector3.MoveTowards(line[i].position, targets[i], _shiftSpeedMps * Time.deltaTime);
                    if (line[i].position != targets[i])
                    {
                        moving = true;
                    }
                }

                yield return null;
            }

            _shifting[column] = false;
        }
    }
}
