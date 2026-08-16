using System;
using PotteryHaptics.Core;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// CSVログの1行に対応する試行記録。JND/Identification両実験で共通のフォーマットを使う。
    /// </summary>
    [Serializable]
    public struct TrialRecord
    {
        public string participantId;
        public string phase;
        public VisualCondition visualCondition;
        public int trialIndex;
        public TextureType textureType;
        public string stimulusName;
        public float physicalValue;
        public float relativeIntensityPercent;
        public IntensityLevel intensityLevel;
        public string responseValue;
        public bool correct;
        public float reactionTimeSec;
        public string timestampIso;
    }
}
