using System;
using UnityEngine;

namespace AIAdventure.LLM
{
    /// <summary>
    /// Modelin JSON çıktısını ayrıştırır ve doğrular:
    /// - Metnin içinden ilk '{' ile son '}' arasını alır (model fazladan yazı eklese bile).
    /// - "reply" boşsa yanıtı geçersiz sayar.
    /// - "impact" değerini 1-3 aralığına sıkıştırır.
    /// - Tanınmayan "player_intent" değerini "neutral", "action" değerini "continue" kabul eder.
    /// </summary>
    public class JsonNpcResponseParser : INpcResponseParser
    {
        [Serializable]
        private class Dto
        {
            public string reply;
            public string player_intent;
            public int impact;
            public string action;
        }

        public bool TryParse(string raw, out NpcResponse response, out string error)
        {
            response = default;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Boş yanıt.";
                return false;
            }

            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                error = "Yanıtta JSON nesnesi bulunamadı.";
                return false;
            }

            Dto dto;
            try
            {
                dto = JsonUtility.FromJson<Dto>(raw.Substring(start, end - start + 1));
            }
            catch (Exception exception)
            {
                error = "Geçersiz JSON: " + exception.Message;
                return false;
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.reply))
            {
                error = "\"reply\" alanı eksik ya da boş.";
                return false;
            }

            response = new NpcResponse(
                dto.reply.Trim(),
                ParseIntent(dto.player_intent),
                Mathf.Clamp(dto.impact, 1, 3),
                ParseAction(dto.action));
            error = null;
            return true;
        }

        private static PlayerIntent ParseIntent(string intent)
        {
            switch (intent?.Trim().ToLowerInvariant())
            {
                case "bluff": return PlayerIntent.Bluff;
                case "insult": return PlayerIntent.Insult;
                case "irrelevant": return PlayerIntent.Irrelevant;
                case "beg": return PlayerIntent.Beg;
                case "deal": return PlayerIntent.Deal;
                default: return PlayerIntent.Neutral;
            }
        }

        private static NpcAction ParseAction(string action)
        {
            switch (action?.Trim().ToLowerInvariant())
            {
                case "spin": return NpcAction.Spin;
                case "pass_turn":
                case "pass": return NpcAction.PassTurn;
                case "add_bullet": return NpcAction.AddBullet;
                case "surrender": return NpcAction.Surrender;
                case "accept_baron_first": return NpcAction.AcceptBaronFirst;
                case "accept_double": return NpcAction.AcceptDouble;
                case "accept_remove_bullet": return NpcAction.AcceptRemoveBullet;
                default: return NpcAction.Continue;
            }
        }
    }
}
