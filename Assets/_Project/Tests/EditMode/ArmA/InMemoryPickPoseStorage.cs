namespace MediKitTwin.ArmA.Tests
{
    /// <summary>글자를 메모리에 들고 있는 시험용 저장소. 몇 번 썼는지도 센다.</summary>
    internal sealed class InMemoryPickPoseStorage : IPickPoseStorage
    {
        public string Text { get; set; } = string.Empty;

        public int WriteCount { get; private set; }

        public string Read() => Text;

        public void Write(string text)
        {
            Text = text;
            WriteCount++;
        }
    }
}
