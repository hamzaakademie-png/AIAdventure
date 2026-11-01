namespace AIAdventure.LLM
{
    public enum LLMErrorType
    {
        None,
        ServerUnreachable,
        Timeout,
        ModelNotFound,
        HttpError,
        EmptyResponse,
        Cancelled,
        Unknown
    }

    /// <summary>
    /// Bir LLM isteğinin sonucu. İstek başarısız olsa bile istisna fırlatılmaz;
    /// hata türü burada döner, böylece oyun hiçbir durumda çökmez.
    /// </summary>
    public readonly struct LLMResult
    {
        public bool Success { get; }
        public string Content { get; }
        public LLMErrorType Error { get; }
        public string ErrorMessage { get; }
        public long LatencyMs { get; }

        private LLMResult(bool success, string content, LLMErrorType error, string errorMessage, long latencyMs)
        {
            Success = success;
            Content = content;
            Error = error;
            ErrorMessage = errorMessage;
            LatencyMs = latencyMs;
        }

        public static LLMResult Ok(string content, long latencyMs) =>
            new LLMResult(true, content, LLMErrorType.None, null, latencyMs);

        public static LLMResult Fail(LLMErrorType error, string message, long latencyMs) =>
            new LLMResult(false, null, error, message, latencyMs);
    }
}
