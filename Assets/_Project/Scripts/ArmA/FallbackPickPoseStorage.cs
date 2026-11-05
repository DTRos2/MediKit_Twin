using System;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세 모음을 먼저 쓰는 저장소(primary)에서 읽고, 거기에 아직 아무것도 없으면 기본값을 담은 저장소(fallback)에서 읽는다.
    /// 쓸 때는 언제나 primary에만 쓴다. fallback은 바꾸지 않는다.
    /// 빌드에서 사용자 데이터 폴더에 자세 파일이 아직 없을 때, 빌드에 실어 둔 기본 자세를 대신 쓰려고 만들었다.
    /// 자세를 하나 저장하면 TaughtPickPoseRepository가 읽은 전체(fallback 내용 포함)에 그 자세를 더해 primary에 쓰므로, 그때부터는 primary가 전부 대신한다.
    /// </summary>
    public sealed class FallbackPickPoseStorage : IPickPoseStorage
    {
        private readonly IPickPoseStorage _primary;
        private readonly IPickPoseStorage _fallback;

        public FallbackPickPoseStorage(IPickPoseStorage primary, IPickPoseStorage fallback)
        {
            _primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public string Read()
        {
            string text = _primary.Read();
            return string.IsNullOrWhiteSpace(text) ? _fallback.Read() : text;
        }

        public void Write(string text)
        {
            _primary.Write(text);
        }
    }
}
