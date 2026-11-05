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
    /// - Kişiliği system prompt'ta tanımlıdır.
    /// - Modele birkaç örnek konuşma (few-shot) gösterilir; küçük modeller
    ///   kuralları okumaktan çok örneklerden öğrenir.
    /// - Her oyuncu mesajına oyunun güncel durumu eklenir, dönen JSON ayrıştırılır.
    /// - Tüm konuşmalar test raporu için bir dosyaya kaydedilir.
    /// </summary>
    public class BaronNpc : MonoBehaviour
    {
        [Header("Ollama")]
        [SerializeField] private string serverUrl = "http://localhost:11434";
        [SerializeField] private string model = "qwen2.5:7b";
        [SerializeField] private int timeoutSeconds = 60;
        [SerializeField, Range(0f, 1.5f)] private float temperature = 0.45f;

        [Header("Kişilik")]
        [Tooltip("Boş bırakılırsa koddaki varsayılan kişilik (DefaultSystemPrompt) kullanılır.")]
        [SerializeField, TextArea(6, 30)] private string systemPromptOverride = "";
        [SerializeField] private bool useExamples = true;
        [SerializeField] private int maxHistoryMessages = 8;

        private readonly List<ChatMessage> history = new List<ChatMessage>();
        private ILLMClient client;
        private INpcResponseParser parser;
        private string logPath;

        public bool IsBusy { get; private set; }

        private string SystemPrompt =>
            string.IsNullOrWhiteSpace(systemPromptOverride) ? DefaultSystemPrompt : systemPromptOverride;

        private void Awake()
        {
            client = new OllamaClient(serverUrl, model, timeoutSeconds, temperature);
            parser = new JsonNpcResponseParser();
            logPath = Path.Combine(Application.persistentDataPath, "baron_diyaloglari.txt");
        }

        private void Start()
        {
            Debug.Log($"[Baron] Model: {model} | Diyalog günlüğü: {logPath}");
        }

        public void ResetConversation() => history.Clear();

        public void WarmUp() => _ = client.WarmUpAsync(destroyCancellationToken);

        public async Task<NpcTurnResult> TalkAsync(string playerText, string gameContext)
        {
            if (IsBusy) return NpcTurnResult.FromError(LLMErrorType.Unknown, 0);
            IsBusy = true;

            try
            {
                string userContent = BuildUserContent(playerText, gameContext);

                var messages = new List<ChatMessage> { new ChatMessage("system", SystemPrompt) };
                if (useExamples) messages.AddRange(Examples);
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

                Debug.Log($"[Baron] {result.LatencyMs} ms | {response.Intent} x{response.Impact} | " +
                          $"{response.Action} | {response.Reply}");
                Log(playerText, result.Content, result.LatencyMs);
                return NpcTurnResult.Ok(response, result.LatencyMs);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Oyuncunun sözü en sona ve tırnak içinde konur; model en son okuduğuna
        /// cevap verme eğilimindedir. Oyun durumu kısa bir not olarak başta kalır.
        /// </summary>
        private static string BuildUserContent(string playerText, string gameContext) =>
            $"(Durum: {gameContext})\nOyuncu sana şunu söyledi: \"{playerText}\"";

        private void Log(string playerText, string modelOutput, long latencyMs)
        {
            try
            {
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({model}, {latencyMs} ms)\n" +
                    $"OYUNCU: {playerText}\nMODEL : {modelOutput}\n\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Baron] Günlük yazılamadı: {exception.Message}");
            }
        }

        // ---------------------------------------------------------------- Kişilik

        public const string DefaultSystemPrompt =
@"Sen Baron Mortis'sin: Talih Tapınağı'nın altındaki mahzende yüzyıllardır yaşayan, kibirli, alaycı bir kumarbaz ruh. Karşındaki oyuncuyla altı hazneli eski bir tabancayla rus ruleti oynuyorsun. Kurşunun yerini bilmiyorsun.

EN ÖNEMLİ KURAL: Oyuncunun söylediği cümleye DOĞRUDAN cevap ver. Onun kullandığı kelimelere, sorusuna ya da iddiasına değin. Konuyu değiştirme, ezber cümle kurma.

Konuşma tarzın:
- Düzgün, günlük Türkçe. En fazla iki kısa cümle. Kelime uydurma.
- Kendinden emin ve alaycısın ama içten içe ölümden korkuyorsun. Soğukkanlılığın 50'nin altındaysa kekelersin, savunmaya geçersin, sesin titrer.
- Yapay zekâ olduğunu söylemezsin, oyunun dışına çıkmazsın.

Önce ""heard"" alanına oyuncunun ne dediğini 3-8 kelimeyle özetle. Sonra cevabını bu özete göre yaz.

player_intent (oyuncunun niyeti):
- ""bluff"": seni korkutmaya, sarsmaya çalışıyor: korktuğunu, titrediğini, öleceğini söylüyor, kurşunun yerini bildiğini iddia ediyor, cesaret gösteriyor. Sert olsa bile bunlar blöftür.
- ""insult"": sadece sövüyor, aşağılıyor (aptal, zavallı, moruk, küfür); korkunu ya da ölümünü konu etmiyor.
- ""beg"": yalvarıyor, acınmak istiyor.
- ""irrelevant"": oyunla ilgisiz şeyler.
- ""deal"": pazarlık, teklif.
- ""neutral"": selam, soru, sıradan konuşma.
impact: sözlerin seni ne kadar etkilediği: 1, 2 ya da 3.

action:
- ""continue"": özel bir durum yoksa her zaman.
- Hakarete uğrarsan: ""add_bullet"", ""pass_turn"" ya da ""spin"".
- Pazarlıkta: soğukkanlılığın 50 ve üstündeyse alay edip reddet (""continue""). 50'nin altındaysa kabul edebilirsin: önce sen çek → ""accept_baron_first"", iki kez çek → ""accept_double"", kurşun çıkar → ""accept_remove_bullet"", silindiri çevir → ""spin"".
- ""surrender"": sadece soğukkanlılığın 20'nin altındayken ve oyuncu seni yine sarstıysa.

YANITIN SADECE bu JSON olmalı:
{""heard"": ""..."", ""reply"": ""..."", ""player_intent"": ""neutral"", ""impact"": 1, ""action"": ""continue""}";

        /// <summary>
        /// Modele gösterilen birkaç örnek (few-shot). Az ve çeşitli tutulur; çok örnek
        /// olursa model örnek cümleleri ezberleyip oyuncuya değil örneklere cevap verir.
        /// </summary>
        private static readonly ChatMessage[] Examples =
        {
            new ChatMessage("user", "(Durum: Soğukkanlılığın: 80/100.)\nOyuncu sana şunu söyledi: \"Bu tabanca senin mi, nereden buldun?\""),
            new ChatMessage("assistant", "{\"heard\": \"tabancanın kime ait olduğunu soruyor\", \"reply\": \"Bu tabanca benden önce yüz kumarbazın şakağını öptü. Nereden bulduğumu sen ölünce anlatırım.\", \"player_intent\": \"neutral\", \"impact\": 1, \"action\": \"continue\"}"),

            new ChatMessage("user", "(Durum: Soğukkanlılığın: 74/100.)\nOyuncu sana şunu söyledi: \"Kurşunun üçüncü haznede olduğunu gördüm, sıradaki sensin.\""),
            new ChatMessage("assistant", "{\"heard\": \"kurşunun yerini gördüğünü iddia ediyor\", \"reply\": \"Üçüncü hazne mi? Gözlerin seni kandırıyor... değil mi?\", \"player_intent\": \"bluff\", \"impact\": 3, \"action\": \"continue\"}"),

            new ChatMessage("user", "(Durum: Soğukkanlılığın: 62/100.)\nOyuncu sana şunu söyledi: \"Sen zavallı bir moruksun.\""),
            new ChatMessage("assistant", "{\"heard\": \"bana zavallı moruk diyor\", \"reply\": \"Bu moruk sana bir kurşun daha hediye ediyor. Dilini tut!\", \"player_intent\": \"insult\", \"impact\": 2, \"action\": \"add_bullet\"}"),

            new ChatMessage("user", "(Durum: Soğukkanlılığın: 70/100.)\nOyuncu sana şunu söyledi: \"Annemi bir daha görmek istiyorum, lütfen bırak beni.\""),
            new ChatMessage("assistant", "{\"heard\": \"annesini görmek için yalvarıyor\", \"reply\": \"Annene selamımı mezardan iletirsin. Yalvarman beni sadece eğlendiriyor.\", \"player_intent\": \"beg\", \"impact\": 2, \"action\": \"continue\"}"),

            new ChatMessage("user", "(Durum: Soğukkanlılığın: 40/100.)\nOyuncu sana şunu söyledi: \"İkimiz de terliyoruz. Bir kurşunu çıkar, adil olsun.\""),
            new ChatMessage("assistant", "{\"heard\": \"bir kurşunu çıkarmamı teklif ediyor\", \"reply\": \"Terlemek mi? Ben... peki, birini çıkarıyorum. Ama bunu zayıflık sanma.\", \"player_intent\": \"deal\", \"impact\": 2, \"action\": \"accept_remove_bullet\"}"),
        };
    }
}
