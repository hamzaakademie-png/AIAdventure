using System.Collections;
using AIAdventure.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AIAdventure.Level
{
    /// <summary>
    /// Parmaklık. Open() çağrılınca yavaşça yukarı kalkar ve
    /// NavMesh üzerindeki engelini kaldırır.
    /// </summary>
    public class Gate : MonoBehaviour, IOpenable
    {
        [SerializeField] private float openHeight = 3.2f;
        [SerializeField] private float openDuration = 1.5f;

        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            Debug.Log("[Gate] Parmaklık açılıyor.");
            StartCoroutine(Raise());
        }

        private IEnumerator Raise()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * openHeight;
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, elapsed / openDuration);
                yield return null;
            }
            transform.position = end;

            NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null)
                obstacle.enabled = false;
        }
    }
}
