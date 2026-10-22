using UnityEngine;

namespace AIAdventure.Player
{
    /// <summary>
    /// Kamerayı hedefi (oyuncuyu) sabit bir mesafeden yumuşakça takip ettirir.
    /// Mahzen gibi kapalı alanlarda SetView ile kamera alçaltılabilir.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -8f);
        [SerializeField] private float lookHeight = 0f;
        [SerializeField] private float smoothSpeed = 5f;

        public Vector3 Offset => offset;
        public float LookHeight => lookHeight;

        public void SetView(Vector3 newOffset, float newLookHeight)
        {
            offset = newOffset;
            lookHeight = newLookHeight;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
