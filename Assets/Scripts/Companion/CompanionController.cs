using System;
using AIAdventure.Companion.States;
using AIAdventure.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AIAdventure.Companion
{
    /// <summary>
    /// Companion'ın beyni: durum makinesini çalıştırır.
    /// Hareketi NavMeshAgent yapar; bu sınıf sadece hangi durumda olunacağına karar verir.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CompanionSensor))]
    public class CompanionController : MonoBehaviour, ICompanionCommandReceiver
    {
        [SerializeField] private Transform player;
        [SerializeField] private float followDistance = 2.5f;
        [SerializeField] private float interactRange = 1.2f;
        [SerializeField] private float interactDuration = 1f;
        [SerializeField] private float slowDownDistance = 0.8f;

        private ICompanionState currentState;
        private float baseSpeed;

        public NavMeshAgent Agent { get; private set; }
        public CompanionSensor Sensor { get; private set; }
        public Transform Player => player;
        public float FollowDistance => followDistance;
        public float InteractDuration => interactDuration;
        public string CurrentStateName => currentState != null ? currentState.Name : "None";

        public bool IsBusy => currentState != null && currentState is not FollowState;

        /// <summary>Durum değiştiğinde tetiklenir (UI ve testler için).</summary>
        public event Action<string> StateChanged;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Sensor = GetComponent<CompanionSensor>();
            baseSpeed = Agent.speed;
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
            ChangeState(new MoveToTargetState(this, target));
        }

        /// <summary>Sensör yakında engel görürse yavaşlar.</summary>
        public void ApplySensorSpeed()
        {
            Agent.speed = Sensor.HasObstacleWithin(slowDownDistance) ? baseSpeed * 0.5f : baseSpeed;
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
