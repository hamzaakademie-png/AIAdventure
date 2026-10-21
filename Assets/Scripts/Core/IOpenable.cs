namespace AIAdventure.Core
{
    /// <summary>
    /// Açılabilen her şey (parmaklık, NPC'nin koruduğu kapı) bu arayüzü uygular.
    /// Şalter de NPC de kapıyı bu arayüz üzerinden açar, birbirlerini tanımazlar.
    /// </summary>
    public interface IOpenable
    {
        bool IsOpen { get; }
        void Open();
    }
}
