using UnityEngine;

namespace PotteryHaptics.Core
{
    /// <summary>
    /// 実機不要の開発用入力。W/Sキーで高さ、矢印キーで水平移動をシミュレートする。
    /// 大学での実機切り替え時は、FingerTracker.positionProviderBehaviour を
    /// Leap用Providerに差し替えるだけで済む(このクラスは削除せず開発機用に残す)。
    /// </summary>
    public class DummyKeyboardFingerInputSource : MonoBehaviour, IFingerPositionProvider
    {
        [SerializeField] private float heightSpeedCmPerSec = 10f;
        [SerializeField] private float horizontalSpeedCmPerSec = 10f;
        [SerializeField] private float initialHeightCm = 25f;
        [SerializeField] private float minHeightCm = 0f;
        [SerializeField] private float maxHeightCm = 30f;
        [SerializeField] private float horizontalRangeCm = 15f;

        private Vector3 simulatedPositionCm;

        private void Awake()
        {
            simulatedPositionCm = new Vector3(0f, initialHeightCm, 0f);
        }

        private void Update()
        {
            float verticalInput = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.W)) verticalInput += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) verticalInput -= 1f;

            float horizontalX = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) horizontalX += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) horizontalX -= 1f;

            float horizontalZ = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) horizontalZ += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) horizontalZ -= 1f;

            simulatedPositionCm.y = Mathf.Clamp(
                simulatedPositionCm.y + verticalInput * heightSpeedCmPerSec * Time.deltaTime,
                minHeightCm, maxHeightCm);

            simulatedPositionCm.x = Mathf.Clamp(
                simulatedPositionCm.x + horizontalX * horizontalSpeedCmPerSec * Time.deltaTime,
                -horizontalRangeCm, horizontalRangeCm);

            simulatedPositionCm.z = Mathf.Clamp(
                simulatedPositionCm.z + horizontalZ * horizontalSpeedCmPerSec * Time.deltaTime,
                -horizontalRangeCm, horizontalRangeCm);
        }

        public bool TryGetFingerPositionCm(out Vector3 positionCm)
        {
            positionCm = simulatedPositionCm;
            return true;
        }
    }
}
