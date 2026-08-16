using System.IO;
using UnityEditor;
using UnityEngine;

namespace PotteryHaptics.Core.Editor
{
    /// <summary>
    /// HapticCalibrationConfigを唯一の情報源として、JND用(弾性5+粘性5)・識別課題用(4)の
    /// StimulusDefinitionアセットをAssets/_Project/ScriptableObjects/Stimuli/配下に生成する。
    /// JND用5段階のうち0%の刺激は、TrialSequencerJNDの基準刺激(standardStimulus)としてもそのまま
    /// 流用する(基準と比較刺激が同一の"catch trial"を兼ねるため、別アセットを重複生成しない)。
    /// </summary>
    public static class StimulusSetGenerator
    {
        private const string OutputDirectory = "Assets/_Project/ScriptableObjects/Stimuli";

        [MenuItem("HapticResearch/Generate All Stimulus Sets")]
        public static void GenerateAll()
        {
            HapticCalibrationConfig config = CalibrationConfigProvider.GetOrCreate();
            GenerateElasticityJndSet(config);
            GenerateViscosityJndSet(config);
            GenerateIdentificationSet(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("HapticResearch/Generate Elasticity JND Set Only")]
        public static void GenerateElasticityJndSetMenuItem()
        {
            GenerateElasticityJndSet(CalibrationConfigProvider.GetOrCreate());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("HapticResearch/Generate Viscosity JND Set Only")]
        public static void GenerateViscosityJndSetMenuItem()
        {
            GenerateViscosityJndSet(CalibrationConfigProvider.GetOrCreate());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateElasticityJndSet(HapticCalibrationConfig config)
        {
            EnsureOutputDirectory();
            foreach (float percent in config.jndRelativePercents)
            {
                float physicalValue = config.elasticityBaseK * (1f + percent / 100f);
                string displayName = Mathf.Approximately(percent, 0f)
                    ? "弾性基準刺激(0%)"
                    : $"弾性比較刺激({FormatSignedPercent(percent)}%)";

                CreateOrUpdateStimulus(
                    GetJndAssetPath(TextureType.Elasticity, percent),
                    TextureType.Elasticity, physicalValue, percent, IntensityLevel.NotApplicable, displayName);
            }
        }

        public static void GenerateViscosityJndSet(HapticCalibrationConfig config)
        {
            EnsureOutputDirectory();
            foreach (float percent in config.jndRelativePercents)
            {
                float physicalValue = config.viscosityBaseB * (1f + percent / 100f);
                string displayName = Mathf.Approximately(percent, 0f)
                    ? "粘性基準刺激(0%)"
                    : $"粘性比較刺激({FormatSignedPercent(percent)}%)";

                CreateOrUpdateStimulus(
                    GetJndAssetPath(TextureType.Viscosity, percent),
                    TextureType.Viscosity, physicalValue, percent, IntensityLevel.NotApplicable, displayName);
            }
        }

        public static void GenerateIdentificationSet(HapticCalibrationConfig config)
        {
            EnsureOutputDirectory();

            CreateOrUpdateStimulus(
                GetIdentificationAssetPath(TextureType.Elasticity, IntensityLevel.Weak),
                TextureType.Elasticity, config.elasticityBaseK * config.identificationWeakRatio, 0f,
                IntensityLevel.Weak, "弾性(弱)");

            CreateOrUpdateStimulus(
                GetIdentificationAssetPath(TextureType.Elasticity, IntensityLevel.Strong),
                TextureType.Elasticity, config.elasticityBaseK * config.identificationStrongRatio, 0f,
                IntensityLevel.Strong, "弾性(強)");

            CreateOrUpdateStimulus(
                GetIdentificationAssetPath(TextureType.Viscosity, IntensityLevel.Weak),
                TextureType.Viscosity, config.viscosityBaseB * config.identificationWeakRatio, 0f,
                IntensityLevel.Weak, "粘性(弱)");

            CreateOrUpdateStimulus(
                GetIdentificationAssetPath(TextureType.Viscosity, IntensityLevel.Strong),
                TextureType.Viscosity, config.viscosityBaseB * config.identificationStrongRatio, 0f,
                IntensityLevel.Strong, "粘性(強)");
        }

        public static StimulusDefinition[] LoadElasticityJndSet(HapticCalibrationConfig config)
        {
            return LoadJndSet(TextureType.Elasticity, config);
        }

        public static StimulusDefinition[] LoadViscosityJndSet(HapticCalibrationConfig config)
        {
            return LoadJndSet(TextureType.Viscosity, config);
        }

        public static StimulusDefinition LoadStandardStimulus(TextureType type, HapticCalibrationConfig config)
        {
            return AssetDatabase.LoadAssetAtPath<StimulusDefinition>(GetJndAssetPath(type, 0f));
        }

        public static StimulusDefinition LoadIdentificationStimulus(TextureType type, IntensityLevel level)
        {
            return AssetDatabase.LoadAssetAtPath<StimulusDefinition>(GetIdentificationAssetPath(type, level));
        }

        private static StimulusDefinition[] LoadJndSet(TextureType type, HapticCalibrationConfig config)
        {
            var result = new StimulusDefinition[config.jndRelativePercents.Length];
            for (int i = 0; i < config.jndRelativePercents.Length; i++)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<StimulusDefinition>(GetJndAssetPath(type, config.jndRelativePercents[i]));
            }

            return result;
        }

        private static StimulusDefinition CreateOrUpdateStimulus(
            string assetPath, TextureType type, float physicalValue, float relativePercent,
            IntensityLevel level, string displayName)
        {
            StimulusDefinition stimulus = AssetDatabase.LoadAssetAtPath<StimulusDefinition>(assetPath);
            bool isNew = stimulus == null;
            if (isNew)
            {
                stimulus = ScriptableObject.CreateInstance<StimulusDefinition>();
            }

            stimulus.Initialize(type, physicalValue, relativePercent, level, displayName);

            if (isNew)
            {
                AssetDatabase.CreateAsset(stimulus, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(stimulus);
            }

            return stimulus;
        }

        private static string GetJndAssetPath(TextureType type, float percent)
        {
            string prefix = type == TextureType.Elasticity ? "Elasticity" : "Viscosity";
            return Path.Combine(OutputDirectory, $"{prefix}_JND_{FormatPercentToken(percent)}.asset");
        }

        private static string GetIdentificationAssetPath(TextureType type, IntensityLevel level)
        {
            string prefix = type == TextureType.Elasticity ? "Elasticity" : "Viscosity";
            return Path.Combine(OutputDirectory, $"{prefix}_Identification_{level}.asset");
        }

        private static string FormatPercentToken(float percent)
        {
            int rounded = Mathf.RoundToInt(percent);
            if (rounded == 0)
            {
                return "P000";
            }

            return rounded > 0 ? $"P{rounded}" : $"M{-rounded}";
        }

        private static string FormatSignedPercent(float percent)
        {
            return percent > 0f ? $"+{percent:0}" : $"{percent:0}";
        }

        private static void EnsureOutputDirectory()
        {
            if (!AssetDatabase.IsValidFolder(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
                AssetDatabase.Refresh();
            }
        }
    }
}
