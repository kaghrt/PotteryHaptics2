using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// Minimal/Rich映像条件をminimalVisual/richVisualのSetActive切り替えで実現する。
    /// Minimal条件は識別課題の統制条件として質感情報を含めないため変更しない。
    /// </summary>
    public class VisualController : MonoBehaviour, IVisualConditionSwitcher
    {
        [SerializeField] private GameObject minimalVisual;
        [SerializeField] private GameObject richVisual;
        [SerializeField] private VisualCondition initialCondition = VisualCondition.Minimal;

        public VisualCondition CurrentCondition { get; private set; }

        private void Awake()
        {
            SetVisualCondition(initialCondition);
        }

        public void SetVisualCondition(VisualCondition condition)
        {
            CurrentCondition = condition;

            if (minimalVisual != null)
            {
                minimalVisual.SetActive(condition == VisualCondition.Minimal);
            }

            if (richVisual != null)
            {
                richVisual.SetActive(condition == VisualCondition.Rich);
            }
        }
    }
}
