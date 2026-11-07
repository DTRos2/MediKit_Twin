namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 주어진 난수로 배열을 제자리에서 섞는다(Fisher-Yates). 난수를 밖에서 받으므로 같은 시드면 같은 순서가 나온다.
    /// 랜덤 레시피가 칸 고르기, 슬롯 번호 매기기에 같이 쓰므로 따로 둔다.
    /// </summary>
    public static class RandomShuffle
    {
        public static void InPlace<T>(System.Random random, T[] items)
        {
            for (int last = items.Length - 1; last > 0; last--)
            {
                // Next의 위쪽 끝은 포함되지 않으므로 last + 1이면 0부터 last까지 고른다.
                int other = random.Next(last + 1);
                (items[last], items[other]) = (items[other], items[last]);
            }
        }
    }
}
