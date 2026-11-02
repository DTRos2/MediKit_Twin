using System;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// RecipeItem을 JsonUtility로 덮어쓰기 위한 사본. RecipeItem은 만드는 방법이 없는 구조체라 직접 채울 수 없다.
    /// 필드 이름이 RecipeItem의 직렬화 필드 이름(_componentId 등)과 같아야 JsonUtility가 같은 칸으로 읽는다.
    /// RecipeItem의 필드 이름을 바꾸면 이 파일도 바꿔야 하며, 어긋나면 RecipeContentRandomizerTests가 알려 준다.
    /// </summary>
    [Serializable]
    internal sealed class RecipeItemData
    {
        [SerializeField] private string _componentId;
        [SerializeField] private string _shelfSlotId;
        [SerializeField] private int _slotNumber;
        [SerializeField] private Vector3 _slotLocalPosition;

        public RecipeItemData(RandomRecipeItem item)
        {
            _componentId = item.ComponentId;
            _shelfSlotId = item.ShelfSlotId;
            _slotNumber = item.SlotNumber;
            _slotLocalPosition = item.SlotLocalPosition;
        }
    }
}
