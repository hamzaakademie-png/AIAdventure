using UnityEngine;

namespace AIAdventure.Player
{
    /// <summary>
    /// Geçici hata ayıklama aracı: oyuncu bir şeye çarpınca onun adını
    /// Console'a yazar. Sorun çözülünce Player'dan kaldırılabilir.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CarpismaDedektoru : MonoBehaviour
    {
        private Collider last;
        private float timer;

        private void Update() => timer -= Time.deltaTime;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Zemine basmayı sayma, sadece önümüzü kesenleri yaz.
            if (hit.normal.y > 0.7f) return;
            if (hit.collider == last && timer > 0f) return;

            last = hit.collider;
            timer = 1f;
            Debug.Log($"[Çarpışma] Önümü kesen: '{GetPath(hit.collider.transform)}' " +
                      $"({hit.collider.GetType().Name})", hit.collider);
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
    }
}
