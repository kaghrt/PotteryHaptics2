using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// 粘性Surface上の指の水平位置に対し、追従にわずかな遅れを持たせることで
    /// 「まとわりつく」視覚効果を表現する。指を離した後もゆっくり静止位置へ戻る。
    /// 追従のズレ量に応じて、動いた方向に伸びる(引っ張られる)演出を加える。
    ///
    /// 【重要】静止時の位置・スケールは、Awake時点のstretchTargetの実際の値を自動で
    /// 記録して使う(SceneBuilder等が設定した値をそのまま基準にするため、
    /// Inspectorで別途一致させる必要がない)。
    /// </summary>
    public class MudStretchDriver : MonoBehaviour
    {
        [SerializeField] private ViscositySurface viscositySurface;
        [SerializeField] private Transform stretchTarget;
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float returnSmoothTime = 0.6f;
        [SerializeField] private float stretchScale = 0.3f;

        [Header("追従ズレ量に応じた伸び演出")]
        [Tooltip("このズレ量(ワールド距離)で伸びが最大(horizontalStretchFactor分)になる")]
        [SerializeField] private float maxStretchOffset = 0.05f;
        [Tooltip("最大時に水平方向へ何倍まで伸ばすか(0.4なら+40%)")]
        [SerializeField] private float horizontalStretchFactor = 1.0f;
        [Tooltip("水平に伸びる分、垂直方向をどれだけ潰すか(体積が保たれる印象にする)")]
        [SerializeField] private float verticalSquashOnStretchFactor = 0.4f;

        private Vector3 restLocalPosition;
        private Vector3 restLocalScale;
        private bool restCaptured;
        private Vector3 velocity;

        private void Awake()
        {
            CaptureRestTransformIfNeeded();
        }

        /// <summary>
        /// stretchTargetが現在持っている位置・スケールを「静止時の基準」として記録する。
        /// これにより、SceneBuilder側でスケール・位置がどう設定されていても、
        /// このスクリプトが上書きして壊すことがなくなる。
        /// </summary>
        private void CaptureRestTransformIfNeeded()
        {
            if (restCaptured || stretchTarget == null)
            {
                return;
            }

            restLocalPosition = stretchTarget.localPosition;
            restLocalScale = stretchTarget.localScale;
            restCaptured = true;
        }

        private void Update()
        {
            if (viscositySurface == null || viscositySurface.Tracker == null || stretchTarget == null)
            {
                return;
            }

            CaptureRestTransformIfNeeded();

            bool isInContact = viscositySurface.IsInContact;
            Vector3 targetLocalPosition = restLocalPosition;

            if (isInContact)
            {
                Vector3 fingerPositionCm = viscositySurface.Tracker.CurrentPositionCm;
                Vector3 horizontalOffset = new Vector3(fingerPositionCm.x, 0f, fingerPositionCm.z) * (stretchScale * 0.01f);
                targetLocalPosition = restLocalPosition + horizontalOffset;
            }

            float smoothTime = isInContact ? followSmoothTime : returnSmoothTime;
            stretchTarget.localPosition = Vector3.SmoothDamp(stretchTarget.localPosition, targetLocalPosition, ref velocity, smoothTime);

            ApplyStretchScale();
        }

        /// <summary>
        /// 静止位置からの現在のズレ量に応じて、水平方向に伸ばし垂直方向をやや潰す。
        /// ズレが無い(静止時)は記録済みのrestLocalScaleのまま。
        /// </summary>
        private void ApplyStretchScale()
        {
            float currentOffsetMagnitude = (stretchTarget.localPosition - restLocalPosition).magnitude;
            float stretchAmount = maxStretchOffset > 0f
                ? Mathf.Clamp01(currentOffsetMagnitude / maxStretchOffset)
                : 0f;

            float stretchXZ = 1f + stretchAmount * horizontalStretchFactor;
            float stretchY = 1f - stretchAmount * verticalSquashOnStretchFactor;

            stretchTarget.localScale = new Vector3(
                restLocalScale.x * stretchXZ,
                restLocalScale.y * stretchY,
                restLocalScale.z * stretchXZ);
        }
    }
}
