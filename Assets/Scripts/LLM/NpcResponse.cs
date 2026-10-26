namespace AIAdventure.LLM
{
    /// <summary>NPC'nin oyunu etkileyen kararı.</summary>
    public enum NpcAction
    {
        Continue,
        Spin,
        PassTurn,
        Surrender
    }

    /// <summary>
    /// Modelin JSON çıktısından ayrıştırılmış, doğrulanmış yanıt.
    /// Şema: {"reply": string, "nerve_change": int, "action": "continue|spin|pass_turn|surrender"}
    /// </summary>
    public readonly struct NpcResponse
    {
        public string Reply { get; }
        public int NerveChange { get; }
        public NpcAction Action { get; }

        public NpcResponse(string reply, int nerveChange, NpcAction action)
        {
            Reply = reply;
            NerveChange = nerveChange;
            Action = action;
        }
    }
}
