using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// シーン順序(Launcher → JND_Elasticity → JND_Viscosity → Identification)を進行させるsingleton。
    /// シーン名はBuild Settingsのシーンリストと完全一致させること。
    /// </summary>
    public class SceneFlowController : MonoBehaviour
    {
        public static SceneFlowController Instance { get; private set; }

        private static readonly string[] SceneOrder =
        {
            "Launcher",
            "JND_Elasticity",
            "JND_Viscosity",
            "Identification"
        };

        private int currentSceneOrderIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentSceneOrderIndex = Array.IndexOf(SceneOrder, SceneManager.GetActiveScene().name);
        }

        public void NextScene()
        {
            currentSceneOrderIndex++;
            if (currentSceneOrderIndex < 0 || currentSceneOrderIndex >= SceneOrder.Length)
            {
                Debug.LogWarning("[SceneFlowController] シーン順序の末尾に到達しました。");
                return;
            }

            SceneManager.LoadScene(SceneOrder[currentSceneOrderIndex]);
        }
    }
}
