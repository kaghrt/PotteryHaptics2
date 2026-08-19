using UnityEngine;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 実験セッション全体で共有される被験者情報を保持するsingleton。
    /// 映像条件はRich固定になったため、Minimal/Rich切り替え(旧FirstVisualCondition/
    /// GetVisualCondition/ConditionCounterbalancer)は廃止した。
    /// </summary>
    public class ExperimentManager : MonoBehaviour
    {
        public static ExperimentManager Instance { get; private set; }

        public string ParticipantId { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void InitializeSession(string participantId)
        {
            ParticipantId = participantId;
        }
    }
}
