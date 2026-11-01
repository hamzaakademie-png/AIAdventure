using UnityEngine;

namespace AIAdventure.Companion.States
{
    /// <summary>
    /// Varsayılan durum: oyuncuyu doğal biçimde takip eder.
    /// - Oyuncu yakındaysa durur, başını ona çevirir.
    /// - Oyuncu "resumeDistance"tan uzaklaşınca yürüyerek peşinden gider.
    /// - Çok geride kalırsa koşarak yetişir.
    /// - Yeterince yaklaşınca tekrar durur (iki eşik sayesinde sürekli dur-kalk yapmaz).
    /// Hedef her 0.25 sn'de yenilenir, yolu NavMesh her seferinde yeniden hesaplar.
    /// </summary>
    public class FollowState : ICompanionState
    {
        private const float RepathInterval = 0.25f;
        private const float TurnSpeed = 4f;

        private readonly CompanionController companion;
        private float timer;
        private bool following;

        public string Name => "Follow";

        public FollowState(CompanionController companion)
        {
            this.companion = companion;
        }

        public void Enter()
        {
            companion.Agent.stoppingDistance = companion.FollowDistance;
            timer = 0f;
            following = companion.DistanceToPlayer() > companion.ResumeDistance;
        }

        public void Tick()
        {
            if (companion.Player == null) return;

            float distance = companion.DistanceToPlayer();

            if (!following)
            {
                FacePlayer();
                if (distance > companion.ResumeDistance) following = true;
                else return;
            }

            if (distance <= companion.FollowDistance)
            {
                following = false;
                companion.Agent.ResetPath();
                return;
            }

            companion.SetDesiredSpeed(distance > companion.CatchUpDistance ? companion.RunSpeed : companion.WalkSpeed);
            companion.ApplySensorSpeed();

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = RepathInterval;
            companion.Agent.SetDestination(companion.Player.position);
        }

        public void Exit() { }

        private void FacePlayer()
        {
            Vector3 toPlayer = companion.Player.position - companion.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.01f) return;

            companion.transform.rotation = Quaternion.Slerp(companion.transform.rotation,
                Quaternion.LookRotation(toPlayer), TurnSpeed * Time.deltaTime);
        }
    }
}
