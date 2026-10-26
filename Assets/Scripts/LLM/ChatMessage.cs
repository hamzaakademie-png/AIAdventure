using System;

namespace AIAdventure.LLM
{
    /// <summary>
    /// Dil modeline giden/gelen tek bir mesaj. role: "system", "user" veya "assistant".
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage() { }

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}
