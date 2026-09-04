using Leap;
using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// Ultraleap Trackingパッケージ(LeapServiceProvider)から、手の位置を取得する
    /// IFingerPositionProvider実装。DummyKeyboardFingerInputSourceの実機版として、
    /// FingerTrackerのpositionProviderBehaviourに差し替えて使う。
    ///
    /// 【設計変更】当初は人差し指の指先(Hand.Index.TipPosition)を追従していたが、
    /// 指先1点だけでは超音波の集束点が狭すぎて感じにくく、また被験者が実際に動かす
    /// 意識の中心は手のひらであるため、手のひらの中心(Hand.PalmPosition)を
    /// 追従するように変更した。
    ///
    /// 【重要な前提】LeapServiceProviderが返す座標は既にUnity座標系(メートル単位)。
    /// HDK-REC192は超音波アレイ面を上向きに設置し、その上に手をかざす構成のため、
    /// Y軸=高さ（デバイス上面からの距離）という対応が、ElasticitySurface/ViscositySurfaceの
    /// 前提(FingerTracker.CurrentPositionCm.y = 高さ)とそのまま一致する想定。
    /// もし実機で軸の向きが逆・ズレている場合は、LeapServiceProviderのTransform
    /// (position/rotation)側で調整するか、このクラスの座標変換部分に補正を追加すること。
    /// </summary>
    public class LeapMotionFingerInputSource : MonoBehaviour, IFingerPositionProvider
    {
        [Tooltip("シーン内のLeapServiceProviderへの参照")]
        [SerializeField] private LeapServiceProvider leapServiceProvider;

        [Tooltip("trueなら右手を優先して追跡する。優先した手が検出されない場合は、" +
                 "検出されている最初の手にフォールバックする。")]
        [SerializeField] private bool preferRightHand = true;

        public bool TryGetFingerPositionCm(out Vector3 positionCm)
        {
            positionCm = Vector3.zero;

            if (leapServiceProvider == null)
            {
                return false;
            }

            Frame frame = leapServiceProvider.CurrentFrame;
            if (frame == null || frame.Hands == null || frame.Hands.Count == 0)
            {
                return false;
            }

            Hand hand = FindPreferredHand(frame);
            if (hand == null)
            {
                return false;
            }

            // LeapのUnity APIはメートル単位のUnity座標を返すため、cmに変換する。
            // 指先ではなく手のひらの中心を追従する。
            positionCm = hand.PalmPosition * 100f;
            return true;
        }

        private Hand FindPreferredHand(Frame frame)
        {
            Hand fallback = null;

            foreach (Hand hand in frame.Hands)
            {
                if (fallback == null)
                {
                    fallback = hand;
                }

                bool isPreferredSide = preferRightHand ? hand.IsRight : hand.IsLeft;
                if (isPreferredSide)
                {
                    return hand;
                }
            }

            // 優先する手が見つからなければ、検出されている最初の手を使う
            return fallback;
        }
    }
}
