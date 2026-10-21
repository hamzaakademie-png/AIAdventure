namespace AIAdventure.Core
{
    /// <summary>
    /// Oyuncunun companion'a emir verebilmesi için gereken arayüz.
    /// Oyuncu tarafı companion'ın iç yapısını bilmez, sadece bu arayüzü bilir.
    /// </summary>
    public interface ICompanionCommandReceiver
    {
        bool IsBusy { get; }
        void GoInteract(IInteractable target);
    }
}
