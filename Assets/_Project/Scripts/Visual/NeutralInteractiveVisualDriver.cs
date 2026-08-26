using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// Identification課題用の、質感によらない単一の中立オブジェクトを駆動する。
    /// ElasticitySurfaceがアクティブな間は押し込み量に応じてY軸方向に凹み(スケール圧縮)、
    /// ViscositySurfaceがアクティブな間はX-Z水平位置に追従しつつ、動いた方向に伸びる。
    /// メッシュ・マテリアルは試行が弾性/粘性のどちらでも一切変更しない。
    /// 変形量はSurfaceの物理パラメータ(k値・b値)には一切連動させず、指の実際の移動量のみに連動させる。
    ///
    /// 【重要】静止時の位置・スケールは、Awake時点のvisualTransformの実際の値を自動で
    /// 記録して使う(SceneBuilder等が設定した値をそのまま基準にするため、
    /// Inspectorで別途一致させる必要がない)。
    /// </summary>
    public class NeutralInteractiveVisualDriver : MonoBehaviour
    {
        [SerializeField] private ElasticitySurface elasticitySurface;
        [SerializeField] private ViscositySurface viscositySurface;
        [SerializeField] private Transform visualTransform;

        [Header("弾性: 押し込みによる凹み(スケール圧縮)")]
        [SerializeField] private float maxSquash = 0.5f;
        [SerializeField] private float squashSmoothTime = 0.05f;

        [Header("粘性: 水平方向への追従")]
        [SerializeField] private float stretchScale = 0.3f;
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float returnSmoothTime = 0.6f;

        [Header("粘性: 追従ズレ量に応じた伸び演出")]
        [Tooltip("このズレ量(ワールド距離)で伸びが最大(horizontalStretchFactor分)になる")]
        [SerializeField] private float maxStretchOffset = 0.05f;
        [Tooltip("最大時に水平方向へ何倍まで伸ばすか(0.4なら+40%)")]
        [SerializeField] private float horizontalStretchFactor = 1.0f;
        [Tooltip("水平に伸びる分、垂直方向をどれだけ潰すか(体積が保たれる印象にする)")]
        [SerializeField] private float verticalSquashOnStretchFactor = 0.4f;

        private Vector3 restLocalScale;
        private Vector3 restLocalPosition;
        private bool restCaptured;

        private float currentSquash;
        private float squashVelocity;
        private Vector3 currentPositionOffset;
        private Vector3 positionVelocity;

        private void Awake()
        {
            CaptureRestTransformIfNeeded();
        }

        /// <summary>
        /// visualTransformが現在持っている位置・スケールを「静止時の基準」として記録する。
        /// </summary>
        private void CaptureRestTransformIfNeeded()
        {
            if (restCaptured || visualTransform == null)
            {
                return;
            }

            restLocalScale = visualTransform.localScale;
            restLocalPosition = visualTransform.localPosition;
            restCaptured = true;
        }

        private void Update()
        {
            if (visualTransform == null)
            {
                return;
            }

            CaptureRestTransformIfNeeded();

            UpdateSquash();
            UpdatePositionOffset();
            ApplyCombinedScale();
        }

        private void UpdateSquash()
        {
            float targetSquash = 0f;
            if (elasticitySurface != null && elasticitySurface.Tracker != null && elasticitySurface.IsInContact)
            {
                float maxDepthCm = Mathf.Max(0f, elasticitySurface.ContactStartHeightCm - elasticitySurface.MaxPushHeightCm);
                float depthCm = Mathf.Clamp(elasticitySurface.ContactStartHeightCm - elasticitySurface.Tracker.CurrentPositionCm.y, 0f, maxDepthCm);
                targetSquash = maxDepthCm > 0f ? (depthCm / maxDepthCm) * maxSquash : 0f;
            }

            currentSquash = Mathf.SmoothDamp(currentSquash, targetSquash, ref squashVelocity, squashSmoothTime);
        }

        private void UpdatePositionOffset()
        {
            Vector3 targetOffset = Vector3.zero;
            float smoothTime = returnSmoothTime;

            if (viscositySurface != null && viscositySurface.Tracker != null && viscositySurface.IsInContact)
            {
                Vector3 fingerPositionCm = viscositySurface.Tracker.CurrentPositionCm;
                targetOffset = new Vector3(fingerPositionCm.x, 0f, fingerPositionCm.z) * (stretchScale * 0.01f);
                smoothTime = followSmoothTime;
            }

            currentPositionOffset = Vector3.SmoothDamp(currentPositionOffset, targetOffset, ref positionVelocity, smoothTime);
            visualTransform.localPosition = restLocalPosition + currentPositionOffset;
        }

        /// <summary>
        /// 弾性由来の凹み(currentSquash)と、粘性由来の伸び(追従ズレ量)を1回で合成してスケールに適用する。
        /// 弾性・粘性は別セッションのため通常は片方だけが非ゼロになるが、念のため両方を乗算合成しておく。
        /// </summary>
        private void ApplyCombinedScale()
        {
            float squashY = 1f - currentSquash;
            float squashXZ = 1f + currentSquash * 0.5f;

            float stretchAmount = maxStretchOffset > 0f
                ? Mathf.Clamp01(currentPositionOffset.magnitude / maxStretchOffset)
                : 0f;
            float stretchXZ = 1f + stretchAmount * horizontalStretchFactor;
            float stretchY = 1f - stretchAmount * verticalSquashOnStretchFactor;

            visualTransform.localScale = new Vector3(
                restLocalScale.x * squashXZ * stretchXZ,
                restLocalScale.y * squashY * stretchY,
                restLocalScale.z * squashXZ * stretchXZ);
        }
    }
}
