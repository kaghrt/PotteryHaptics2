using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// Identification課題用の、質感によらない単一の中立オブジェクトを駆動する。
    ///
    /// 【2026-09 設計変更】当初は「弾性の試行なら凹む」「粘性の試行なら伸びる」という、
    /// 質感の種類に応じて異なる種類の変形を割り当てていたが、これでは「凹む=弾性」
    /// 「伸びる=粘性」という対応が見た目からそのまま読み取れてしまい、識別課題の
    /// 目的(触覚だけで質感を判別できるかを見る)を損なっていた。
    ///
    /// そこで、質感の種類ではなく「指がどちらの方向にどれだけ動いているか」だけを見て
    /// 変形するように変更した。垂直方向の変位に応じた凹み・膨らみと、水平方向の変位に
    /// 応じた伸びを、それぞれ独立に計算してから1回だけ合成する。これにより、
    /// 「上下に凹んだ状態を保ったまま、水平にも伸びる」という自然な重なりが生まれ、
    /// かつ弾性・粘性どちらの試行でも全く同じルールで反応するため、見た目から
    /// 質感の種類が漏れることがない。
    ///
    /// 基準位置(restPositionCm)は、シーン中の適当な高さ・水平位置を1つ決めて使う。
    /// ElasticitySurface/ViscositySurfaceのcontactStartHeightCmやfixedHeightCmとは
    /// 独立した、見た目専用の基準値でよい(force等の物理パラメータには一切連動させないため)。
    /// </summary>
    public class NeutralInteractiveVisualDriver : MonoBehaviour
    {
        [SerializeField] private FingerTracker fingerTracker;
        [SerializeField] private Transform visualTransform;

        [Header("垂直方向の変形(凹み/膨らみ)")]
        [Tooltip("この高さ(cm)を基準に、上下どちらにずれても変形量として扱う")]
        [SerializeField] private float restHeightCm = 15f;
        [Tooltip("この変位(cm)で垂直方向の変形が最大になる")]
        [SerializeField] private float maxVerticalDeviationCm = 10f;
        [SerializeField] private float maxSquash = 0.5f;
        [SerializeField] private float squashSmoothTime = 0.05f;

        [Header("水平方向の変形(伸び・追従)")]
        [SerializeField] private float stretchScale = 0.3f;
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float returnSmoothTime = 0.6f;
        [Tooltip("このズレ量(ワールド距離)で伸びが最大になる")]
        [SerializeField] private float maxStretchOffset = 0.05f;
        [Tooltip("最大時に水平方向へ何倍まで伸ばすか")]
        [SerializeField] private float horizontalStretchFactor = 1.0f;
        [Tooltip("水平に伸びる分、垂直方向をどれだけ潰すか")]
        [SerializeField] private float verticalSquashOnStretchFactor = 0.4f;

        private Vector3 restLocalScale;
        private bool restCaptured;

        private float currentVerticalSquash;
        private float squashVelocity;
        private Vector3 currentPositionOffset;
        private Vector3 positionVelocity;

        private void Awake()
        {
            CaptureRestScaleIfNeeded();
        }

        private void CaptureRestScaleIfNeeded()
        {
            if (restCaptured || visualTransform == null)
            {
                return;
            }

            restLocalScale = visualTransform.localScale;
            restCaptured = true;
        }

        private void Update()
        {
            if (visualTransform == null || fingerTracker == null || !fingerTracker.IsTracking)
            {
                return;
            }

            CaptureRestScaleIfNeeded();

            UpdateVerticalSquash();
            UpdateHorizontalOffset();
            ApplyCombinedScale();
        }

        /// <summary>
        /// 指の高さが基準(restHeightCm)からどれだけ離れているかに応じて、
        /// 垂直方向の凹み量を更新する。弾性・粘性どちらの試行かは一切見ない。
        /// </summary>
        private void UpdateVerticalSquash()
        {
            float deviationCm = Mathf.Abs(fingerTracker.CurrentPositionCm.y - restHeightCm);
            float targetSquash = maxVerticalDeviationCm > 0f
                ? Mathf.Clamp01(deviationCm / maxVerticalDeviationCm) * maxSquash
                : 0f;

            currentVerticalSquash = Mathf.SmoothDamp(currentVerticalSquash, targetSquash, ref squashVelocity, squashSmoothTime);
        }

        /// <summary>
        /// 指の水平位置(X-Z)の変化に追従するオフセットを更新する。
        /// 弾性・粘性どちらの試行かは一切見ない。
        /// </summary>
        private void UpdateHorizontalOffset()
        {
            Vector3 fingerPositionCm = fingerTracker.CurrentPositionCm;
            Vector3 targetOffset = new Vector3(fingerPositionCm.x, 0f, fingerPositionCm.z) * (stretchScale * 0.01f);

            float horizontalSpeed = fingerTracker.HorizontalSpeedCmPerSec;
            float smoothTime = horizontalSpeed > 0.5f ? followSmoothTime : returnSmoothTime;

            currentPositionOffset = Vector3.SmoothDamp(currentPositionOffset, targetOffset, ref positionVelocity, smoothTime);
            visualTransform.localPosition = currentPositionOffset;
        }

        /// <summary>
        /// 垂直方向の凹み(currentVerticalSquash)と、水平方向の伸び(追従ズレ量)を
        /// 1回で合成してスケールに適用する。両方が同時に効いていても、
        /// 片方がもう片方をリセットすることはない。
        /// </summary>
        private void ApplyCombinedScale()
        {
            float squashY = 1f - currentVerticalSquash;
            float squashXZ = 1f + currentVerticalSquash * 0.5f;

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
