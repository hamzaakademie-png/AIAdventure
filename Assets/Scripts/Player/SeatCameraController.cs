using UnityEngine;

namespace AIAdventure.Player
{
    /// <summary>
    /// Oyuncu bir masaya oturduğunda kamerayı takipten çıkarıp sabit bir
    /// oturma açısına yumuşakça taşır; kalkınca takibi geri açar.
    /// Hem rulet hem de diğer masalar tarafından kullanılabilir.
    /// </summary>
    public class SeatCameraController
    {
        private readonly CameraFollow follow;
        private readonly Transform cameraTransform;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool active;

        public SeatCameraController(CameraFollow follow)
        {
            this.follow = follow;
            cameraTransform = follow != null ? follow.transform
                : Camera.main != null ? Camera.main.transform : null;
        }

        public void Sit(Vector3 position, Vector3 lookAt)
        {
            targetPosition = position;
            targetRotation = Quaternion.LookRotation(lookAt - position);
            active = true;
            if (follow != null) follow.enabled = false;
        }

        public void Stand()
        {
            active = false;
            if (follow != null) follow.enabled = true;
        }

        public void Tick(float speed)
        {
            if (!active || cameraTransform == null) return;

            float t = speed * Time.deltaTime;
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(cameraTransform.position, targetPosition, t),
                Quaternion.Slerp(cameraTransform.rotation, targetRotation, t));
        }
    }
}
