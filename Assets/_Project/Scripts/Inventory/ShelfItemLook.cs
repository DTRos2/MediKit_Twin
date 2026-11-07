using System;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>구성품 한 종류의 자리표시 모양: 모양, 전체 크기(m, 원기둥은 x=지름·y=높이), 색.</summary>
    [Serializable]
    public struct ShelfItemLook
    {
        [Tooltip("레시피의 구성품 ID. 슬롯 감지 센서가 보고하는 ID와 같다.")]
        [SerializeField] private string _componentId;
        [SerializeField] private ShelfItemShape _shape;
        [Tooltip("전체 크기(m). 원기둥은 x가 지름, y가 높이이다.")]
        [SerializeField] private Vector3 _size;
        [SerializeField] private Color _color;

        public ShelfItemLook(string componentId, ShelfItemShape shape, Vector3 size, Color color)
        {
            _componentId = componentId;
            _shape = shape;
            _size = size;
            _color = color;
        }

        public string ComponentId => _componentId;

        public ShelfItemShape Shape => _shape;

        public Vector3 Size => _size;

        public Color Color => _color;
    }
}
