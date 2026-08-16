using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 粘性(F = b * speed)。X-Z平面の水平移動速度で判定する。
    /// 指定の高さ帯(fixedHeightCm ± heightToleranceCm)にいる間のみ有効。
    /// </summary>
    public class ViscositySurface : VirtualSurfaceBase
    {
        [SerializeField] private float fixedHeightCm = 15f;
        [SerializeField] private float heightToleranceCm = 5f;
        [SerializeField] private float minSpeedCmPerSec = 0.5f;

        public float FixedHeightCm
        {
            get => fixedHeightCm;
            set => fixedHeightCm = value;
        }

        public float HeightToleranceCm
        {
            get => heightToleranceCm;
            set => heightToleranceCm = value;
        }

        public float MinSpeedCmPerSec
        {
            get => minSpeedCmPerSec;
            set => minSpeedCmPerSec = value;
        }

        protected override bool EvaluateContact()
        {
            if (!fingerTracker.IsTracking)
            {
                return false;
            }

            float heightDiffCm = Mathf.Abs(fingerTracker.CurrentPositionCm.y - fixedHeightCm);
            return heightDiffCm <= heightToleranceCm && fingerTracker.HorizontalSpeedCmPerSec >= minSpeedCmPerSec;
        }

        protected override float ComputeForce()
        {
            return ActiveStimulus.PhysicalValue * fingerTracker.HorizontalSpeedCmPerSec;
        }
    }
}
