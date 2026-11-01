namespace AIAdventure.LLM
{
    /// <summary>
    /// Modelin ham metnini oyunun anlayacağı NpcResponse'a çevirir.
    /// Bozuk girdide istisna fırlatmaz, false döner.
    /// </summary>
    public interface INpcResponseParser
    {
        bool TryParse(string raw, out NpcResponse response, out string error);
    }
}
