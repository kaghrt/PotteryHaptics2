using UnityEngine;
using PotteryHaptics.Core;

namespace PotteryHaptics.Visual
{
    /// <summary>
    /// 粘性Surface上の指の水平位置に対し、追従にわずかな遅れを持たせることで
    /// 「まとわりつく」視覚効果を表現する。指を離した後もゆっくり静止位置へ戻る。
    /// </summary>
    public class MudStretchDriver : MonoBehaviour
    {
        [SerializeField] private ViscositySurface viscositySurface;
        [SerializeField] private Transform stretchTarget;
        [SerializeField] private Vector3 restLocalPosition = Vector3.zero;
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float returnSmoothTime = 0.6f;
        [SerializeField] private float stretchScale = 0.3f;

        private Vector3 velocity;

        private void Update()
        {
            if (viscositySurface == null || viscositySurface.Tracker == null || stretchTarget == null)
            {
                return;
            }

            bool isInContact = viscositySurface.IsInContact;
            Vector3 targetLocalPosition = restLocalPosition;

            if (isInContact)
            {
                Vector3 fingerPositionCm = viscositySurface.Tracker.CurrentPositionCm;
                Vector3 horizontalOffset = new Vector3(fingerPositionCm.x, 0f, fingerPositionCm.z) * (stretchScale * 0.01f);
                targetLocalPosition = restLocalPosition + horizontalOffset;
            }

            float smoothTime = isInContact ? followSmoothTime : returnSmoothTime;
            stretchTarget.localPosition = Vector3.SmoothDamp(stretchTarget.localPosition, targetLocalPosition, ref velocity, smoothTime);
        }
    }
}
