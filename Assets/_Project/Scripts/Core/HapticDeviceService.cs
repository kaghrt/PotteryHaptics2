using UnityEngine;
using UltrahapticsCoreAsset;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// UltrahapticsCoreAssetの実機接続窓口。SensationEmitter/SensationSourceを
    /// アプリ起動時に1組だけ生成し、シーンをまたいで保持する(DontDestroyOnLoad)。
    /// HapticOutputControllerはこれを経由して実際の触覚出力を行う。
    ///
    /// 【重要】SensationEmitterはシーン内に1つしか存在してはいけない
    /// (SensationEmitter.Start()内でシーン内の数をチェックし、複数あるとエラーログを出す)。
    /// このクラス自身がDontDestroyOnLoadで永続化されるため、各実験シーン側には
    /// SensationEmitterを別途配置しないこと。
    /// </summary>
    public class HapticDeviceService : MonoBehaviour
    {
        public static HapticDeviceService Instance { get; private set; }

        [Tooltip("実機が繋がっていない時にエラーで止めず、Mock(仮想)デバイスとして動かす。開発機での動作確認用。")]
        [SerializeField] private bool allowMockEmitter = true;

        [Tooltip("Assets/Python/配下に定義したSensation Blockの名前")]
        [SerializeField] private string sensationBlockName = "PotteryFocalPoint";

        private SensationEmitter emitter;
        private SensationSource source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            emitter = gameObject.AddComponent<SensationEmitter>();
            emitter.AllowMockEmitter = allowMockEmitter;

            source = gameObject.AddComponent<SensationSource>();
            source.SensationBlock = sensationBlockName;
        }

        /// <summary>
        /// 集束点の位置(エミッタ空間, メートル単位)と強さ(0〜1目安)を毎フレーム更新する。
        /// isInContact=falseの間はRunning=falseにしてミュートする(接触していない間は出力しない)。
        /// </summary>
        public void SetFocalPoint(Vector3 pointInEmitterSpace, float intensity01, bool isInContact)
        {
            if (source == null || source.Inputs == null) return;

            source.Inputs["point"].Value = pointInEmitterSpace;
            source.Inputs["intensity"].Value = new Vector3(Mathf.Clamp01(intensity01), 0f, 0f);
            source.Running = isInContact;
        }
    }
}
