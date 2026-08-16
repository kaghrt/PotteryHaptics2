using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 弾性・粘性の各Surfaceが継承する基底クラス。
    /// ActiveStimulusがnullの間は接触判定・力ともに強制的に0を返す。
    /// </summary>
    public abstract class VirtualSurfaceBase : MonoBehaviour
    {
        [SerializeField] protected FingerTracker fingerTracker;

        /// <summary>Visual層など他層から指位置・速度を読み取るための公開参照。</summary>
        public FingerTracker Tracker => fingerTracker;

        public StimulusDefinition ActiveStimulus { get; set; }

        public float CurrentForce { get; private set; }
        public bool IsInContact { get; private set; }

        protected virtual void Update()
        {
            if (ActiveStimulus == null || fingerTracker == null)
            {
                CurrentForce = 0f;
                IsInContact = false;
                return;
            }

            IsInContact = EvaluateContact();
            CurrentForce = IsInContact ? ComputeForce() : 0f;
        }

        /// <summary>現在の指の位置・速度から接触しているかを判定する。</summary>
        protected abstract bool EvaluateContact();

        /// <summary>接触中である前提で、その時点の反力を計算する。</summary>
        protected abstract float ComputeForce();
    }
}
