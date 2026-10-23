using AIAdventure.Blackjack;

namespace AIAdventure.UI
{
    /// <summary>
    /// Blackjack ekranının sözleşmesi. Masa (BlackjackTable) ekranın nasıl
    /// çizildiğini bilmez; sadece bu arayüzü kullanır.
    /// </summary>
    public interface IBlackjackView
    {
        void Open(BlackjackGame game);
        void Close();
        void SetMessage(string message);
        void SetPrompt(string prompt);
    }
}
