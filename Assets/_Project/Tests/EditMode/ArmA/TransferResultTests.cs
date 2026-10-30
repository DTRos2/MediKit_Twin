using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TransferResultTests
    {
        [Test]
        public void Fail_KeepsTheFailureAndItsDetail()
        {
            var result = new TransferResult();

            result.Fail(TransferFailure.MotionFailed, "물건 위치로 팔 뻗기: 시간 초과");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TransferFailure.MotionFailed));
            Assert.That(result.Detail, Is.EqualTo("물건 위치로 팔 뻗기: 시간 초과"));
        }

        [Test]
        public void Fail_WithoutDetail_LeavesItEmpty()
        {
            var result = new TransferResult();

            result.Fail(TransferFailure.GraspFailed);

            Assert.That(result.Detail, Is.Empty);
        }

        [Test]
        public void Succeed_ClearsAnEarlierFailureDetail()
        {
            var result = new TransferResult();
            result.Fail(TransferFailure.MotionFailed, "이전 실패");

            result.Succeed();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failure, Is.EqualTo(TransferFailure.None));
            Assert.That(result.Detail, Is.Empty);
        }
    }
}
