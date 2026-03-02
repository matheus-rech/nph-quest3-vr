using UnityEngine;
using UnityEngine.InputSystem;

namespace Quest3VR.NPH
{
    public class SliceScrollController : MonoBehaviour
    {
        [SerializeField] private CTSliceViewer sliceViewer;
        [SerializeField] private InputActionProperty thumbstickAction;
        [SerializeField] private float scrollThreshold = 0.5f;
        [SerializeField] private float scrollCooldown = 0.2f;

        private float lastScrollTime;

        private void Update()
        {
            if (sliceViewer == null || thumbstickAction.action == null) return;

            Vector2 thumbstick = thumbstickAction.action.ReadValue<Vector2>();

            if (Time.time - lastScrollTime < scrollCooldown) return;

            if (thumbstick.y > scrollThreshold)
            {
                sliceViewer.NextSlice();
                lastScrollTime = Time.time;
            }
            else if (thumbstick.y < -scrollThreshold)
            {
                sliceViewer.PreviousSlice();
                lastScrollTime = Time.time;
            }
        }
    }
}
