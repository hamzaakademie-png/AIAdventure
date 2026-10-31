using UnityEngine;

namespace AIAdventure.Player
{
    /// <summary>
    /// Oyuncunun görünen karakter modeli üzerinde durur. Oyuncunun gerçek hızını
    /// (CharacterController) okuyup StarterAssets Animator'ına iletir; böylece
    /// dururken bekleme, yürürken yürüme, hızlanınca koşma animasyonu oynar.
    /// Hareket mantığı PlayerMovement'ta kalır, bu sınıf sadece görüntüden sorumludur.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");

        [SerializeField] private float smoothing = 10f;

        private Animator animator;
        private CharacterController controller;
        private float speed;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            controller = GetComponentInParent<CharacterController>();
            animator.applyRootMotion = false;

            if (controller == null)
                Debug.LogError("[PlayerAnimator] Bu model Player'ın içinde (alt nesnesi) olmalı.");
        }

        private void Update()
        {
            if (controller == null) return;

            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            speed = Mathf.Lerp(speed, velocity.magnitude, smoothing * Time.deltaTime);

            animator.SetFloat(SpeedId, speed);
            animator.SetFloat(MotionSpeedId, 1f);
            animator.SetBool(GroundedId, true);
        }

        // StarterAssets animasyonlarındaki olaylar bu metotları çağırır.
        // Boş bırakıldı; olmazlarsa Unity "receiver yok" hatası verir.
        private void OnFootstep(AnimationEvent animationEvent) { }
        private void OnLand(AnimationEvent animationEvent) { }
    }
}
