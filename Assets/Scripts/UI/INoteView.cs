namespace AIAdventure.UI
{
    /// <summary>
    /// Not okuma ekranının sözleşmesi. Okuma mantığı (PlayerReader) ekranın
    /// nasıl çizildiğini bilmez; ileride farklı bir arayüz yazılırsa sadece bu
    /// arayüzü uygulaması yeterli olur.
    /// </summary>
    public interface INoteView
    {
        bool IsOpen { get; }
        void Show(string title, string body);
        void Hide();
        void SetPrompt(string text);
    }
}
