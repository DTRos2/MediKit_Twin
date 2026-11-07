using MediKitTwin.Dashboard;
using MediKitTwin.Orders;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>주문 한 건을 보여 주는 목록 줄(프리팹): 순번, 병원 이름, 키트 종류 배지, 수량. 어디에 무엇이 놓이는지는 프리팹이 정하고 이 클래스는 값만 채운다.</summary>
    public sealed class OrderRowItem : MonoBehaviour
    {
        [SerializeField] private Text _sequence;
        [SerializeField] private Text _hospital;
        [SerializeField] private Image _badge;
        [SerializeField] private Text _kitCode;
        [SerializeField] private Text _quantity;

        public void Show(OrderRecord order)
        {
            _sequence.text = order.Sequence.ToString();
            _hospital.text = order.HospitalName;
            _kitCode.text = order.KitCode;
            _badge.color = KitBadgePalette.Fill(order.KitCode, DashboardPalette.Default);
            _quantity.text = OrderPanelTexts.Quantity(order.Quantity);
        }
    }
}
