using UnityEngine;
using UnityEngine.UI;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// 被験者ID入力欄 + Startボタン。押すとセッションを初期化し、シーン進行を開始する。
    /// </summary>
    public class LauncherController : MonoBehaviour
    {
        [SerializeField] private InputField participantIdInputField;
        [SerializeField] private Button startButton;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStart);
            }
        }

        private void HandleStart()
        {
            string participantId = participantIdInputField != null ? participantIdInputField.text : string.Empty;
            if (string.IsNullOrWhiteSpace(participantId))
            {
                Debug.LogWarning("[LauncherController] 被験者IDが未入力です。");
                return;
            }

            if (ExperimentManager.Instance != null)
            {
                ExperimentManager.Instance.InitializeSession(participantId);
            }

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.NextScene();
            }
        }
    }
}
