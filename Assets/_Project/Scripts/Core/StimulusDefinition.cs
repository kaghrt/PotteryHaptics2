using UnityEngine;

namespace PotteryHaptics.Core
{
    public enum TextureType
    {
        Elasticity,
        Viscosity,
        Inertia // 今回のメイン実験からは除外。将来拡張のため列挙型のみ残す
    }

    public enum IntensityLevel
    {
        NotApplicable,
        Weak,
        Strong
    }

    [CreateAssetMenu(menuName = "HapticResearch/Stimulus Definition", fileName = "NewStimulusDefinition")]
    public class StimulusDefinition : ScriptableObject
    {
        [SerializeField] private TextureType textureType;
        [SerializeField] private float physicalValue;
        [SerializeField] private float relativeIntensityPercent;
        [SerializeField] private IntensityLevel intensityLevel = IntensityLevel.NotApplicable;
        [SerializeField] private string displayName;

        public TextureType TextureType => textureType;
        public float PhysicalValue => physicalValue;
        public float RelativeIntensityPercent => relativeIntensityPercent;
        public IntensityLevel IntensityLevel => intensityLevel;
        public string DisplayName => displayName;

        public void Initialize(
            TextureType type,
            float physicalVal,
            float relativePercent,
            IntensityLevel level,
            string name)
        {
            textureType = type;
            physicalValue = physicalVal;
            relativeIntensityPercent = relativePercent;
            intensityLevel = level;
            displayName = name;
        }
    }
}
