using System;
using UnityEngine;

namespace AIAdventure.LLM
{
    /// <summary>
    /// Modelin JSON çıktısını ayrıştırır ve doğrular:
    /// - Metnin içinden ilk '{' ile son '}' arasını alır (model fazladan yazı eklese bile).
    /// - "reply" boşsa yanıtı geçersiz sayar.
    /// - "nerve_change" değerini güvenli aralığa sıkıştırır.
    /// - Tanınmayan "action" değerini "continue" kabul eder.
    /// </summary>
    public class JsonNpcResponseParser : INpcResponseParser
    {
        [Serializable]
        private class Dto
        {
            public string reply;
            public int nerve_change;
            public string action;
        }

        private readonly int maxNerveChange;

        public JsonNpcResponseParser(int maxNerveChange = 30)
        {
            this.maxNerveChange = maxNerveChange;
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

            int nerve = Mathf.Clamp(dto.nerve_change, -maxNerveChange, maxNerveChange);
            response = new NpcResponse(dto.reply.Trim(), nerve, ParseAction(dto.action));
            error = null;
            return true;
        }

        private static NpcAction ParseAction(string action)
        {
            switch (action?.Trim().ToLowerInvariant())
            {
                case "spin": return NpcAction.Spin;
                case "pass_turn":
                case "pass": return NpcAction.PassTurn;
                case "surrender": return NpcAction.Surrender;
                default: return NpcAction.Continue;
            }
        }
    }
}
