using UnityEngine;

namespace AIAdventure.Companion.States
{
    /// <summary>
    /// Varsayılan durum: oyuncuyu belli bir mesafeden takip eder.
    /// Hedef her 0.25 sn'de yenilenir, yolu NavMesh her seferinde yeniden hesaplar.
    /// </summary>
    public class FollowState : ICompanionState
    {
        private const float RepathInterval = 0.25f;

        private readonly CompanionController companion;
        private float timer;

        public string Name => "Follow";

        public FollowState(CompanionController companion)
        {
            this.companion = companion;
        }

        public void Enter()
        {
            companion.Agent.stoppingDistance = companion.FollowDistance;
            timer = 0f;
        }

        public void Tick()
        {
            companion.ApplySensorSpeed();

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = RepathInterval;

            if (companion.Player != null)
                companion.Agent.SetDestination(companion.Player.position);
        }

        public void Exit() { }
    }
}
