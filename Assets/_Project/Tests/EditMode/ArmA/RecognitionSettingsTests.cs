using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class RecognitionSettingsTests
    {
        // 그리퍼가 도구 끝점 반경 6cm 안의 물건을 쥔다(JointGripper의 기본 쥐는 반경).
        private const float GripperGraspRadiusM = 0.06f;

        [Test]
        public void TheDefaultCorrectionLimit_CoversTheGraspRadius()
        {
            // 한계가 쥐는 반경보다 작으면 알려 준 자세가 조금만 벗어나도 고치다 만다.
            Assert.That(new RecognitionSettings().MaxCorrectionM, Is.GreaterThan(GripperGraspRadiusM));
        }

        [Test]
        public void TheCorrectionLimit_IsNeverBelowTheMinimum()
        {
            RecognitionSettings settings = PerceptionTestData.Recognition(false, 0.05f, 0.01f);

            Assert.That(settings.MaxCorrectionM, Is.GreaterThan(settings.MinCorrectionM));
        }
    }
}
