using PotteryHaptics.Core;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 被験者IDの末尾の奇偶で映像条件(Minimal/Rich)の提示順序をカウンターバランスする。
    /// </summary>
    public static class ConditionCounterbalancer
    {
        public static VisualCondition DetermineFirstCondition(string participantId)
        {
            int lastDigit = ExtractLastDigit(participantId);
            return lastDigit % 2 == 0 ? VisualCondition.Minimal : VisualCondition.Rich;
        }

        private static int ExtractLastDigit(string participantId)
        {
            if (string.IsNullOrEmpty(participantId))
            {
                return 0;
            }

            for (int i = participantId.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(participantId[i]))
                {
                    return participantId[i] - '0';
                }
            }

            return 0;
        }
    }
}
