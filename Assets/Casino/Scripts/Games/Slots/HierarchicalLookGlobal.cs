using UnityEngine;

namespace Assets.Casino.Slots
{
    public class HierarchicalLookGlobal : MonoBehaviour
    {
        [Header("Hierarchy Links")]
        [SerializeField] private Transform headSphere; 

        [Header("Limits")]
        [Range(0f, 90f)][SerializeField] private float maxHeadX = 20f;     
        [Range(0f, 90f)][SerializeField] private float maxShouldersX = 30f;

        private Transform cameraTransform;

        void Update()
        {
            if (cameraTransform == null)
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
                return;
            }

            float cameraYaw = cameraTransform.eulerAngles.y;   
            float cameraPitch = cameraTransform.eulerAngles.x; 
            float cameraRoll = cameraTransform.eulerAngles.z;  


            if (cameraPitch > 180f) cameraPitch -= 360f;


            float totalMaxAngle = maxHeadX + maxShouldersX;


            float clampedPitch = Mathf.Clamp(cameraPitch, -totalMaxAngle, totalMaxAngle);

            float headX = 0f;
            float shouldersX = 0f;

            if (totalMaxAngle > 0f) 
            {
                float pitchPercent = clampedPitch / totalMaxAngle;

                headX = pitchPercent * maxHeadX;

                float absolutePitch = Mathf.Abs(clampedPitch);
                if (absolutePitch > maxHeadX)
                {
                    float sign = Mathf.Sign(clampedPitch);
                    shouldersX = (absolutePitch - maxHeadX) * sign;
                }
            }


            transform.rotation = Quaternion.Euler(shouldersX, cameraYaw, cameraRoll);

            if (headSphere != null)
            {
                headSphere.rotation = transform.rotation * Quaternion.Euler(headX, 0f, 0f);
            }
        }
    }
}