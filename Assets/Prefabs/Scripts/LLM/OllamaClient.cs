using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace AIAdventure.LLM
{
    /// <summary>
    /// Yerel Ollama sunucusuna (/api/chat) asenkron istek atar.
    /// - Ana iş parçacığını bloklamaz (UnityWebRequest + await).
    /// - "format: json" ile modeli geçerli JSON üretmeye zorlar.
    /// - Sunucu kapalı, zaman aşımı, model yok gibi durumları LLMResult ile bildirir.
    /// </summary>
    public class OllamaClient : ILLMClient
    {
        [Serializable]
        private class ChatRequest
        {
            public string model;
            public ChatMessage[] messages;
            public bool stream;
            public string format;
            public Options options;
        }

        [Serializable]
        private class Options
        {
            public float temperature;
            public int num_predict;
        }

        [Serializable]
        private class ChatResponse
        {
            public ChatMessage message;
            public string error;
        }

        private readonly string chatUrl;
        private readonly string model;
        private readonly int timeoutSeconds;
        private readonly float temperature;

        public OllamaClient(string serverUrl, string model, int timeoutSeconds, float temperature)
        {
            chatUrl = serverUrl.TrimEnd('/') + "/api/chat";
            this.model = model;
            this.timeoutSeconds = timeoutSeconds;
            this.temperature = temperature;
        }

        public async Task<LLMResult> SendAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
        {
            var body = new ChatRequest
            {
                model = model,
                messages = new List<ChatMessage>(messages).ToArray(),
                stream = false,
                format = "json",
                options = new Options { temperature = temperature, num_predict = 200 }
            };

            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                using UnityWebRequest request = await PostAsync(JsonUtility.ToJson(body), timeoutSeconds, cancellationToken);
                long latency = watch.ElapsedMilliseconds;

                if (cancellationToken.IsCancellationRequested)
                    return LLMResult.Fail(LLMErrorType.Cancelled, "İstek iptal edildi.", latency);

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    bool timedOut = request.error != null &&
                                    request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
                    return timedOut
                        ? LLMResult.Fail(LLMErrorType.Timeout, $"{timeoutSeconds} sn içinde cevap gelmedi.", latency)
                        : LLMResult.Fail(LLMErrorType.ServerUnreachable, request.error, latency);
                }

                string text = request.downloadHandler.text;
                ChatResponse response = TryReadResponse(text);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string detail = response?.error ?? request.error;
                    return request.responseCode == 404
                        ? LLMResult.Fail(LLMErrorType.ModelNotFound, detail, latency)
                        : LLMResult.Fail(LLMErrorType.HttpError, $"HTTP {request.responseCode}: {detail}", latency);
                }

                string content = response?.message?.content;
                return string.IsNullOrWhiteSpace(content)
                    ? LLMResult.Fail(LLMErrorType.EmptyResponse, "Model boş cevap döndü.", latency)
                    : LLMResult.Ok(content, latency);
            }
            catch (Exception exception)
            {
                return LLMResult.Fail(LLMErrorType.Unknown, exception.Message, watch.ElapsedMilliseconds);
            }
        }

        public async Task WarmUpAsync(CancellationToken cancellationToken)
        {
            try
            {
                string body = "{\"model\":\"" + model + "\",\"messages\":[]}";
                using UnityWebRequest request = await PostAsync(body, 120, cancellationToken);
                Debug.Log(request.result == UnityWebRequest.Result.Success
                    ? $"[Ollama] '{model}' belleğe yüklendi."
                    : $"[Ollama] Isınma isteği başarısız: {request.error}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Ollama] Isınma isteği atılamadı: {exception.Message}");
            }
        }

        private async Task<UnityWebRequest> PostAsync(string json, int timeout, CancellationToken cancellationToken)
        {
            var request = new UnityWebRequest(chatUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeout
            };
            request.SetRequestHeader("Content-Type", "application/json");

            var completion = new TaskCompletionSource<bool>();
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => completion.TrySetResult(true);

            using (cancellationToken.Register(() =>
                   {
                       request.Abort();
                       completion.TrySetResult(false);
                   }))
            {
                await completion.Task;
            }
            return request;
        }

        private static ChatResponse TryReadResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try { return JsonUtility.FromJson<ChatResponse>(text); }
            catch { return null; }
        }
    }
}
