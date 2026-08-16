using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// 弾性Surfaceの押し込み量に応じてClayDeformationシェーダーの凹み表現パラメータを駆動する。
    /// deformationToDistanceRatioは変形量と手の移動距離の比率(deformation-to-distance ratio)の
    /// 操作変数で、疑似触覚(pseudo-haptics)検証における知覚硬さの操作に用いる。
    /// </summary>
    public class DeformationShaderDriver : MonoBehaviour
    {
        private static readonly int DeformationStrengthId = Shader.PropertyToID("_DeformationStrength");

        [SerializeField] private ElasticitySurface elasticitySurface;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private float deformationToDistanceRatio = 1f;
        [SerializeField] private float smoothTime = 0.05f;

        private MaterialPropertyBlock propertyBlock;
        private float currentDeformation;
        private float deformationVelocity;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (elasticitySurface == null || elasticitySurface.Tracker == null || targetRenderer == null)
            {
                return;
            }

            float maxDepthCm = Mathf.Max(0f, elasticitySurface.ContactStartHeightCm - elasticitySurface.MaxPushHeightCm);
            float depthCm = elasticitySurface.IsInContact
                ? Mathf.Clamp(elasticitySurface.ContactStartHeightCm - elasticitySurface.Tracker.CurrentPositionCm.y, 0f, maxDepthCm)
                : 0f;

            float targetDeformation = maxDepthCm > 0f
                ? (depthCm / maxDepthCm) * deformationToDistanceRatio
                : 0f;

            currentDeformation = Mathf.SmoothDamp(currentDeformation, targetDeformation, ref deformationVelocity, smoothTime);

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(DeformationStrengthId, currentDeformation);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
