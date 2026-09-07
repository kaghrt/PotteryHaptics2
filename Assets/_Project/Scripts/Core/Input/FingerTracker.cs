using System.Collections.Generic;
using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// IFingerPositionProvider経由で指位置を取得し、位置・水平移動速度を毎フレーム更新する。
    /// 実機切り替え時は providerGameObject をLeap用Providerが乗ったオブジェクトに
    /// 差し替えるだけで済む。
    ///
    /// 【2026-09 速度の平滑化を追加】
    /// 当初、HorizontalSpeedCmPerSecは「直前フレームとの瞬間速度」をそのまま使っていたが、
    /// これだと手のわずかな震えがそのまま速度の激しい変動として現れ、粘性(F = b * speed)側の
    /// 弁別がしづらいという問題があった。直近smoothingWindowSec秒間の移動平均速度に変更し、
    /// 瞬間的なブレの影響を抑えつつ、被験者が意図した速さがより安定して反映されるようにした。
    /// </summary>
    public class FingerTracker : MonoBehaviour
    {
        [Tooltip("IFingerPositionProviderを実装したコンポーネントが乗っているGameObject。" +
                 "開発時はDummyKeyboardFingerInputSourceが乗ったオブジェクト、" +
                 "実機ではLeapMotionFingerInputSourceが乗ったオブジェクトを指定する。")]
        [SerializeField] private GameObject providerGameObject;

        [Tooltip("水平移動速度を平滑化する際の移動平均ウィンドウ幅(秒)。" +
                 "大きくするほど滑らかになるが、反応が遅れる。")]
        [SerializeField] private float smoothingWindowSec = 0.25f;

        private IFingerPositionProvider provider;
        private Vector3 previousPositionCm;
        private bool hasPreviousPosition;

        // 平滑化用: 直近の(距離, 経過時間)のペアを保持するリングバッファ的なリスト
        private readonly List<(float distanceCm, float deltaTime)> recentSamples = new List<(float, float)>();

        public Vector3 CurrentPositionCm { get; private set; }
        public float HorizontalSpeedCmPerSec { get; private set; }
        public bool IsTracking { get; private set; }

        private void Awake()
        {
            ResolveProvider();
        }

        /// <summary>
        /// providerGameObjectからIFingerPositionProviderを実装したコンポーネントを取得する。
        /// 同じGameObjectに複数のIFingerPositionProvider実装が乗っていることは想定していない。
        /// </summary>
        private void ResolveProvider()
        {
            if (providerGameObject == null)
            {
                provider = null;
                return;
            }

            provider = providerGameObject.GetComponent<IFingerPositionProvider>();

            if (provider == null)
            {
                Debug.LogError($"[FingerTracker] providerGameObject『{providerGameObject.name}』に" +
                                "IFingerPositionProviderを実装したコンポーネントが見つかりません。");
            }
        }

        private void Update()
        {
            if (provider == null)
            {
                IsTracking = false;
                HorizontalSpeedCmPerSec = 0f;
                recentSamples.Clear();
                return;
            }

            IsTracking = provider.TryGetFingerPositionCm(out Vector3 positionCm);
            if (!IsTracking)
            {
                hasPreviousPosition = false;
                HorizontalSpeedCmPerSec = 0f;
                recentSamples.Clear();
                return;
            }

            if (hasPreviousPosition && Time.deltaTime > 0f)
            {
                Vector3 previousHorizontal = new Vector3(previousPositionCm.x, 0f, previousPositionCm.z);
                Vector3 currentHorizontal = new Vector3(positionCm.x, 0f, positionCm.z);
                float distanceCm = Vector3.Distance(previousHorizontal, currentHorizontal);

                recentSamples.Add((distanceCm, Time.deltaTime));
                TrimOldSamples();

                HorizontalSpeedCmPerSec = ComputeAverageSpeed();
            }

            CurrentPositionCm = positionCm;
            previousPositionCm = positionCm;
            hasPreviousPosition = true;
        }

        /// <summary>
        /// smoothingWindowSecより古いサンプルをリストから取り除く。
        /// </summary>
        private void TrimOldSamples()
        {
            float totalTime = 0f;
            foreach (var sample in recentSamples)
            {
                totalTime += sample.deltaTime;
            }

            while (totalTime > smoothingWindowSec && recentSamples.Count > 1)
            {
                totalTime -= recentSamples[0].deltaTime;
                recentSamples.RemoveAt(0);
            }
        }

        /// <summary>
        /// 直近のサンプルから、移動平均速度(cm/秒)を計算する。
        /// 合計距離 ÷ 合計時間、という単純な移動平均。
        /// </summary>
        private float ComputeAverageSpeed()
        {
            float totalDistance = 0f;
            float totalTime = 0f;

            foreach (var sample in recentSamples)
            {
                totalDistance += sample.distanceCm;
                totalTime += sample.deltaTime;
            }

            return totalTime > 0f ? totalDistance / totalTime : 0f;
        }

        /// <summary>
        /// 実行時にプロバイダーを差し替える場合に使う。
        /// </summary>
        public void SetPositionProvider(GameObject newProviderGameObject)
        {
            providerGameObject = newProviderGameObject;
            hasPreviousPosition = false;
            recentSamples.Clear();
            ResolveProvider();
        }
    }
}
