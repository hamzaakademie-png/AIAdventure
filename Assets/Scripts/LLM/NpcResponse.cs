namespace AIAdventure.LLM
{
    /// <summary>NPC'nin oyunu etkileyen kararı.</summary>
    public enum NpcAction
    {
        Continue,
        Spin,
        PassTurn,
        AddBullet,
        Surrender,
        AcceptBaronFirst,
        AcceptDouble,
        AcceptRemoveBullet
    }

    /// <summary>Modelin, oyuncunun mesajına dair yorumu.</summary>
    public enum PlayerIntent
    {
        Neutral,
        Bluff,
        Insult,
        Irrelevant,
        Beg,
        Deal
    }

    /// <summary>
    /// Modelin JSON çıktısından ayrıştırılmış, doğrulanmış yanıt.
    /// Şema: {"reply": string, "player_intent": string, "impact": 1-3, "action": string}
    /// Küçük modeller sayısal puan üretmekte tutarsız olduğu için model sadece
    /// niyeti ve şiddeti söyler; soğukkanlılık değişimini oyun kuralları hesaplar.
    /// </summary>
    public readonly struct NpcResponse
    {
        public string Reply { get; }
        public PlayerIntent Intent { get; }
        public int Impact { get; }
        public NpcAction Action { get; }

        public NpcResponse(string reply, PlayerIntent intent, int impact, NpcAction action)
        {
            Reply = reply;
            Intent = intent;
            Impact = impact;
            Action = action;
        }
    }
}
