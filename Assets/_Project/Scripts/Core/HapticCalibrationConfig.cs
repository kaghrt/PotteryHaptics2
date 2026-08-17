using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 実機キャリブレーション定数の唯一の情報源(single source of truth)。
    /// StimulusSetGeneratorおよび各VirtualSurfaceBase実装の初期値は、このアセットを参照する。
    /// Assets/_Project/ScriptableObjects/Calibration/HapticCalibrationConfig.asset に1つだけ配置する。
    /// </summary>
    [CreateAssetMenu(menuName = "HapticResearch/Haptic Calibration Config", fileName = "HapticCalibrationConfig")]
    public class HapticCalibrationConfig : ScriptableObject
    {
        [Header("弾性")]
        public float elasticityBaseK = 1.0f; // 仮値。実機調整後に差し替える
        public float elasticityContactStartHeightCm = 25f;
        public float elasticityMaxPushHeightCm = 5f;

        [Header("粘性")]
        public float viscosityBaseB = 1.0f; // 仮値
        public float viscosityFixedHeightCm = 15f;
        public float viscosityHeightToleranceCm = 5f;
        public float viscosityMinSpeedCmPerSec = 0.5f;

        [Header("識別課題用の強弱倍率")]
        public float identificationWeakRatio = 0.7f;
        public float identificationStrongRatio = 1.3f;

        [Header("JND用の相対強度[%](恒常法5段階)")]
        public float[] jndRelativePercents = { -30f, -15f, 0f, 15f, 30f };

        [Header("実機出力の正規化")]
        [Tooltip("この値のforceで、超音波の出力強度が最大(intensity=1.0)になる。実機調整後に差し替える。")]
        public float maxForceForFullIntensity = 2.0f; // ElasticityTestでの実測値を暫定継承
    }
}
