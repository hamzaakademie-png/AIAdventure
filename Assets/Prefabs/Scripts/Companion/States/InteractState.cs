using AIAdventure.Core;
using UnityEngine;

namespace AIAdventure.Companion.States
{
    /// <summary>
    /// Hedefe ulaşınca durur, ona döner, kısa bir süre bekler ve etkileşir.
    /// Sonra oyuncuyu takibe döner.
    /// </summary>
    public class InteractState : ICompanionState
    {
        private readonly CompanionController companion;
        private readonly IInteractable target;
        private float timer;

        public string Name => "Interact";

        public InteractState(CompanionController companion, IInteractable target)
        {
            this.companion = companion;
            this.target = target;
        }

        public void Enter()
        {
            companion.Agent.ResetPath();
            timer = companion.InteractDuration;

            Vector3 direction = target.Position - companion.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                companion.transform.rotation = Quaternion.LookRotation(direction);
        }

        public void Tick()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (target.CanInteract)
                target.Interact();

            companion.ChangeState(new FollowState(companion));
        }

        public void Exit() { }
    }
}
