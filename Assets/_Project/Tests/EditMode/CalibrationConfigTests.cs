using NUnit.Framework;
using PotteryHaptics.Core;
using PotteryHaptics.Core.Editor;

namespace PotteryHaptics.Tests.EditMode
{
    public class CalibrationConfigTests
    {
        [Test]
        public void CalibrationConfig_Exists_And_HasFiveJndLevels()
        {
            HapticCalibrationConfig config = CalibrationConfigProvider.GetOrCreate();

            Assert.IsNotNull(config, "HapticCalibrationConfigアセットが取得できること");
            Assert.AreEqual(5, config.jndRelativePercents.Length, "jndRelativePercentsは5段階であること");
        }
    }
}
