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
    ///
    /// 【接触開始時のアタック強調(2026-09)】
    /// 人間の触覚には順応(同じ刺激が続くと感じにくくなる)があり、持続的に一定の変調を
    /// 出し続けるだけでは弱く感じられることが実機検証で分かった。
    /// Ultraleap公式デモ(Fixed Button等)は接触の瞬間に強く感じられたことから、
    /// 接触開始の瞬間だけintensityを一時的に強調する「アタック」を追加した。
    /// </summary>
    public class HapticOutputController : MonoBehaviour
    {
        [SerializeField] private List<VirtualSurfaceBase> surfaces = new List<VirtualSurfaceBase>();
        public IReadOnlyList<VirtualSurfaceBase> Surfaces => surfaces;

        [Tooltip("集束点の位置を取得するFingerTracker。弾性用/粘性用Surfaceで共有しているものを1つ渡す。")]
        [SerializeField] private FingerTracker fingerTracker;

        [Tooltip("forceを超音波の0〜1強度へ正規化するための基準値(maxForceForFullIntensity)を持つ設定アセット。")]
        [SerializeField] private HapticCalibrationConfig calibrationConfig;

        [Header("接触開始時のアタック強調")]
        [Tooltip("接触開始からこの秒数の間、アタック強調を適用する")]
        [SerializeField] private float attackDurationSec = 0.2f;
        [Tooltip("アタック中、通常のintensityに掛ける倍率(1.0でクランプされるため実質的な上限は1.0)")]
        [SerializeField] private float attackMultiplier = 1.8f;

        [Header("強度調整の診断用(切り分けが済んだらfalseに戻すこと)")]
        [Tooltip("ONにすると、力の大きさに関わらず接触中は常にintensity=1.0(最大)で出力する。")]
        [SerializeField] private bool debugForceMaxIntensity = false;

        [Header("デバッグログ")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private float logIntervalSec = 0.5f;

        private float logTimer;
        private bool wasInContact;
        private float timeSinceContactStart;

        private void Update()
        {
            if (surfaces == null || surfaces.Count == 0) return;

            VirtualSurfaceBase active = FindActiveSurface();
            float force = active != null ? active.CurrentForce : 0f;
            bool isInContact = active != null && active.IsInContact;

            UpdateAttackTimer(isInContact);
            SendForceToDevice(force, isInContact);

            if (!logToConsole) return;

            logTimer += Time.deltaTime;
            if (logTimer < logIntervalSec) return;

            logTimer = 0f;
            Vector3 posCm = fingerTracker != null ? fingerTracker.CurrentPositionCm : Vector3.zero;
            float speed = fingerTracker != null ? fingerTracker.HorizontalSpeedCmPerSec : 0f;
            Debug.Log($"[HapticOutputController] Force={force:0.00}, Contact={isInContact}, HeightY={posCm.y:0.0}cm, SpeedXZ={speed:0.0}cm/s, DebugMaxIntensity={debugForceMaxIntensity}, AttackT={timeSinceContactStart:0.00}");
        }

        /// <summary>
        /// 接触が新たに始まった瞬間(false→true)を検知し、経過時間をリセットする。
        /// 接触が続いている間は経過時間を積算し、接触が切れたらリセットする。
        /// </summary>
        private void UpdateAttackTimer(bool isInContact)
        {
            if (isInContact && !wasInContact)
            {
                timeSinceContactStart = 0f;
            }
            else if (isInContact)
            {
                timeSinceContactStart += Time.deltaTime;
            }
            else
            {
                timeSinceContactStart = 0f;
            }

            wasInContact = isInContact;
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
        /// FingerTrackerのCurrentPositionCm(cm単位, Unity空間)をメートルに変換した上で、
        /// UltrahapticsCoreAsset.UnityToEmitterSpace.Transform(Y軸とZ軸を入れ替える公式の変換行列)
        /// を通してエミッタ空間の座標に変換する。
        /// 接触開始直後のattackDurationSec間は、intensityにattackMultiplierを掛けて強調する。
        /// </summary>
        private void SendForceToDevice(float force, bool isInContact)
        {
            if (HapticDeviceService.Instance == null || fingerTracker == null) return;

            float intensity01;
            if (debugForceMaxIntensity)
            {
                intensity01 = isInContact ? 1.0f : 0f;
            }
            else
            {
                float maxForce = calibrationConfig != null ? calibrationConfig.maxForceForFullIntensity : 2.0f;
                intensity01 = maxForce > 0f ? Mathf.Clamp01(force / maxForce) : 0f;
            }

            if (isInContact && timeSinceContactStart < attackDurationSec)
            {
                intensity01 = Mathf.Clamp01(intensity01 * attackMultiplier);
            }

            Vector3 fingerPositionM = fingerTracker.CurrentPositionCm * 0.01f;
            Vector3 emitterSpacePosition = UltrahapticsCoreAsset.UnityToEmitterSpace.Transform.MultiplyPoint3x4(fingerPositionM);

            HapticDeviceService.Instance.SetFocalPoint(emitterSpacePosition, intensity01, isInContact);
        }
    }
}
