using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 実験セッション全体で共有される被験者情報・映像条件割り当てを保持するsingleton。
    /// </summary>
    public class ExperimentManager : MonoBehaviour
    {
        public static ExperimentManager Instance { get; private set; }

        public string ParticipantId { get; private set; }
        public VisualCondition FirstVisualCondition { get; private set; }

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
            FirstVisualCondition = ConditionCounterbalancer.DetermineFirstCondition(participantId);
        }

        /// <summary>前半/後半どちらの映像条件を使うかを返す。</summary>
        public VisualCondition GetVisualCondition(bool isSecondHalf)
        {
            if (!isSecondHalf)
            {
                return FirstVisualCondition;
            }

            return FirstVisualCondition == VisualCondition.Minimal ? VisualCondition.Rich : VisualCondition.Minimal;
        }
    }
}
