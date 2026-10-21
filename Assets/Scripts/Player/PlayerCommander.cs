using AIAdventure.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Player
{
    /// <summary>
    /// E tuşuna basılınca en yakın etkileşilebilir nesneyi bulur ve companion'a
    /// "git, onu kullan" emri verir. Companion'ı sadece arayüz üzerinden tanır.
    /// </summary>
    public class PlayerCommander : MonoBehaviour
    {
        [SerializeField] private GameObject companionObject;
        [SerializeField] private float commandRange = 15f;

        private ICompanionCommandReceiver companion;

        private void Awake()
        {
            if (companionObject != null)
                companion = companionObject.GetComponent<ICompanionCommandReceiver>();

            if (companion == null)
                Debug.LogError("[PlayerCommander] Companion Object alanına companion'ı sürükle.");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || companion == null) return;
            if (!keyboard.eKey.wasPressedThisFrame) return;

            IInteractable target = FindNearestInteractable();
            if (target == null)
            {
                Debug.Log("[Player] Yakında kullanılacak bir şey yok.");
                return;
            }

            Debug.Log("[Player] Companion'a emir verildi.");
            companion.GoInteract(target);
        }

        private IInteractable FindNearestInteractable()
        {
            IInteractable nearest = null;
            float nearestDistance = commandRange;

            Collider[] hits = Physics.OverlapSphere(transform.position, commandRange,
                ~0, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable == null || !interactable.CanInteract) continue;

                float distance = Vector3.Distance(transform.position, interactable.Position);
                if (distance < nearestDistance)
                {
                    nearest = interactable;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
    }
}
