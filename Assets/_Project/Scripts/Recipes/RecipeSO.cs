using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Recipes
{
    /// <summary>
    /// 키트 종류 하나의 레시피. 로봇암 A(선반 위치, 슬롯 좌표), 검사(정답 목록), 데이터 생성기, 박스 공급부가 같은 레시피를 읽는다.
    /// 키트 종류를 추가할 때는 이 에셋을 하나 더 만들고 스크립트는 고치지 않는다. (NFR-07)
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Recipe", fileName = "Recipe_")]
    public sealed class RecipeSO : ScriptableObject
    {
        [Tooltip("키트 종류 코드(EM, BL, DX). 대문자로 쓰고 레시피끼리 겹치면 안 된다.")]
        [SerializeField] private string _kitCode;
        [SerializeField] private BoxSize _boxSize;

        [Tooltip("피킹 순서대로 나열한다. 항목 1개가 슬롯 1칸이다.")]
        [SerializeField] private List<RecipeItem> _items = new List<RecipeItem>();

        public string KitCode => _kitCode;

        public BoxSize BoxSize => _boxSize;

        public IReadOnlyList<RecipeItem> Items => _items;
    }
}
