using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 수동 키팅을 씬에 붙이는 얇은 컴포넌트. 협력자와 설정을 받아 순수 클래스(ManualKitter, ManualKittingSession)를 만들고,
    /// 수동 모드를 켜고 끌 때 자동 키팅을 멈추고 다시 돌리며, 지시받은 칸을 코루틴으로 실행한다.
    /// 집어 놓는 판단은 순수 클래스가 하고, 화면은 IManualKitting으로만 이 클래스를 안다.
    /// </summary>
    public sealed class ManualKittingStation : MonoBehaviour, IManualKitting
    {
        [Header("협력자 (인터페이스를 구현한 컴포넌트)")]
        [Tooltip("자재 재고(IStock, 보통 ShelfStockPresenter).")]
        [SerializeField] private MonoBehaviour _stock;
        [Tooltip("선반 물건 위치(IShelfItemLocator, 보통 ShelfStockPresenter).")]
        [SerializeField] private MonoBehaviour _itemLocator;
        [Tooltip("박스 공급과 슬롯 위치(IBoxSupply와 IKitBoxLocator를 모두 구현, 보통 KitBoxSupply).")]
        [SerializeField] private MonoBehaviour _boxSupply;
        [Tooltip("구성품 이송(IItemTransfer, 보통 ArmItemTransfer). 자동 키팅과 같은 것을 쓴다.")]
        [SerializeField] private MonoBehaviour _itemTransfer;
        [Tooltip("수동 모드 동안 자동 키팅을 멈출 대상(IStationPause, 보통 KittingController).")]
        [SerializeField] private MonoBehaviour _autoKitting;

        [Header("설정")]
        [Tooltip("고를 수 있는 키트 종류. 첫 번째가 처음 선택된다.")]
        [SerializeField] private List<RecipeSO> _recipes = new List<RecipeSO>();
        [Tooltip("박스가 지그에 놓이기를 기다리는 최대 시간(초).")]
        [SerializeField, Min(1f)] private float _boxWaitTimeoutSeconds = 10f;
        [Tooltip("재고가 비었을 때 알릴 주문 순번. 0은 어떤 주문도 아니다.")]
        [SerializeField, Min(0)] private int _orderSequence;

        private readonly ManualKittingSession _session = new ManualKittingSession();
        private readonly List<string> _kitCodes = new List<string>();
        private ManualKitter _kitter;
        private IBoxSupply _box;
        private IStationPause _autoPause;

        public bool IsManualMode { get; private set; }

        public bool IsBusy { get; private set; }

        public string Message { get; private set; } = string.Empty;

        public bool CanRequest => IsManualMode && !IsBusy && _autoPause.IsPaused;

        public IReadOnlyList<string> KitCodes => _kitCodes;

        public string SelectedKitCode
        {
            get => _session.Recipe != null ? _session.Recipe.KitCode : string.Empty;
            set => SelectRecipe(value);
        }

        private void Awake()
        {
            var stock = _stock as IStock;
            var itemLocator = _itemLocator as IShelfItemLocator;
            _box = _boxSupply as IBoxSupply;
            var boxLocator = _boxSupply as IKitBoxLocator;
            var transfer = _itemTransfer as IItemTransfer;
            _autoPause = _autoKitting as IStationPause;

            if (stock == null || itemLocator == null || _box == null || boxLocator == null || transfer == null || _autoPause == null)
            {
                Debug.LogError($"{name}: Stock, Item Locator, Box Supply, Item Transfer, Auto Kitting을 모두 지정해야 한다.", this);
                enabled = false;
                return;
            }

            foreach (RecipeSO recipe in _recipes)
            {
                if (recipe != null)
                {
                    _kitCodes.Add(recipe.KitCode);
                }
            }

            var boxAwaiter = new BoxAwaiter(_box, boxLocator, () => SimClock.Now, _boxWaitTimeoutSeconds);
            var filler = new ItemFiller(stock, boxLocator, itemLocator, transfer);
            _kitter = new ManualKitter(_session, stock, boxLocator, boxAwaiter, filler, transfer, _orderSequence);

            if (_kitCodes.Count > 0)
            {
                SelectRecipe(_kitCodes[0]);
            }
        }

        private void OnDisable()
        {
            // 수동 모드인 채로 꺼지면 자동 키팅이 영영 멈춰 있지 않도록 풀어 준다.
            if (IsManualMode && _autoPause != null)
            {
                _autoPause.Resume();
                IsManualMode = false;
            }
        }

        public void SetManualMode(bool isOn)
        {
            if (isOn == IsManualMode)
            {
                return;
            }

            if (!isOn && IsBusy)
            {
                Message = "로봇이 움직이는 동안에는 끌 수 없다.";
                return;
            }

            IsManualMode = isOn;
            if (isOn)
            {
                _autoPause.RequestPause();
                Message = "자동 키팅이 하던 키트를 끝내고 멈추기를 기다린다.";
            }
            else
            {
                _autoPause.Resume();
                Message = "자동 키팅을 다시 시작했다.";
            }
        }

        public bool TryRequestPick(string shelfSlotId)
        {
            if (!CanRequest)
            {
                Message = !IsManualMode
                    ? "수동 모드를 먼저 켠다."
                    : IsBusy ? "로봇이 움직이는 중이다." : "자동 키팅이 하던 키트를 끝내고 멈추기를 기다린다.";
                return false;
            }

            StartCoroutine(Fill(shelfSlotId));
            return true;
        }

        public void StartNewBox()
        {
            if (IsBusy)
            {
                Message = "로봇이 움직이는 중이다.";
                return;
            }

            _box.RemoveBox();
            _session.Reset();
            Message = "박스를 치웠다. 새 박스가 내려오면 다시 채운다.";
        }

        private void SelectRecipe(string kitCode)
        {
            foreach (RecipeSO recipe in _recipes)
            {
                if (recipe != null && recipe.KitCode == kitCode)
                {
                    _session.SelectRecipe(recipe);
                    return;
                }
            }
        }

        private IEnumerator Fill(string shelfSlotId)
        {
            IsBusy = true;
            Message = $"{shelfSlotId}로 이동해 집는 중...";

            var result = new ManualFillResult();
            yield return _kitter.Fill(shelfSlotId, result);

            Message = result.Message;
            IsBusy = false;
        }
    }
}
