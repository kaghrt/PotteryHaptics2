using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// IFingerPositionProvider経由で指位置を取得し、位置・水平移動速度を毎フレーム更新する。
    /// 実機切り替え時は positionProviderBehaviour をLeap用Providerに差し替えるだけで済む。
    /// </summary>
    public class FingerTracker : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour positionProviderBehaviour;

        private IFingerPositionProvider provider;
        private Vector3 previousPositionCm;
        private bool hasPreviousPosition;

        public Vector3 CurrentPositionCm { get; private set; }
        public float HorizontalSpeedCmPerSec { get; private set; }
        public bool IsTracking { get; private set; }

        private void Awake()
        {
            provider = positionProviderBehaviour as IFingerPositionProvider;
        }

        private void Update()
        {
            if (provider == null)
            {
                IsTracking = false;
                HorizontalSpeedCmPerSec = 0f;
                return;
            }

            IsTracking = provider.TryGetFingerPositionCm(out Vector3 positionCm);
            if (!IsTracking)
            {
                hasPreviousPosition = false;
                HorizontalSpeedCmPerSec = 0f;
                return;
            }

            if (hasPreviousPosition && Time.deltaTime > 0f)
            {
                Vector3 previousHorizontal = new Vector3(previousPositionCm.x, 0f, previousPositionCm.z);
                Vector3 currentHorizontal = new Vector3(positionCm.x, 0f, positionCm.z);
                float distanceCm = Vector3.Distance(previousHorizontal, currentHorizontal);
                HorizontalSpeedCmPerSec = distanceCm / Time.deltaTime;
            }

            CurrentPositionCm = positionCm;
            previousPositionCm = positionCm;
            hasPreviousPosition = true;
        }

        public void SetPositionProvider(MonoBehaviour providerBehaviour)
        {
            positionProviderBehaviour = providerBehaviour;
            provider = providerBehaviour as IFingerPositionProvider;
            hasPreviousPosition = false;
        }
    }
}
