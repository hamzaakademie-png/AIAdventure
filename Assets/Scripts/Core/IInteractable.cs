using UnityEngine;

namespace AIAdventure.Core
{
    /// <summary>
    /// Companion'ın gidip etkileşebileceği nesneler (şalter, kol vb.) bu arayüzü uygular.
    /// </summary>
    public interface IInteractable
    {
        Vector3 Position { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
