using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// 対応するVirtualSurfaceBase.ActiveStimulusがnullでない間だけ自身のRendererを表示する。
    /// Identificationシーンで弾性用/粘性用の視覚オブジェクトを、今回の試行がどちらの質感かに
    /// 応じて自動的に出し分けるために使う。
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ShowWhenSurfaceActive : MonoBehaviour
    {
        [SerializeField] private VirtualSurfaceBase surface;

        private Renderer targetRenderer;

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (surface == null || targetRenderer == null)
            {
                return;
            }

            targetRenderer.enabled = surface.ActiveStimulus != null;
        }
    }
}
