using System.Collections;
using AIAdventure.LLM;

namespace AIAdventure.NPC
{
    /// <summary>
    /// Baron'un beden dili. Rulet masası, Baron'un nasıl canlandırıldığını
    /// (animasyon, IK) bilmez; sadece bu arayüzü kullanır.
    /// </summary>
    public interface IBaronBody
    {
        /// <summary>Oyuncunun niyetine göre beden tepkisi verir (kızma, gülme, gerilme, konuşma).</summary>
        void React(PlayerIntent intent);

        /// <summary>Soğukkanlılık düşükken bekleme pozu gergin olur.</summary>
        void SetNervous(bool nervous);

        /// <summary>Masadaki mermiyi alıp silaha koyar, silindiri çevirir, silahı geri bırakır.</summary>
        IEnumerator LoadGun();

        /// <summary>Masadaki silahı alıp şakağına götürür.</summary>
        IEnumerator RaiseGunToTemple();

        /// <summary>Silahı masaya geri bırakır.</summary>
        IEnumerator LowerGun();

        /// <summary>Kurşun ona denk geldi.</summary>
        void Die();
    }
}
