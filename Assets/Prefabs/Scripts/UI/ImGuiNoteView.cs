using UnityEngine;

namespace AIAdventure.UI
{
    /// <summary>
    /// Notları eski bir kâğıt görünümünde ekrana çizer, yakındaki nota
    /// yaklaşınca "[F] Notu oku" ipucunu gösterir. Kurulum gerektirmez.
    /// </summary>
    public class ImGuiNoteView : MonoBehaviour, INoteView
    {
        private const float ReferenceHeight = 1080f;

        private string title;
        private string body;
        private string prompt;

        private GUIStyle paperStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle hintStyle;
        private GUIStyle promptStyle;
        private Texture2D paperTexture;
        private Texture2D dimTexture;

        public bool IsOpen { get; private set; }

        public void Show(string noteTitle, string noteBody)
        {
            title = noteTitle;
            body = noteBody;
            IsOpen = true;
        }

        public void Hide() => IsOpen = false;

        public void SetPrompt(string text) => prompt = text;

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            if (IsOpen)
                DrawNote(width);
            else if (!string.IsNullOrEmpty(prompt))
                GUI.Box(new Rect((width - 320f) / 2f, ReferenceHeight - 140f, 320f, 56f), prompt, promptStyle);
        }

        private void DrawNote(float width)
        {
            GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), dimTexture);

            Rect paper = new Rect((width - 820f) / 2f, (ReferenceHeight - 760f) / 2f, 820f, 760f);
            GUI.Box(paper, GUIContent.none, paperStyle);

            GUILayout.BeginArea(new Rect(paper.x + 50f, paper.y + 40f, paper.width - 100f, paper.height - 80f));
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(24f);
            GUILayout.Label(body, bodyStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("[F] Kapat", hintStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            Color ink = new Color(0.22f, 0.13f, 0.06f);
            paperTexture = MakeTexture(new Color(0.91f, 0.85f, 0.70f));
            dimTexture = MakeTexture(new Color(0f, 0f, 0f, 0.75f));

            paperStyle = new GUIStyle(GUI.skin.box) { normal = { background = paperTexture } };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ink }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23, wordWrap = true, normal = { textColor = ink }
            };
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(ink.r, ink.g, ink.b, 0.7f) }
            };
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (paperTexture != null) Destroy(paperTexture);
            if (dimTexture != null) Destroy(dimTexture);
        }
    }
}
