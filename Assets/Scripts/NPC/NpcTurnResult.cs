using AIAdventure.LLM;

namespace AIAdventure.NPC
{
    /// <summary>
    /// NPC ile bir konuşma turunun sonucu: ya geçerli bir yanıt, ya da oyuncuya
    /// gösterilecek anlamlı bir hata mesajı.
    /// </summary>
    public readonly struct NpcTurnResult
    {
        public bool Success { get; }
        public NpcResponse Response { get; }
        public LLMErrorType Error { get; }
        public bool ParseFailed { get; }
        public long LatencyMs { get; }

        private NpcTurnResult(bool success, NpcResponse response, LLMErrorType error, bool parseFailed, long latencyMs)
        {
            Success = success;
            Response = response;
            Error = error;
            ParseFailed = parseFailed;
            LatencyMs = latencyMs;
        }

        public static NpcTurnResult Ok(NpcResponse response, long latencyMs) =>
            new NpcTurnResult(true, response, LLMErrorType.None, false, latencyMs);

        public static NpcTurnResult FromError(LLMErrorType error, long latencyMs) =>
            new NpcTurnResult(false, default, error, false, latencyMs);

        public static NpcTurnResult FromParseError(long latencyMs) =>
            new NpcTurnResult(false, default, LLMErrorType.None, true, latencyMs);

        /// <summary>Hata durumunda oyuncuya gösterilecek açıklama.</summary>
        public string UserMessage
        {
            get
            {
                if (Success) return null;
                if (ParseFailed) return "Baron anlaşılmaz bir şeyler mırıldandı. (Modelden geçersiz yanıt geldi, tekrar dene.)";

                switch (Error)
                {
                    case LLMErrorType.ServerUnreachable:
                        return "Baron'a ulaşılamıyor: Ollama çalışmıyor. Ollama'yı başlatıp tekrar yazabilirsin. Oyun şimdilik sadece şansla sürüyor.";
                    case LLMErrorType.Timeout:
                        return "Baron çok uzun düşündü ve cevap veremedi (zaman aşımı). Tekrar dene.";
                    case LLMErrorType.ModelNotFound:
                        return "Dil modeli bulunamadı. CMD'de 'ollama pull llama3.2:3b' komutunu çalıştır.";
                    case LLMErrorType.EmptyResponse:
                        return "Baron sessiz kaldı. (Model boş yanıt döndü, tekrar dene.)";
                    default:
                        return "Baron'la bağlantıda bir sorun oluştu. Tekrar dene.";
                }
            }
        }
    }
}
