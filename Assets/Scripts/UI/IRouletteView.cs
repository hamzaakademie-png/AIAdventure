using System;
using AIAdventure.Roulette;

namespace AIAdventure.UI
{
    /// <summary>
    /// Rulet masasının ekran sözleşmesi. Masa (RouletteTable) ekranın nasıl
    /// çizildiğini bilmez.
    /// </summary>
    public interface IRouletteView
    {
        /// <summary>Oyuncu bir mesaj yazıp Enter'a bastığında tetiklenir.</summary>
        event Action<string> MessageSubmitted;

        /// <summary>Oyuncu şu an yazı alanına yazıyor mu? (O sırada kısayol tuşları çalışmaz.)</summary>
        bool IsTyping { get; }

        void Open(RouletteGame game);
        void Close();
        void AddChatLine(string speaker, string text);
        void AddNarration(string text);
        void AddSystemLine(string text);
        void SetThinking(bool thinking);
        void SetHint(string hint);
        void SetPrompt(string prompt);
        void ShowEnding(bool playerWon, string text);
        void FlashShot();
    }
}
