using AIAdventure.LLM;

namespace AIAdventure.Roulette
{
    /// <summary>
    /// Oyuncunun niyetinin (LLM'in yorumu) Baron'un soğukkanlılığına etkisi.
    /// Model niyeti ve şiddeti (1-3) söyler, puanı bu tablo belirler; böylece
    /// "hakaret Baron'u güçlendirir, blöf zayıflatır" kuralı her zaman tutarlıdır.
    /// </summary>
    public static class NerveRules
    {
        public static int Delta(PlayerIntent intent, int impact)
        {
            switch (intent)
            {
                case PlayerIntent.Bluff: return -(8 + 7 * impact);   // -15, -22, -29
                case PlayerIntent.Insult: return 5 * impact;         // +5, +10, +15
                case PlayerIntent.Beg: return 6 * impact;            // +6, +12, +18
                case PlayerIntent.Irrelevant: return 3 * impact;     // +3, +6, +9
                default: return 0;                                   // nötr ve pazarlık
            }
        }

        /// <summary>
        /// Modelin seçtiği eylemi niyetle karşılaştırır; tutarsızsa "continue"ya çevirir.
        /// Ceza eylemleri sadece hakarete, anlaşmalar sadece pazarlığa verilebilir.
        /// </summary>
        public static NpcAction ValidateAction(PlayerIntent intent, NpcAction action)
        {
            switch (action)
            {
                case NpcAction.AddBullet:
                case NpcAction.PassTurn:
                    return intent == PlayerIntent.Insult ? action : NpcAction.Continue;

                case NpcAction.Spin:
                    return intent == PlayerIntent.Insult || intent == PlayerIntent.Deal ? action : NpcAction.Continue;

                case NpcAction.AcceptBaronFirst:
                case NpcAction.AcceptDouble:
                case NpcAction.AcceptRemoveBullet:
                    return intent == PlayerIntent.Deal ? action : NpcAction.Continue;

                default:
                    return action;
            }
        }

        /// <summary>Oyuncuya, sözlerinin etkisini anlatan kısa açıklama.</summary>
        public static string Describe(PlayerIntent intent, int delta)
        {
            switch (intent)
            {
                case PlayerIntent.Bluff: return $"Sözlerin işledi, Baron'un sinirleri sarsıldı ({delta}).";
                case PlayerIntent.Insult: return $"Hakaretin Baron'u öfkelendirdi; öfke onu güçlendiriyor (+{delta}).";
                case PlayerIntent.Beg: return $"Korkunu gören Baron rahatladı (+{delta}).";
                case PlayerIntent.Irrelevant: return $"Baron saçmalığına gülüyor, rahatladı (+{delta}).";
                default: return null;
            }
        }
    }
}
