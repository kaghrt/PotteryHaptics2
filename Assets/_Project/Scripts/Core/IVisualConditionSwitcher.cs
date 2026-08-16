namespace PotteryHaptics.Core
{
    public enum VisualCondition
    {
        Minimal,
        Rich
    }

    /// <summary>
    /// Minimal/Rich映像条件の切り替えを行うコンポーネントが実装するインターフェース。
    /// 実装は _Project.Visual 層の VisualController。
    /// </summary>
    public interface IVisualConditionSwitcher
    {
        VisualCondition CurrentCondition { get; }
        void SetVisualCondition(VisualCondition condition);
    }
}
