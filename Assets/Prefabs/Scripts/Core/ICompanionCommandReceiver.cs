namespace AIAdventure.Core
{
    /// <summary>
    /// Oyuncunun companion'a emir verebilmesi için gereken arayüz.
    /// Oyuncu tarafı companion'ın iç yapısını bilmez, sadece bu arayüzü bilir.
    /// </summary>
    public interface ICompanionCommandReceiver
    {
        bool IsBusy { get; }

        /// <summary>Companion emir dinlemeden önce ödül (et) bekliyor mu?</summary>
        bool WantsTreat { get; }

        void GoInteract(IInteractable target);

        /// <summary>Önce verilen eti yer, sonra hedefe gidip etkileşir.</summary>
        void FeedThenInteract(IInteractable target);
    }
}
