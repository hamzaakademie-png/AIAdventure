using UnityEngine;

namespace AIAdventure.Core
{
    /// <summary>
    /// Oyuncunun okuyabildiği her şey (not, yazıt, günlük sayfası) bu arayüzü uygular.
    /// </summary>
    public interface IReadable
    {
        string Title { get; }
        string Body { get; }
        Vector3 Position { get; }
    }
}
