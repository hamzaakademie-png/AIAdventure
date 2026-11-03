using UnityEngine;

namespace AIAdventure.Blackjack
{
    /// <summary>
    /// Kurpiyerin modeline (Animator'ün olduğu objeye) takılır.
    /// PlayDeal çağrılınca Animator'e kart dağıtma tetiğini gönderir.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class DealerBody : MonoBehaviour, IDealerBody
    {
        [Tooltip("Animator Controller'daki trigger parametresinin adı.")]
        [SerializeField] private string dealTrigger = "Dagit";

        [Tooltip("Animasyon başladıktan kaç saniye sonra el en öne uzanıyor; kart o an uçar.")]
        [SerializeField] private float releaseDelay = 0.6f;

        private Animator animator;
        private int dealTriggerHash;

        public float ReleaseDelay => releaseDelay;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            // Trigger adını her seferinde string olarak aramamak için bir kere hash'e çeviriyoruz.
            dealTriggerHash = Animator.StringToHash(dealTrigger);
        }

        public void PlayDeal()
        {
            animator.SetTrigger(dealTriggerHash);
        }
    }
}
