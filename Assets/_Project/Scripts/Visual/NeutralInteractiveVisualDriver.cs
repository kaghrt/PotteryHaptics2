using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// Identification課題用の、質感によらない単一の中立オブジェクトを駆動する。
    /// ElasticitySurfaceがアクティブな間は押し込み量に応じてY軸方向に凹み(スケール圧縮)、
    /// ViscositySurfaceがアクティブな間はX-Z水平位置に応じて追従移動する。
    /// メッシュ・マテリアルは試行が弾性/粘性のどちらでも一切変更しない
    /// (ShowWhenSurfaceActiveのように別オブジェクトを出し分けると質感の答えが見た目から
    /// 漏れてしまうため、常に同一オブジェクト・同一マテリアルを保つのがこのクラスの制約)。
    /// 変形量はDeformationShaderDriver/MudStretchDriverと同じく、Surfaceの物理パラメータ
    /// (k値・b値)には一切連動させず、指の実際の移動量のみに連動させる。
    /// </summary>
    public class NeutralInteractiveVisualDriver : MonoBehaviour
    {
        [SerializeField] private ElasticitySurface elasticitySurface;
        [SerializeField] private ViscositySurface viscositySurface;
        [SerializeField] private Transform visualTransform;
        [SerializeField] private Vector3 restLocalScale = Vector3.one;
        [SerializeField] private Vector3 restLocalPosition = Vector3.zero;
        [SerializeField] private float maxSquash = 0.5f;
        [SerializeField] private float squashSmoothTime = 0.05f;
        [SerializeField] private float stretchScale = 0.3f;
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float returnSmoothTime = 0.6f;

        private float currentSquash;
        private float squashVelocity;
        private Vector3 currentPositionOffset;
        private Vector3 positionVelocity;

        private void Update()
        {
            if (visualTransform == null)
            {
                return;
            }

            UpdateSquash();
            UpdatePositionOffset();
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

            visualTransform.localScale = new Vector3(
                restLocalScale.x * (1f + currentSquash * 0.5f),
                restLocalScale.y * (1f - currentSquash),
                restLocalScale.z * (1f + currentSquash * 0.5f));
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
    }
}
