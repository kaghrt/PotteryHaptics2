using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 指位置取得の抽象化。実機ではLeap Motion由来のProviderを実装して差し替える。
    /// 開発時はDummyKeyboardFingerInputSourceを使用する。
    /// </summary>
    public interface IFingerPositionProvider
    {
        bool TryGetFingerPositionCm(out Vector3 positionCm);
    }
}
