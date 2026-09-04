using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// IFingerPositionProvider経由で指位置を取得し、位置・水平移動速度を毎フレーム更新する。
    /// 実機切り替え時は providerGameObject をLeap用Providerが乗ったオブジェクトに
    /// 差し替えるだけで済む。
    ///
    /// 【重要】以前は positionProviderBehaviour(MonoBehaviour型)で直接コンポーネントを
    /// 参照する設計だったが、同じGameObjectに複数のMonoBehaviourが乗っている場合
    /// （例: LeapServiceProviderとLeapMotionFingerInputSourceが同居するケース）、
    /// UnityのInspector上でオブジェクトをドラッグすると意図しない方のコンポーネントが
    /// 選ばれてしまい、正しい方を指定できないというUnity Editor側の既知の挙動があった。
    /// これを回避するため、GameObject単位で参照を持ち、内部でGetComponentする方式に変更した。
    /// これによりInspector上ではGameObjectをドラッグするだけでよく、
    /// コンポーネントの選択が曖昧になることがない。
    /// </summary>
    public class FingerTracker : MonoBehaviour
    {
        [Tooltip("IFingerPositionProviderを実装したコンポーネントが乗っているGameObject。" +
                 "開発時はDummyKeyboardFingerInputSourceが乗ったオブジェクト、" +
                 "実機ではLeapMotionFingerInputSourceが乗ったオブジェクトを指定する。")]
        [SerializeField] private GameObject providerGameObject;

        private IFingerPositionProvider provider;
        private Vector3 previousPositionCm;
        private bool hasPreviousPosition;

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

        /// <summary>
        /// 実行時にプロバイダーを差し替える場合に使う。
        /// </summary>
        public void SetPositionProvider(GameObject newProviderGameObject)
        {
            providerGameObject = newProviderGameObject;
            hasPreviousPosition = false;
            ResolveProvider();
        }
    }
}
