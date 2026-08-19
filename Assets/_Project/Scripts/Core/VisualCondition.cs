namespace PotteryHaptics.Core
{
    /// <summary>
    /// CSVログのvisualCondition列との後方互換のために列挙型自体は残すが、
    /// Minimal/Richの2条件を切り替える仕組み(旧IVisualConditionSwitcher/VisualController/
    /// ConditionCounterbalancer)は廃止済み。現在は常にRichのみを記録する。
    /// </summary>
    public enum VisualCondition
    {
        Minimal,
        Rich
    }
}
