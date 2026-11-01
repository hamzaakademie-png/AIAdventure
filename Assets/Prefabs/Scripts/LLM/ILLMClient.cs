using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AIAdventure.LLM
{
    /// <summary>
    /// Bir dil modeli sunucusuyla konuşmanın sözleşmesi. NPC'ler sadece bu arayüzü
    /// bilir; Ollama yerine başka bir yerel sunucu kullanılırsa sadece yeni bir
    /// sınıf yazmak yeterli olur.
    /// </summary>
    public interface ILLMClient
    {
        Task<LLMResult> SendAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);

        /// <summary>Modeli belleğe önceden yükler; ilk cevabın gecikmesini azaltır.</summary>
        Task WarmUpAsync(CancellationToken cancellationToken);
    }
}
