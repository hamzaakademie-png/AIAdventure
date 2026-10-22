using AIAdventure.Core;
using UnityEngine;

namespace AIAdventure.Level
{
    /// <summary>
    /// Şalter. Companion etkileşince rengi değişir ve bağlı kapıyı açar.
    /// Kapıyı IOpenable arayüzü üzerinden tanır.
    /// </summary>
    public class Lever : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject targetObject;
        [SerializeField] private Color activatedColor = Color.green;

        private IOpenable target;
        private bool used;

        public Vector3 Position => transform.position;
        public bool CanInteract => !used;

        private void Awake()
        {
            if (targetObject != null)
                target = targetObject.GetComponent<IOpenable>();

            if (target == null)
                Debug.LogError("[Lever] Target Object alanına açılacak kapıyı (Gate) sürükle.");
        }

        public void Interact()
        {
            if (used) return;
            used = true;

            Renderer leverRenderer = GetComponent<Renderer>();
            if (leverRenderer != null)
                leverRenderer.material.color = activatedColor;

            Debug.Log("[Lever] Şalter indirildi.");
            target?.Open();
        }
    }
}
