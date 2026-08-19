using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PotteryHaptics.Core;
using PotteryHaptics.Core.Editor;
using PotteryHaptics.Experiment;
using PotteryHaptics.Visual;

namespace PotteryHaptics.Tests.EditMode
{
    /// <summary>
    /// SceneBuilder.BuildEverything()実行後、各シーンの主要参照が正しく配線されていることを検証する。
    /// 過去に手動配線由来の未配線バグ(HapticOutputController欠落、視覚オブジェクト未配置等)が
    /// 発生したため、これらを機械的に再発防止する。
    /// </summary>
    public class SceneBuilderTests
    {
        private const string ScenesDirectory = "Assets/_Project/Scenes";

        [OneTimeSetUp]
        public void BuildAllScenesOnce()
        {
            SceneBuilder.BuildEverything();
        }

        [Test]
        public void JndElasticityScene_HasWiredReferences()
        {
            Scene scene = OpenScene("JND_Elasticity");

            var sequencer = FindComponent<TrialSequencerJND>(scene);
            Assert.IsNotNull(sequencer, "TrialSequencerJNDが存在すること");
            AssertFieldNotNull(sequencer, "surface");
            AssertFieldNotNull(sequencer, "responseUI");
            AssertFieldNotNull(sequencer, "standardStimulus");
            AssertComparisonStimuliNotEmpty(sequencer);

            AssertHapticOutputControllerHasSurfaces(scene);

            var deformationDriver = FindComponent<DeformationShaderDriver>(scene);
            Assert.IsNotNull(deformationDriver, "DeformationShaderDriverが存在すること(Rich映像固定)");
            AssertFieldNotNull(deformationDriver, "elasticitySurface");
            AssertFieldNotNull(deformationDriver, "targetRenderer");
        }

        [Test]
        public void JndViscosityScene_HasWiredReferences()
        {
            Scene scene = OpenScene("JND_Viscosity");

            var sequencer = FindComponent<TrialSequencerJND>(scene);
            Assert.IsNotNull(sequencer, "TrialSequencerJNDが存在すること");
            AssertFieldNotNull(sequencer, "surface");
            AssertFieldNotNull(sequencer, "responseUI");
            AssertFieldNotNull(sequencer, "standardStimulus");
            AssertComparisonStimuliNotEmpty(sequencer);

            AssertHapticOutputControllerHasSurfaces(scene);

            var stretchDriver = FindComponent<MudStretchDriver>(scene);
            Assert.IsNotNull(stretchDriver, "MudStretchDriverが存在すること(Rich映像固定)");
            AssertFieldNotNull(stretchDriver, "viscositySurface");
            AssertFieldNotNull(stretchDriver, "stretchTarget");
        }

        [Test]
        public void IdentificationScene_HasWiredReferencesAndBothSurfaces()
        {
            Scene scene = OpenScene("Identification");

            var sequencer = FindComponent<TrialSequencerIdentification>(scene);
            Assert.IsNotNull(sequencer, "TrialSequencerIdentificationが存在すること");
            AssertFieldNotNull(sequencer, "elasticitySurface");
            AssertFieldNotNull(sequencer, "viscositySurface");
            AssertFieldNotNull(sequencer, "responseUI");
            AssertFieldNotNull(sequencer, "elasticityWeakStimulus");
            AssertFieldNotNull(sequencer, "elasticityStrongStimulus");
            AssertFieldNotNull(sequencer, "viscosityWeakStimulus");
            AssertFieldNotNull(sequencer, "viscosityStrongStimulus");

            Assert.IsNotNull(FindComponent<ElasticitySurface>(scene), "Identificationシーンに弾性Surfaceが存在すること");
            Assert.IsNotNull(FindComponent<ViscositySurface>(scene), "Identificationシーンに粘性Surfaceが存在すること");

            AssertHapticOutputControllerHasSurfaces(scene);

            var neutralDriver = FindComponent<NeutralInteractiveVisualDriver>(scene);
            Assert.IsNotNull(neutralDriver, "質感によらない共通のNeutralInteractiveVisualDriverが存在すること");
            AssertFieldNotNull(neutralDriver, "elasticitySurface");
            AssertFieldNotNull(neutralDriver, "viscositySurface");
            AssertFieldNotNull(neutralDriver, "visualTransform");

            // 弾性用/粘性用に別々の視覚オブジェクト(器/泥)を出し分ける実装は、見た目から
            // 正答が漏れるため廃止した。共通オブジェクトが1つだけ存在することを確認する。
            int neutralDriverCount = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<NeutralInteractiveVisualDriver>(true))
                .Count();
            Assert.AreEqual(1, neutralDriverCount, "NeutralInteractiveVisualDriverは1つだけ存在すること");
        }

        private static Scene OpenScene(string sceneName)
        {
            string path = $"{ScenesDirectory}/{sceneName}.unity";
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void AssertFieldNotNull(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"フィールド {fieldName} が見つかること");
            object value = field.GetValue(target);
            Assert.IsNotNull(value, $"{target.GetType().Name}.{fieldName} がnullでないこと");
        }

        private static void AssertComparisonStimuliNotEmpty(object sequencer)
        {
            FieldInfo field = sequencer.GetType().GetField("comparisonStimuli", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "comparisonStimuliフィールドが見つかること");
            var array = field.GetValue(sequencer) as StimulusDefinition[];
            Assert.IsNotNull(array, "comparisonStimuliが配列であること");
            Assert.IsTrue(array.Length > 0 && array.All(s => s != null), "comparisonStimuliの全要素がnullでないこと");
        }

        private static void AssertHapticOutputControllerHasSurfaces(Scene scene)
        {
            var output = FindComponent<HapticOutputController>(scene);
            Assert.IsNotNull(output, "HapticOutputControllerが存在すること");
            Assert.IsTrue(output.Surfaces.Count > 0, "HapticOutputController.surfacesが空でないこと");
        }
    }
}
