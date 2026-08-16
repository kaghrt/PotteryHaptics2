using System;
using UnityEngine;
using UnityEngine.UI;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 2肢強制選択(2AFC)の回答UI。ボタン2つと反応時間計測を提供する。
    /// TrialSequencerがOnResponseを購読して次の処理に進む。
    /// </summary>
    public class ResponseUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button optionAButton;
        [SerializeField] private Button optionBButton;
        [SerializeField] private Text optionALabel;
        [SerializeField] private Text optionBLabel;

        public event Action<string, float> OnResponse;

        private string responseValueA;
        private string responseValueB;
        private float promptStartTime;
        private bool waitingForResponse;

        private void Awake()
        {
            if (optionAButton != null)
            {
                optionAButton.onClick.AddListener(() => HandleResponse(responseValueA));
            }

            if (optionBButton != null)
            {
                optionBButton.onClick.AddListener(() => HandleResponse(responseValueB));
            }

            SetVisible(false);
        }

        public void PromptResponse(string valueA, string displayLabelA, string valueB, string displayLabelB)
        {
            responseValueA = valueA;
            responseValueB = valueB;

            if (optionALabel != null) optionALabel.text = displayLabelA;
            if (optionBLabel != null) optionBLabel.text = displayLabelB;

            promptStartTime = Time.time;
            waitingForResponse = true;
            SetVisible(true);
        }

        private void HandleResponse(string responseValue)
        {
            if (!waitingForResponse)
            {
                return;
            }

            waitingForResponse = false;
            float reactionTimeSec = Time.time - promptStartTime;
            SetVisible(false);
            OnResponse?.Invoke(responseValue, reactionTimeSec);
        }

        private void SetVisible(bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }
        }
    }
}
