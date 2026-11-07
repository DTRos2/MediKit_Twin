#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Orders;
using MediKitTwin.Recipes;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 처음부터 끝까지 한 번에 확인한다: Scene_Cell을 열고, 화면의 "적용"과 같은 방법(IOrderInput.LoadText)으로 주문을 넣은 뒤,
    /// 로봇이 저장한 자세로 선반 앞에 가서 물건을 찾아 물리적으로 쥐고, 박스로 옮겨 놓고, 키트를 끝내는지(여러 키트면 이어서 다음 키트까지) 본다.
    /// 시험 도중에 로봇이 내는 경고와 오류는 정상 동작의 일부(재시도 등)일 수 있어 시험 실패로 치지 않고, 키팅 현황으로만 판정한다.
    /// 시간은 프레임당 고정(captureFramerate)이라 실제 시간보다 빠르게 흐를 수 있고, 같은 입력이면 같은 시간만큼 흐른다.
    /// 씬을 열려면 UnityEditor가 필요하므로 에디터에서만 돈다.
    /// </summary>
    public sealed class KittingEndToEndTests
    {
        private const string RobotName = "fr5_robot";
        private const string WatchedCell = "Shelf_A-1";
        private const string BoxName = "KitBox";

        // 주문 한 줄: 순번, 병원명, 키트 코드, 수량. EM 키트 하나에 구성품이 네 개라 집기와 놓기를 네 번 본다.
        private const string OneKitOrder = "1,시험병원,EM,1";
        private const string TwoKitOrder = "1,시험병원,EM,2";

        // 키트 코드마다 가져오는 선반 행이 다르다(EM은 A행, BL은 B행, DX는 C행). 행마다 알려 준 자세와 선반 모양이 달라 따로 본다.
        private const string BlKitOrder = "1,시험병원,BL,1";
        private const string DxKitOrder = "1,시험병원,DX,1";

        // 주문 입력 패널의 "샘플 채우기"와 같은 기본 주문: EM 2개, DX 3개, BL 2개로 키트 7개다.
        private const string DefaultOrder = "1,A병원,EM,2\n2,B병원,DX,3\n3,C병원,BL,2";
        private const int DefaultOrderKitCount = 7;

        private const float SimTimeoutSeconds = 900f;
        private const double WallTimeoutSeconds = 1500d;

        // 랜덤 주문은 키트 수가 시험마다 달라 제한 시간을 키트 수에 맞춘다. 키트 하나가 시뮬레이션 약 80초, 실제 약 45초이므로 여유를 둔다.
        private const float RandomSimBaseSeconds = 120f;
        private const float RandomSimSecondsPerKit = 150f;
        private const double RandomWallBaseSeconds = 120d;
        private const double RandomWallSecondsPerKit = 100d;
        private const int RandomTimeoutMilliseconds = 3600000;

        // 랜덤 주문의 한 줄 최대 수량과 병원명. 수량이 작을수록 키트 종류가 자주 바뀌어 번갈아 만드는 조합이 많이 나온다.
        private const int RandomMaxQuantityPerLine = 2;
        private static readonly string[] RandomHospitals = { "가병원", "나병원", "다병원" };

        // 구성품까지 섞는 시험에서 키트 하나가 담는 구성품 수. 한 개짜리 키트는 박스에 놓는 일이 한 번뿐이라 시험하는 것이 적고,
        // 실제 레시피는 모두 네 개이므로 세 개 이상으로 둔다. 네 개는 박스 슬롯 수의 한계다.
        private const int RandomMinItemsPerKit = 3;
        private const int RandomMaxItemsPerKit = 4;

        [UnityTest, Timeout(1800000)]
        public IEnumerator OneKit_IsPickedFromTheShelf_CarriedAndPlacedInTheBox()
        {
            KittingRun run = null;
            yield return OpenCellAndRun(OneKitOrder, 1, WatchedCell, made => run = made);

            AssertKitMade(run);
        }

        [UnityTest, Timeout(1800000)]
        public IEnumerator BlKit_IsMadeFromTheBRow()
        {
            KittingRun run = null;
            yield return OpenCellAndRun(BlKitOrder, 1, "Shelf_B-1", made => run = made);

            AssertKitMade(run);
        }

        [UnityTest, Timeout(1800000)]
        public IEnumerator DxKit_IsMadeFromTheCRow()
        {
            KittingRun run = null;
            yield return OpenCellAndRun(DxKitOrder, 1, "Shelf_C-1", made => run = made);

            AssertKitMade(run);
        }

        [UnityTest, Timeout(1800000)]
        public IEnumerator TwoKits_AreMadeOneAfterAnother()
        {
            KittingRun run = null;
            yield return OpenCellAndRun(TwoKitOrder, 2, WatchedCell, made => run = made);

            Assert.That(run.EndReason, Is.EqualTo(KittingRun.Done), run.Report);
            Assert.That(run.Recorder.KitsStarted, Is.EqualTo(2), "두 번째 키트를 시작하지 않았다\n" + run.Report);
            Assert.That(run.CompletedKits, Is.EqualTo(2), run.Report);
        }

        [UnityTest, Timeout(1800000)]
        public IEnumerator DefaultOrder_AllKitsAreMadeOneAfterAnother()
        {
            KittingRun run = null;
            yield return OpenCellAndRun(DefaultOrder, DefaultOrderKitCount, WatchedCell, made => run = made);

            Assert.That(run.EndReason, Is.EqualTo(KittingRun.Done), run.Report);
            Assert.That(run.Recorder.KitsStarted, Is.EqualTo(DefaultOrderKitCount), "키트를 모두 시작하지 않았다\n" + run.Report);
            Assert.That(run.CompletedKits, Is.EqualTo(DefaultOrderKitCount), run.Report);
            AssertPlacedCleanly(run);
        }

        /// <summary>
        /// 키트 코드와 수량을 시드로 섞은 주문을 돌린다. 정해 둔 주문이 놓치는 조합(종류를 번갈아 만들기, 같은 종류 반복)에서 생기는 실패를 찾는 시험이다.
        /// 시드와 키트 수는 환경 변수 MEDIKIT_E2E_SEED, MEDIKIT_E2E_KITS로 정하고 정하지 않으면 기본값이다. 실패 메시지에 시드와 주문이 나오므로
        /// 같은 시드를 다시 주면 같은 주문으로 재현된다. 키트 코드는 씬의 레시피에서 읽으므로 레시피를 늘리면 주문에도 나온다.
        /// </summary>
        [UnityTest, Timeout(RandomTimeoutMilliseconds)]
        public IEnumerator RandomOrder_EveryKitIsMadeAndPlacedCleanly()
        {
            RandomOrderSettings settings = RandomOrderSettings.FromEnvironment();
            GeneratedOrder generated = null;
            KittingRun run = null;

            yield return OpenCellAndRun(
                () =>
                {
                    generated = new RandomOrderGenerator(KitCodesInScene(), RandomHospitals, RandomMaxQuantityPerLine)
                        .Generate(settings.Seed, settings.Kits);
                    UnityEngine.Debug.Log($"[E2E] {generated.Describe()}\n{generated.Csv}");
                    return (generated.Csv, generated.KitCount);
                },
                WatchedCell,
                made => run = made,
                kits => RandomSimBaseSeconds + RandomSimSecondsPerKit * kits,
                kits => RandomWallBaseSeconds + RandomWallSecondsPerKit * kits);

            string context = generated.Describe() + "\n";
            Assert.That(run.EndReason, Is.EqualTo(KittingRun.Done), context + run.Report);
            Assert.That(run.Recorder.KitsStarted, Is.EqualTo(generated.KitCount), context + "키트를 모두 시작하지 않았다\n" + run.Report);
            Assert.That(run.CompletedKits, Is.EqualTo(generated.KitCount), context + run.Report);
            AssertPlacedCleanly(run, context);
        }

        /// <summary>
        /// 주문(키트 코드와 수량)에 더해 키트 안에 든 구성품까지 시드로 섞어 돌린다. 레시피가 바뀌어도(어떤 칸에서 무엇을 어떤 순서로 집어
        /// 어느 슬롯에 놓는가) 팔이 키트를 만드는지 본다. 칸 안의 물건은 씬에 놓여 있어 바꿀 수 없으므로 선반 12칸의 (칸, 구성품) 짝 안에서만 고른다.
        /// 레시피는 메모리의 객체만 바꾸고 시험이 끝나면(실패해도) 되돌리므로 에셋 파일은 그대로다.
        /// 시드와 키트 수는 랜덤 주문 시험과 같은 환경 변수(MEDIKIT_E2E_SEED, MEDIKIT_E2E_KITS)로 정한다.
        /// 한 칸의 시작 재고가 5개라 키트를 5개보다 많이 만들면 칸이 바닥나 보충을 기다릴 수 있다.
        /// 실패 메시지에 시드, 주문, 바꾼 구성이 나오므로 같은 시드를 다시 주면 같은 시험으로 재현된다.
        /// </summary>
        [UnityTest, Timeout(RandomTimeoutMilliseconds)]
        public IEnumerator RandomRecipeAndOrder_EveryKitIsMadeAndPlacedCleanly()
        {
            RandomOrderSettings settings = RandomOrderSettings.FromEnvironment();
            RecipeContentRandomizer randomizer = null;
            GeneratedOrder generated = null;
            string recipes = string.Empty;
            KittingRun run = null;

            try
            {
                yield return OpenCellAndRun(
                    () =>
                    {
                        // 씬이 열린 뒤, 주문을 넣기 전에 바꿔야 키트를 계획할 때 바뀐 레시피를 읽는다.
                        randomizer = new RecipeContentRandomizer(
                            Resources.FindObjectsOfTypeAll<RecipeSO>(), RandomMinItemsPerKit, RandomMaxItemsPerKit);
                        randomizer.Apply(settings.Seed);
                        recipes = randomizer.Describe();

                        generated = new RandomOrderGenerator(KitCodesInScene(), RandomHospitals, RandomMaxQuantityPerLine)
                            .Generate(settings.Seed, settings.Kits);
                        UnityEngine.Debug.Log($"[E2E] {generated.Describe()}\n{generated.Csv}\n[E2E] 바꾼 구성\n{recipes}");
                        return (generated.Csv, generated.KitCount);
                    },
                    WatchedCell,
                    made => run = made,
                    kits => RandomSimBaseSeconds + RandomSimSecondsPerKit * kits,
                    kits => RandomWallBaseSeconds + RandomWallSecondsPerKit * kits);
            }
            finally
            {
                // 다음 시험이 바뀐 레시피를 물려받지 않게, 시험이 실패하거나 중단돼도 되돌린다.
                randomizer?.Restore();
            }

            string context = generated.Describe() + "\n바꾼 구성\n" + recipes + "\n";
            Assert.That(run.EndReason, Is.EqualTo(KittingRun.Done), context + run.Report);
            Assert.That(run.Recorder.KitsStarted, Is.EqualTo(generated.KitCount), context + "키트를 모두 시작하지 않았다\n" + run.Report);
            Assert.That(run.CompletedKits, Is.EqualTo(generated.KitCount), context + run.Report);
            AssertPlacedCleanly(run, context);
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureFramerate = 0;
        }

        // 키트 하나가 끝났고, 구성품이 쥐어져 옮겨져 박스에 놓였는지 본다.
        private static void AssertKitMade(KittingRun run)
        {
            Assert.That(run.EndReason, Is.EqualTo(KittingRun.Done), run.Report);
            Assert.That(run.Recorder.WasEverCarrying, Is.True, "쥐고 옮긴 구성품이 하나도 없다\n" + run.Report);
            Assert.That(run.Recorder.WasEverPlaced, Is.True, "박스에 놓인 구성품이 하나도 없다\n" + run.Report);
            Assert.That(run.Placed.PlacedCount, Is.EqualTo(run.Recorder.LargestItemCount), "박스에 놓인 물건 수가 계획한 구성품 수와 다르다\n" + run.Report);
            Assert.That(run.Placed.NoneStillHeld, Is.True, "박스에 놓였는데 아직 쥐고 있다고 표시된 물건이 있다\n" + run.Report);
            Assert.That(run.Placed.AllInsideBox, Is.True, "박스의 자식이지만 박스 영역 밖에 있는 물건이 있다(튕겨 나갔거나 바닥판을 뚫었다)\n" + run.Report);
            AssertPlacedCleanly(run);
        }

        // 놓은 물건이 모두 제때 멈췄는지 본다. 멈추지 못한 채 고정된 물건이 하나라도 있으면 통과로 치지 않는다.
        // 선반에서보다 얼마나 기울어 놓였는지는 판정하지 않고 로그에 남긴다(물건마다 안정되는 방향이 달라 기준을 정하기 전에 값을 본다).
        // context는 실패 메시지 앞에 붙일 설명이다(랜덤 주문이면 시드와 주문).
        private static void AssertPlacedCleanly(KittingRun run, string context = "")
        {
            PhysicsGraspHolder holder = Object.FindAnyObjectByType<PhysicsGraspHolder>();
            Assert.That(holder, Is.Not.Null, "씬에서 PhysicsGraspHolder를 찾을 수 없다");
            UnityEngine.Debug.Log($"[E2E] 놓기: 멈추지 못한 물건 {holder.UnsettledPlacementCount}개, 선반에서보다 가장 많이 기운 각도 {holder.MaxPlacementTiltDeg:0}°");
            Assert.That(holder.UnsettledPlacementCount, Is.EqualTo(0), context + "시간 안에 멈추지 못하거나 박스 밖으로 나간 채 놓인 물건이 있다\n" + run.Report);
        }

        // 씬에 놓인 레시피의 키트 코드. 주문 입력이 검증에 쓰는 목록과 같은 곳(레시피 에셋)에서 읽는다.
        private static IEnumerable<string> KitCodesInScene()
        {
            return Resources.FindObjectsOfTypeAll<RecipeSO>().Select(recipe => recipe.KitCode);
        }

        // 정해진 주문을 넣고 키트를 끝낼 때까지 돌린다. watchedCell은 실패했을 때 자세를 자세히 적을 선반 칸 이름이다. 만든 실행 기록을 onRun으로 돌려준다.
        private static IEnumerator OpenCellAndRun(string order, int kits, string watchedCell, System.Action<KittingRun> onRun)
        {
            return OpenCellAndRun(() => (order, kits), watchedCell, onRun, _ => SimTimeoutSeconds, _ => WallTimeoutSeconds);
        }

        // 씬을 열고 나서 주문을 만들어 넣는다(주문이 씬의 레시피에 달려 있을 때 쓴다). 제한 시간은 키트 수로 정한다.
        private static IEnumerator OpenCellAndRun(
            System.Func<(string Csv, int Kits)> makeOrder, string watchedCell, System.Action<KittingRun> onRun,
            System.Func<int, float> simTimeoutFor, System.Func<int, double> wallTimeoutFor)
        {
            LogAssert.ignoreFailingMessages = true;
            FrameTimingSettings timing = FrameTimingSettings.FromEnvironment();
            int previousTargetFramerate = Application.targetFrameRate;
            int previousVSyncCount = QualitySettings.vSyncCount;
            ApplyFrameTiming(timing);

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            // 배속은 씬을 연 뒤에 건다. 배속 버튼이 부르는 TrySetSpeed와 같은 길이라, 허용하지 않는 배속이면 여기서 알린다.
            Assert.That(SimClock.TrySetSpeed(timing.Speed), Is.True, $"배속 {timing.Speed}배는 SimClock이 허용하는 배속이 아니다");

            var station = CellTestScene.FindOne<IKittingStation>();
            var input = CellTestScene.FindOne<IOrderInput>();
            GameObject robot = GameObject.Find(RobotName);
            Assert.That(station, Is.Not.Null, "씬에서 키팅 설비(IKittingStation)를 찾을 수 없다");
            Assert.That(input, Is.Not.Null, "씬에서 주문 입력(IOrderInput)을 찾을 수 없다");
            Assert.That(robot, Is.Not.Null, $"씬에서 팔({RobotName})을 찾을 수 없다");

            (string order, int kits) = makeOrder();
            var run = new KittingRun(
                station, input, robot, Object.FindAnyObjectByType<IkArmTarget>(), watchedCell, BoxName, simTimeoutFor(kits), wallTimeoutFor(kits));
            yield return run.Execute(order, kits);

            SimClock.TrySetSpeed(FrameTimingSettings.DefaultSpeed);
            Time.captureFramerate = 0;
            Application.targetFrameRate = previousTargetFramerate;
            QualitySettings.vSyncCount = previousVSyncCount;
            onRun(run);
        }

        // 프레임 시간을 고정하거나 프레임 수를 제한한다. 프레임 수를 제한하려면 수직 동기화가 꺼져 있어야 제한이 먹는다.
        private static void ApplyFrameTiming(FrameTimingSettings timing)
        {
            Time.captureFramerate = timing.CaptureFramerate;
            if (timing.TargetFramerate > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = timing.TargetFramerate;
            }

            Debug.Log($"[E2E] 시간 설정: 프레임 고정 {timing.CaptureFramerate}fps(0이면 고정 안 함), 프레임 상한 {timing.TargetFramerate}fps(0이면 제한 없음), 배속 {timing.Speed}배");
        }
    }
}
#endif
