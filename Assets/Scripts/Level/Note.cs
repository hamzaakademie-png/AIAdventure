using AIAdventure.Core;
using UnityEngine;

namespace AIAdventure.Level
{
    /// <summary>
    /// Yerde ya da kaide üzerinde duran, okunabilen bir kâğıt.
    /// Metni Inspector'dan değiştirilebilir.
    /// </summary>
    public class Note : MonoBehaviour, IReadable
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(4, 15)] private string body;

        public string Title => title;
        public string Body => body;
        public Vector3 Position => transform.position;

        public void Setup(string noteTitle, string noteBody)
        {
            title = noteTitle;
            body = noteBody;
        }
    }
}
