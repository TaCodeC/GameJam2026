using UnityEngine;

namespace GameJam.Player.Cave
{
    internal static class CaveCameraSnapper
    {
        private const string SmoothCameraFollowTypeName = "SmoothCameraFollow";
        private const string SnapMethodName = "SnapToTarget";

        public static void SnapAfterTeleport(Transform playerTransform, Vector3 previousPlayerPosition)
        {
            if (playerTransform == null)
                return;

            SnapActiveCameras(playerTransform, previousPlayerPosition);
            NotifySmoothCameraFollowers();
        }

        private static void SnapActiveCameras(Transform playerTransform, Vector3 previousPlayerPosition)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || !camera.isActiveAndEnabled)
                    continue;

                Transform cameraTransform = camera.transform;
                Vector3 currentOffset = cameraTransform.position - previousPlayerPosition;
                cameraTransform.position = playerTransform.position + currentOffset;
            }
        }

        private static void NotifySmoothCameraFollowers()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != SmoothCameraFollowTypeName)
                    continue;

                behaviour.SendMessage(SnapMethodName, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
