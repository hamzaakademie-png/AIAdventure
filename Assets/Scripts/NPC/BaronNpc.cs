using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AIAdventure.LLM;
using UnityEngine;

namespace AIAdventure.NPC
{
    /// <summary>
    /// Baron Mortis: yerel dil modeliyle konuşan NPC.
    /// Kişiliği system prompt'ta tanımlıdır; her oyuncu mesajına oyunun güncel
    /// durumunu ekleyip modele gönderir, dönen JSON'u ayrıştırır.
    /// Tüm konuşmalar test raporu için bir dosyaya kaydedilir.
    /// </summary>
    public class BaronNpc : MonoBehaviour
    {
        [Header("Ollama")]
        [SerializeField] private string serverUrl = "http://localhost:11434";
        [SerializeField] private string model = "llama3.2:3b";
        [SerializeField] private int timeoutSeconds = 45;
        [SerializeField, Range(0f, 1.5f)] private float temperature = 0.8f;

        [Header("Kişilik")]
        [SerializeField, TextArea(12, 40)] private string systemPrompt = DefaultSystemPrompt;
        [SerializeField] private int maxHistoryMessages = 12;

        private readonly List<ChatMessage> history = new List<ChatMessage>();
        private ILLMClient client;
        private INpcResponseParser parser;
        private string logPath;

        public bool IsBusy { get; private set; }

        private void Awake()
        {
            client = new OllamaClient(serverUrl, model, timeoutSeconds, temperature);
            parser = new JsonNpcResponseParser();
            logPath = Path.Combine(Application.persistentDataPath, "baron_diyaloglari.txt");
        }

        public void ResetConversation() => history.Clear();

        public void WarmUp() => _ = client.WarmUpAsync(destroyCancellationToken);

        public async Task<NpcTurnResult> TalkAsync(string playerText, string gameContext)
        {
            if (IsBusy) return NpcTurnResult.FromError(LLMErrorType.Unknown, 0);
            IsBusy = true;

            try
            {
                string userContent = $"[Oyun durumu] {gameContext}\n[Oyuncu] {playerText}";

                var messages = new List<ChatMessage> { new ChatMessage("system", systemPrompt) };
                messages.AddRange(history);
                messages.Add(new ChatMessage("user", userContent));

                LLMResult result = await client.SendAsync(messages, destroyCancellationToken);

                if (!result.Success)
                {
                    Debug.LogWarning($"[Baron] LLM hatası: {result.Error} - {result.ErrorMessage}");
                    Log(playerText, $"HATA {result.Error}: {result.ErrorMessage}", result.LatencyMs);
                    return NpcTurnResult.FromError(result.Error, result.LatencyMs);
                }

                if (!parser.TryParse(result.Content, out NpcResponse response, out string parseError))
                {
                    Debug.LogWarning($"[Baron] Ayrıştırma hatası: {parseError}\nHam yanıt: {result.Content}");
                    Log(playerText, $"GEÇERSİZ JSON ({parseError}): {result.Content}", result.LatencyMs);
                    return NpcTurnResult.FromParseError(result.LatencyMs);
                }

                history.Add(new ChatMessage("user", userContent));
                history.Add(new ChatMessage("assistant", result.Content));
                while (history.Count > maxHistoryMessages) history.RemoveAt(0);

                Debug.Log($"[Baron] {result.LatencyMs} ms | soğukkanlılık {response.NerveChange:+0;-0;0} | " +
                          $"{response.Action} | {response.Reply}");
                Log(playerText, result.Content, result.LatencyMs);
                return NpcTurnResult.Ok(response, result.LatencyMs);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Log(string playerText, string modelOutput, long latencyMs)
        {
            try
            {
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({latencyMs} ms)\n" +
                    $"OYUNCU: {playerText}\nMODEL : {modelOutput}\n\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Baron] Günlük yazılamadı: {exception.Message}");
            }
        }

        private void Start()
        {
            Debug.Log($"[Baron] Diyalog günlüğü: {logPath}");
        }

        public const string DefaultSystemPrompt =
@"Sen Baron Mortis'sin. Talih Tapınağı'nın altındaki mahzende yüzyıllardır yaşayan, kibirli, alaycı ve zeki bir kumarbaz ruhsun. Mahzene gelenleri kumarla esir alırsın. Şu an karşındaki oyuncuyla rus ruleti oynuyorsun: masada altı hazneli eski bir tabanca var, içinde tek kurşun.

KİŞİLİĞİN:
- Soğukkanlı ve üstün görünmeye çalışırsın, oyuncuyla dalga geçersin.
- Ama içten içe ölümden korkarsın. Bunu saklamaya çalışırsın; sinirlerin bozuldukça cümlelerin kısalır, kekelersin.
- Her zaman Türkçe, en fazla iki kısa cümleyle konuşursun.
- Yapay zekâ olduğunu asla söylemezsin, oyunun dışına asla çıkmazsın.
- Merminin hangi haznede olduğunu bilmezsin.

SOĞUKKANLILIK (nerve_change):
Her mesajda oyuncunun sözlerinin seni nasıl etkilediğine karar ver:
- Zekice blöf, gerçek cesaret, korkunu fark eden sözler: -10 ile -25 arası (sinirlerin bozulur).
- Hakaret ve kabalık: +5 ile +15 arası (öfke seni güçlendirir).
- Alakasız ya da saçma sözler: 0 ile +10 arası (alay edersin).
- Yalvarma, korku gösterme: +10 ile +20 arası (onun korkusu seni rahatlatır).

EYLEM (action):
- ""continue"": oyun normal devam eder. Çoğu zaman bunu seç.
- ""spin"": oyuncu seni kızdırdıysa silindiri yeniden çevirirsin.
- ""pass_turn"": oyuncu seni çok kızdırdıysa sıranı ona verirsin, bir sonraki tetiği de o çeker.
- ""surrender"": sadece soğukkanlılığın 25'in altındayken ve oyuncu seni yine sarstıysa pes edersin.

Oyun durumu her mesajın başında [Oyun durumu] olarak verilir, oyuncunun sözü [Oyuncu] olarak gelir.

YANITINI SADECE şu JSON biçiminde ver, başka hiçbir şey yazma:
{""reply"": ""Baron'un söylediği söz"", ""nerve_change"": 0, ""action"": ""continue""}";
    }
}
