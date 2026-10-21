using AIAdventure.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AIAdventure.Companion.States
{
    /// <summary>
    /// Emir verilen hedefe NavMesh ile yol bulup gider.
    /// Yol yoksa, yarımsa ya da süre aşılırsa vazgeçip oyuncuyu takibe döner.
    /// </summary>
    public class MoveToTargetState : ICompanionState
    {
        private const float Timeout = 20f;

        private readonly CompanionController companion;
        private readonly IInteractable target;
        private float elapsed;
        private bool requestFailed;

        public string Name => "MoveToTarget";

        public MoveToTargetState(CompanionController companion, IInteractable target)
        {
            this.companion = companion;
            this.target = target;
        }

        public void Enter()
        {
            elapsed = 0f;
            companion.Agent.stoppingDistance = 0.5f;
            requestFailed = !companion.Agent.SetDestination(target.Position);
        }

        public void Tick()
        {
            companion.ApplySensorSpeed();
            elapsed += Time.deltaTime;

            if (requestFailed)
            {
                Fail("Hedefe yol isteği gönderilemedi.");
                return;
            }

            if (companion.Agent.pathPending) return;

            if (companion.Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Fail("Hedefe ulaşan tam bir yol yok.");
                return;
            }

            if (companion.IsWithinInteractRange(target.Position))
            {
                companion.ChangeState(new InteractState(companion, target));
                return;
            }

            if (companion.HasReachedDestination())
            {
                Fail("Yolun sonuna geldim ama hedefe yetişemiyorum.");
                return;
            }

            if (elapsed > Timeout)
                Fail("Zaman aşımı.");
        }

        public void Exit() { }

        private void Fail(string reason)
        {
            Debug.LogWarning($"[Companion] {reason}");
            companion.ChangeState(new FollowState(companion));
        }
    }
}
