using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// シーン順序(Launcher → JND_Viscosity → JND_Elasticity → Identification)を進行させるsingleton。
    /// シーン名はBuild Settingsのシーンリストと完全一致させること。
    ///
    /// 【2026-09 変更】実際の実験では弾性→粘性ではなく、粘性→弾性の順で実施することになったため、
    /// SceneOrderの並びをJND_Viscosity → JND_Elasticityに変更した。
    /// </summary>
    public class SceneFlowController : MonoBehaviour
    {
        public static SceneFlowController Instance { get; private set; }

        private static readonly string[] SceneOrder =
        {
            "Launcher",
            "JND_Viscosity",
            "JND_Elasticity",
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
