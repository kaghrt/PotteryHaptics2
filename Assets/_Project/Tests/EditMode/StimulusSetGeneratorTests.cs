using System.IO;
using NUnit.Framework;
using UnityEditor;
using PotteryHaptics.Core.Editor;

namespace PotteryHaptics.Tests.EditMode
{
    public class StimulusSetGeneratorTests
    {
        private const string StimuliDirectory = "Assets/_Project/ScriptableObjects/Stimuli";

        [Test]
        public void GenerateAll_Creates14StimulusAssets()
        {
            StimulusSetGenerator.GenerateAll();
            AssetDatabase.Refresh();

            Assert.IsTrue(Directory.Exists(StimuliDirectory), "刺激アセットの出力先フォルダが存在すること");

            string[] assetPaths = Directory.GetFiles(StimuliDirectory, "*.asset", SearchOption.TopDirectoryOnly);
            Assert.AreEqual(14, assetPaths.Length,
                "弾性JND5 + 粘性JND5 + 識別課題4 = 14アセットが生成されること");
        }
    }
}
