using System.Collections.Generic;
using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 触覚出力の窓口。HapticDeviceService経由でUltrahaptics実機に集束点(位置・強さ)を送る。
    ///
    /// 【複数surface対応について】
    /// Identificationシーンでは弾性/粘性の2つのSurfaceが同居し、TrialSequencerIdentificationが
    /// 試行ごとにどちらか一方にのみActiveStimulusを割り当てる(もう一方はnullのまま=Force常に0)。
    /// そのため、ここでは複数surfaceを受け取り、現在接触中(IsInContact)のものを毎フレーム探して出力する。
    /// JND系シーンではsurfaceを1つだけ渡せばよい。
    /// </summary>
    public class HapticOutputController : MonoBehaviour
    {
        [SerializeField] private List<VirtualSurfaceBase> surfaces = new List<VirtualSurfaceBase>();
        public IReadOnlyList<VirtualSurfaceBase> Surfaces => surfaces;

        [Tooltip("集束点の位置を取得するFingerTracker。弾性用/粘性用Surfaceで共有しているものを1つ渡す。")]
        [SerializeField] private FingerTracker fingerTracker;

        [Tooltip("forceを超音波の0〜1強度へ正規化するための基準値(maxForceForFullIntensity)を持つ設定アセット。")]
        [SerializeField] private HapticCalibrationConfig calibrationConfig;

        [Header("デバッグログ")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private float logIntervalSec = 0.5f;

        private float logTimer;

        private void Update()
        {
            if (surfaces == null || surfaces.Count == 0) return;

            VirtualSurfaceBase active = FindActiveSurface();
            float force = active != null ? active.CurrentForce : 0f;
            bool isInContact = active != null && active.IsInContact;

            SendForceToDevice(force, isInContact);

            if (!logToConsole) return;

            logTimer += Time.deltaTime;
            if (logTimer < logIntervalSec) return;

            logTimer = 0f;
            Vector3 posCm = fingerTracker != null ? fingerTracker.CurrentPositionCm : Vector3.zero;
            float speed = fingerTracker != null ? fingerTracker.HorizontalSpeedCmPerSec : 0f;
            Debug.Log($"[HapticOutputController] Force={force:0.00}, Contact={isInContact}, HeightY={posCm.y:0.0}cm, SpeedXZ={speed:0.0}cm/s");
        }

        /// <summary>
        /// 現在接触中のsurfaceを1つ返す。複数同時接触は想定していないため、
        /// 最初に見つかったものを返す(通常は高々1つしかIsInContact=trueにならない設計)。
        /// </summary>
        private VirtualSurfaceBase FindActiveSurface()
        {
            for (int i = 0; i < surfaces.Count; i++)
            {
                if (surfaces[i] != null && surfaces[i].IsInContact)
                    return surfaces[i];
            }
            return null;
        }

        /// <summary>
        /// HapticDeviceService経由でUltrahaptics実機(またはMockデバイス)に出力する。
        /// FingerTrackerのCurrentPositionCm(cm単位, Unity空間)を、エミッタ空間(メートル単位)に変換して渡す。
        ///
        /// 【注意】cm→mの単位変換のみ行っており、Unity空間の原点・軸とエミッタ空間の原点・軸が
        /// 一致している前提になっている。実機での座標ズレが確認された場合は、
        /// HDK-REC192の設置位置に応じたオフセット/回転補正をここに追加すること。
        /// </summary>
        private void SendForceToDevice(float force, bool isInContact)
        {
            if (HapticDeviceService.Instance == null || fingerTracker == null) return;

            float maxForce = calibrationConfig != null ? calibrationConfig.maxForceForFullIntensity : 2.0f;
            float intensity01 = maxForce > 0f ? Mathf.Clamp01(force / maxForce) : 0f;

            Vector3 fingerPositionM = fingerTracker.CurrentPositionCm * 0.01f;
            HapticDeviceService.Instance.SetFocalPoint(fingerPositionM, intensity01, isInContact);
        }
    }
}
