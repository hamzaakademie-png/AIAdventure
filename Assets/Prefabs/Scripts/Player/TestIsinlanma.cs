using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Player
{
    /// <summary>
    /// TEST ARACI: Sayı tuşlarına basınca oyuncuyu belirlenen noktalara ışınlar.
    /// Player'a ekle, "Noktalar" listesine boş objeler sürükle (1 = ilk, 2 = ikinci...).
    /// Teslimden önce Player'dan kaldırılabilir.
    /// </summary>
    public class TestIsinlanma : MonoBehaviour
    {
        [SerializeField] private Transform[] noktalar;

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || noktalar == null) return;

            for (int i = 0; i < noktalar.Length && i < 9; i++)
            {
                if (noktalar[i] == null) continue;
                if (!k[Key.Digit1 + i].wasPressedThisFrame) continue;

                // CharacterController açıkken konum değişmez; kısa süre kapatılır.
                CharacterController controller = GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                transform.SetPositionAndRotation(noktalar[i].position, noktalar[i].rotation);
                if (controller != null) controller.enabled = true;

                Debug.Log($"[Test] Oyuncu '{noktalar[i].name}' noktasına ışınlandı.");
            }
        }
    }
}
