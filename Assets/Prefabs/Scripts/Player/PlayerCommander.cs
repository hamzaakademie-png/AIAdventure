using AIAdventure.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Player
{
    /// <summary>
    /// E tuşuna basılınca en yakın etkileşilebilir nesneyi bulur ve companion'a
    /// "git, onu kullan" emri verir. Companion et bekliyorsa önce cepteki eti verir.
    /// Companion'ı sadece arayüz üzerinden tanır.
    /// </summary>
    public class PlayerCommander : MonoBehaviour
    {
        [SerializeField] private GameObject companionObject;
        [SerializeField] private float commandRange = 15f;

        [Header("Et")]
        [SerializeField] private int meatCount = 1;
        [SerializeField] private float messageDuration = 3f;

        private ICompanionCommandReceiver companion;
        private string message;
        private float messageTimer;
        private GUIStyle style;

        public int MeatCount => meatCount;

        private void Awake()
        {
            if (companionObject != null)
                companion = companionObject.GetComponent<ICompanionCommandReceiver>();

            if (companion == null)
                Debug.LogError("[PlayerCommander] Companion Object alanına companion'ı sürükle.");
        }

        private void Update()
        {
            if (messageTimer > 0f) messageTimer -= Time.deltaTime;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || companion == null) return;
            if (!keyboard.eKey.wasPressedThisFrame) return;

            IInteractable target = FindNearestInteractable();
            if (target == null)
            {
                Show("Yakında kullanılacak bir şey yok.");
                return;
            }

            if (companion.IsBusy)
            {
                Show("Tilki şu an meşgul.");
                return;
            }

            if (companion.WantsTreat)
            {
                if (meatCount <= 0)
                {
                    Show("Tilki ilgisiz bakıyor... Karnı aç gibi.");
                    return;
                }
                meatCount--;
                Show("Cebinden bir parça et çıkarıp tilkiye verdin.");
                companion.FeedThenInteract(target);
                return;
            }

            Show("Tilkiye işaret ettin.");
            companion.GoInteract(target);
        }

        private void Show(string text)
        {
            message = text;
            messageTimer = messageDuration;
            Debug.Log($"[Player] {text}");
        }

        private void OnGUI()
        {
            if (messageTimer <= 0f || string.IsNullOrEmpty(message)) return;

            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(messageTimer));
            Rect rect = new Rect(Screen.width * 0.2f, Screen.height - 140f, Screen.width * 0.6f, 50f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, message, style);
            GUI.color = old;
        }

        private IInteractable FindNearestInteractable()
        {
            IInteractable nearest = null;
            float nearestDistance = commandRange;

            Collider[] hits = Physics.OverlapSphere(transform.position, commandRange,
                ~0, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent(out IInteractable interactable) || !interactable.CanInteract) continue;

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
