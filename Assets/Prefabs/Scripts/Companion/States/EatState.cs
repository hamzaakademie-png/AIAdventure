using AIAdventure.Core;
using UnityEngine;

namespace AIAdventure.Companion.States
{
    /// <summary>
    /// Oyuncu ete verdiğinde: tilki durur, önüne bırakılan eti bir süre yer,
    /// sonra emredilen hedefe (kola) gider.
    /// </summary>
    public class EatState : ICompanionState
    {
        private readonly CompanionController companion;
        private readonly IInteractable target;
        private GameObject treat;
        private float timer;

        public string Name => "Eat";

        public EatState(CompanionController companion, IInteractable target)
        {
            this.companion = companion;
            this.target = target;
        }

        public void Enter()
        {
            companion.Agent.ResetPath();
            timer = companion.EatDuration;

            if (companion.Player != null)
            {
                Vector3 toPlayer = companion.Player.position - companion.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    companion.transform.rotation = Quaternion.LookRotation(toPlayer);
            }

            treat = SpawnTreat(companion.transform.position + companion.transform.forward * 0.45f);
        }

        public void Tick()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            companion.ChangeState(new MoveToTargetState(companion, target));
        }

        public void Exit()
        {
            if (treat != null) Object.Destroy(treat);
        }

        private GameObject SpawnTreat(Vector3 around)
        {
            Vector3 position = around;
            if (Physics.Raycast(around + Vector3.up, Vector3.down, out RaycastHit hit, 5f,
                    ~0, QueryTriggerInteraction.Ignore))
                position = hit.point;

            if (companion.TreatPrefab != null)
                return Object.Instantiate(companion.TreatPrefab, position, Quaternion.identity);

            // Prefab yoksa basit bir et parçası.
            GameObject meat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            meat.name = "Et";
            Object.Destroy(meat.GetComponent<Collider>());
            meat.transform.SetPositionAndRotation(position + Vector3.up * 0.06f,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 90f));
            meat.transform.localScale = new Vector3(0.12f, 0.12f, 0.18f);
            meat.GetComponent<Renderer>().material.color = new Color(0.45f, 0.08f, 0.06f);
            return meat;
        }
    }
}
