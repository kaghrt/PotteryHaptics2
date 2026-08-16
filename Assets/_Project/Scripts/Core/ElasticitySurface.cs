using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 弾性(F = k * depth)。Y軸(高さ)方向の押し込みで判定する。
    /// </summary>
    public class ElasticitySurface : VirtualSurfaceBase
    {
        [SerializeField] private float contactStartHeightCm = 25f;
        [SerializeField] private float maxPushHeightCm = 5f;

        public float ContactStartHeightCm
        {
            get => contactStartHeightCm;
            set => contactStartHeightCm = value;
        }

        public float MaxPushHeightCm
        {
            get => maxPushHeightCm;
            set => maxPushHeightCm = value;
        }

        protected override bool EvaluateContact()
        {
            return fingerTracker.IsTracking && fingerTracker.CurrentPositionCm.y <= contactStartHeightCm;
        }

        protected override float ComputeForce()
        {
            float maxDepthCm = Mathf.Max(0f, contactStartHeightCm - maxPushHeightCm);
            float depthCm = Mathf.Clamp(contactStartHeightCm - fingerTracker.CurrentPositionCm.y, 0f, maxDepthCm);
            return ActiveStimulus.PhysicalValue * depthCm;
        }
    }
}
