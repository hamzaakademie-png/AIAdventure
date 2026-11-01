namespace AIAdventure.Companion
{
    /// <summary>
    /// Durum makinesindeki (FSM) her durum bu arayüzü uygular.
    /// </summary>
    public interface ICompanionState
    {
        string Name { get; }
        void Enter();
        void Tick();
        void Exit();
    }
}
