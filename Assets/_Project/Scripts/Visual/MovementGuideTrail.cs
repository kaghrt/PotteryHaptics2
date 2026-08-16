using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// 指の軌跡をTrailRendererで描画し、質感ごとに期待される動作軸
    /// (弾性=Y軸押し込み、粘性=X-Z平面のsweep動作)を視覚的にガイドする。
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class MovementGuideTrail : MonoBehaviour
    {
        [SerializeField] private VirtualSurfaceBase surface;
        [SerializeField] private float positionScaleCmToUnit = 0.01f;

        private TrailRenderer trail;

        private void Awake()
        {
            trail = GetComponent<TrailRenderer>();
        }

        private void Update()
        {
            if (surface == null || surface.Tracker == null || !surface.Tracker.IsTracking)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                }
                return;
            }

            trail.emitting = surface.IsInContact;
            transform.localPosition = surface.Tracker.CurrentPositionCm * positionScaleCmToUnit;
        }
    }
}
