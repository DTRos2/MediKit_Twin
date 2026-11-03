namespace MediKitTwin.Common
{
    /// <summary>
    /// 컨베이어 이송 속도를 읽고 바꾸는 접점. 속도를 보여 주고 조절하는 쪽(패널)이 컨베이어 에셋이나 구간 구성을 모르고
    /// 이 접점만으로 협력하게 한다. 구현체는 속도를 범위 안으로 맞춰 모든 구간에 같은 값을 넣는다.
    /// </summary>
    public interface IConveyorSpeedControl
    {
        /// <summary>지금 컨베이어에 넣은 이송 속도(m/s).</summary>
        float SpeedMps { get; }

        /// <summary>받아들이는 가장 느린 속도(m/s). 0보다 크다. 0이면 박스가 멈춰 서므로 받지 않는다.</summary>
        float MinSpeedMps { get; }

        /// <summary>받아들이는 가장 빠른 속도(m/s).</summary>
        float MaxSpeedMps { get; }

        /// <summary>속도를 바꾼다. 범위를 벗어난 값은 범위 끝으로 맞춘다. 실행 중에도 바로 반영된다.</summary>
        void SetSpeed(float speedMps);
    }
}
