using System;
using AIAdventure.Companion.States;
using AIAdventure.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AIAdventure.Companion
{
    /// <summary>
    /// Companion'ın beyni: durum makinesini çalıştırır.
    /// Hareketi NavMeshAgent yapar; bu sınıf sadece hangi durumda olunacağına
    /// ve ne hızla gidileceğine karar verir.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CompanionSensor))]
    public class CompanionController : MonoBehaviour, ICompanionCommandReceiver
    {
        [SerializeField] private Transform player;

        [Header("Takip")]
        [Tooltip("Oyuncuya bu kadar yaklaşınca durur.")]
        [SerializeField] private float followDistance = 2.5f;
        [Tooltip("Oyuncu bu kadar uzaklaşınca peşinden gitmeye başlar.")]
        [SerializeField] private float resumeDistance = 4f;
        [Tooltip("Oyuncu bu kadar uzaktaysa koşarak yetişir.")]
        [SerializeField] private float catchUpDistance = 7f;

        [Header("Hız")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float acceleration = 6f;
        [SerializeField] private float angularSpeed = 400f;
        [SerializeField] private float slowDownDistance = 0.8f;

        [Header("Etkileşim")]
        [SerializeField] private float interactRange = 1.2f;
        [SerializeField] private float interactDuration = 1f;

        [Header("Et (ödül)")]
        [Tooltip("Açıksa tilki ilk emirden önce et ister.")]
        [SerializeField] private bool requiresTreat = true;
        [SerializeField] private float eatDuration = 2f;
        [Tooltip("Yere bırakılacak et modeli. Boşsa basit bir et parçası oluşturulur.")]
        [SerializeField] private GameObject treatPrefab;

        private ICompanionState currentState;
        private float desiredSpeed;
        private bool fed;

        public NavMeshAgent Agent { get; private set; }
        public CompanionSensor Sensor { get; private set; }
        public Transform Player => player;
        public float FollowDistance => followDistance;
        public float ResumeDistance => resumeDistance;
        public float CatchUpDistance => catchUpDistance;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float InteractDuration => interactDuration;
        public float EatDuration => eatDuration;
        public GameObject TreatPrefab => treatPrefab;
        public string CurrentStateName => currentState != null ? currentState.Name : "None";

        public bool IsBusy => currentState != null && currentState is not FollowState;
        public bool WantsTreat => requiresTreat && !fed;

        /// <summary>Durum değiştiğinde tetiklenir (animasyon, UI ve testler için).</summary>
        public event Action<string> StateChanged;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Sensor = GetComponent<CompanionSensor>();
            Agent.acceleration = acceleration;
            Agent.angularSpeed = angularSpeed;
            desiredSpeed = walkSpeed;
        }

        private void Start()
        {
            ChangeState(new FollowState(this));
        }

        private void Update()
        {
            currentState?.Tick();
        }

        public void ChangeState(ICompanionState newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();

            Debug.Log($"[Companion] Durum: {currentState.Name}");
            StateChanged?.Invoke(currentState.Name);
        }

        public void GoInteract(IInteractable target)
        {
            if (target == null || !target.CanInteract) return;
            if (IsBusy)
            {
                Debug.Log("[Companion] Meşgulüm, önce elimdeki işi bitiriyorum.");
                return;
            }
            if (WantsTreat)
            {
                Debug.Log("[Companion] Tilki ilgisiz, karnı aç görünüyor.");
                return;
            }
            ChangeState(new MoveToTargetState(this, target));
        }

        public void FeedThenInteract(IInteractable target)
        {
            if (target == null || !target.CanInteract || IsBusy) return;
            fed = true;
            ChangeState(new EatState(this, target));
        }

        public void SetDesiredSpeed(float speed) => desiredSpeed = speed;

        /// <summary>Sensör yakında engel görürse yavaşlar.</summary>
        public void ApplySensorSpeed()
        {
            Agent.speed = Sensor.HasObstacleWithin(slowDownDistance) ? desiredSpeed * 0.5f : desiredSpeed;
        }

        public float DistanceToPlayer()
        {
            if (player == null) return 0f;
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        public bool IsWithinInteractRange(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude <= interactRange;
        }

        public bool HasReachedDestination()
        {
            return !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.1f;
        }
    }
}
