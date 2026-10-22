using AIAdventure.Core;
using AIAdventure.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Player
{
    /// <summary>
    /// Yakındaki notu bulur, F ile okutur. Okurken oyuncunun hareketini kapatır.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerReader : MonoBehaviour
    {
        [SerializeField] private float readRange = 2.2f;

        private PlayerMovement movement;
        private INoteView view;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            view = GetComponent<INoteView>();

            if (view == null)
                Debug.LogError("[PlayerReader] Player'a ImGuiNoteView bileşenini de ekle.");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (view == null || keyboard == null) return;

            if (view.IsOpen)
            {
                if (keyboard.fKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                {
                    view.Hide();
                    movement.SetInputEnabled(true);
                }
                return;
            }

            IReadable nearest = FindNearestReadable();
            view.SetPrompt(nearest != null ? "[F] Notu oku" : null);

            if (nearest != null && keyboard.fKey.wasPressedThisFrame)
            {
                view.SetPrompt(null);
                view.Show(nearest.Title, nearest.Body);
                movement.SetInputEnabled(false);
            }
        }

        private IReadable FindNearestReadable()
        {
            IReadable nearest = null;
            float nearestDistance = readRange;

            foreach (Collider hit in Physics.OverlapSphere(transform.position, readRange, ~0, QueryTriggerInteraction.Ignore))
            {
                IReadable readable = hit.GetComponent<IReadable>();
                if (readable == null) continue;

                float distance = Vector3.Distance(transform.position, readable.Position);
                if (distance <= nearestDistance)
                {
                    nearest = readable;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
    }
}
