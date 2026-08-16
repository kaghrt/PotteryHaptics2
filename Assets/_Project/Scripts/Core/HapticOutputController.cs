using System.Collections.Generic;
using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 登録されたSurfaceのうち接触中のものを毎フレーム探し、力を出力する。
    /// JND系シーンでは1要素、Identificationシーンでは弾性・粘性の2要素を保持する想定。
    /// 実機接続部分は SendForceToDevice の中身だけを差し替えれば完了するように保つ
    /// (現状はダミー実装としてログ出力のみ。実機接続実装は大学での残タスク)。
    /// </summary>
    public class HapticOutputController : MonoBehaviour
    {
        [SerializeField] private List<VirtualSurfaceBase> surfaces = new List<VirtualSurfaceBase>();

        public List<VirtualSurfaceBase> Surfaces => surfaces;

        private void Update()
        {
            VirtualSurfaceBase activeSurface = null;
            foreach (VirtualSurfaceBase surface in surfaces)
            {
                if (surface != null && surface.IsInContact)
                {
                    activeSurface = surface;
                    break;
                }
            }

            if (activeSurface != null)
            {
                SendForceToDevice(activeSurface.CurrentForce, true);
            }
            else
            {
                SendForceToDevice(0f, false);
            }
        }

        private void SendForceToDevice(float force, bool isInContact)
        {
            if (isInContact)
            {
                Debug.Log($"[HapticOutput] force={force:F3}");
            }
        }
    }
}
