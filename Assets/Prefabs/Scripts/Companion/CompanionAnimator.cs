using UnityEngine;
using UnityEngine.AI;

namespace AIAdventure.Companion
{
    /// <summary>
    /// Tilki modelinin üzerinde durur. Animator'daki "Speed" parametresini
    /// NavMeshAgent'ın gerçek hızıyla besler; Idle/Walk/Run geçişini Blend Tree
    /// yumuşak biçimde yapar (ani geçiş, "ping" yok).
    /// Kolu çekerken "Interact" tetikleyicisini çalıştırır.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CompanionAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int InteractHash = Animator.StringToHash("Interact");

        [Tooltip("Speed parametresinin hedefe yaklaşma süresi (sn). Büyük = daha yumuşak.")]
        [SerializeField] private float dampTime = 0.15f;

        private Animator animator;
        private NavMeshAgent agent;
        private CompanionController controller;
        private bool hasSpeed, hasInteract;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.speed = 1f;
            agent = GetComponentInParent<NavMeshAgent>();
            controller = GetComponentInParent<CompanionController>();

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == SpeedHash) hasSpeed = true;
                if (p.nameHash == InteractHash) hasInteract = true;
            }

            if (agent == null)
                Debug.LogError($"[CompanionAnimator] '{name}' Companion'ın alt nesnesi olmalı.");
            if (!hasSpeed)
                Debug.LogWarning($"[CompanionAnimator] '{name}' Animator'ında 'Speed' (Float) parametresi yok.");
        }

        private void OnEnable()
        {
            if (controller != null) controller.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (controller != null) controller.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (agent == null || !hasSpeed) return;

            // Sadece yatay hız: merdivende/eğimde zıplamasın.
            Vector3 v = agent.velocity;
            v.y = 0f;
            animator.SetFloat(SpeedHash, v.magnitude, dampTime, Time.deltaTime);
        }

        private void OnStateChanged(string state)
        {
            if (state == "Interact" && hasInteract)
                animator.SetTrigger(InteractHash);
        }
    }
}
