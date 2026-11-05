using System.Collections;
using AIAdventure.Player;
using UnityEngine;

namespace AIAdventure.Level
{
    /// <summary>
    /// Final sahnesi: oyuncu Leyla'nın odasına girince kontrol kapanır, kamera yandan
    /// ikisini birlikte gösterir, oyuncu kendi yürüme animasyonuyla Leyla'ya yürür,
    /// ikisi birbirine dönüp sarılır. Ardından bitiş ekranı açılır.
    /// Animator'larda "Hug" adlı durum olmalı (Leyla'da ve oyuncunun modelinde).
    /// Leyla'da isteğe bağlı "StandUp" durumu varsa önce ayağa kalkar.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LeylaReunion : MonoBehaviour
    {
        private const string StateStandUp = "StandUp";
        private const string StateHug = "Hug";

        [Header("Bağlantılar")]
        [SerializeField] private Animator leyla;
        [Tooltip("Boşsa oyuncunun içindeki Animator kendiliğinden bulunur.")]
        [SerializeField] private Animator playerModel;
        [Tooltip("Boşsa sahnedeki EndingZone kendiliğinden bulunur.")]
        [SerializeField] private EndingZone ending;

        [Header("Yürüme ve sarılma")]
        [SerializeField] private float walkSpeed = 1.4f;
        [Tooltip("Sarılmaya başlamak için iki karakter arasındaki mesafe (metre).")]
        [SerializeField] private float hugDistance = 0.4f;
        [SerializeField] private float standUpTime = 2f;
        [SerializeField] private float hugTime = 4f;

        [Header("Kamera")]
        [SerializeField] private float cameraSide = 2.6f;
        [SerializeField] private float cameraHeight = 1.5f;
        [SerializeField] private float lookHeight = 1.3f;
        [SerializeField] private float cameraSpeed = 2f;

        private SeatCameraController cameraController;
        private PlayerMovement player;
        private CharacterController controller;
        private bool started;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            cameraController = new SeatCameraController(follow);

            if (ending == null) ending = FindAnyObjectByType<EndingZone>();
            if (leyla == null) Debug.LogError("[LeylaReunion] Leyla alanına Leyla'yı sürükle.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (started || leyla == null) return;
            player = other.GetComponentInParent<PlayerMovement>();
            if (player == null) return;

            started = true;
            controller = player.GetComponent<CharacterController>();
            if (playerModel == null) playerModel = player.GetComponentInChildren<Animator>();
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            Debug.Log("[Final] Leyla ile kavuşma başladı.");
            player.SetInputEnabled(false);
            UpdateCamera();

            if (Play(leyla, StateStandUp, warnIfMissing: false))
                yield return new WaitForSeconds(standUpTime);

            // Oyuncu kendi yürüme animasyonuyla Leyla'ya yürür (en fazla 10 sn).
            for (float t = 0f; t < 10f; t += Time.deltaTime)
            {
                Vector3 toLeyla = Flat(leyla.transform.position - player.transform.position);
                float distance = toLeyla.magnitude;
                if (distance <= hugDistance) break;

                Vector3 direction = toLeyla / distance;
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                    Quaternion.LookRotation(direction), 8f * Time.deltaTime);
                leyla.transform.rotation = Quaternion.Slerp(leyla.transform.rotation,
                    Quaternion.LookRotation(-direction), 4f * Time.deltaTime);

                float step = Mathf.Min(walkSpeed * Time.deltaTime, distance - hugDistance);
                if (controller != null) controller.Move(direction * step);
                else player.transform.position += direction * step;

                UpdateCamera();
                yield return null;
            }

            // Tam karşı karşıya dön ve sarıl.
            Vector3 face = Flat(leyla.transform.position - player.transform.position).normalized;
            if (face.sqrMagnitude > 0.001f)
            {
                player.transform.rotation = Quaternion.LookRotation(face);
                leyla.transform.rotation = Quaternion.LookRotation(-face);
            }
            Play(leyla, StateHug, warnIfMissing: true);
            Play(playerModel, StateHug, warnIfMissing: true);
            UpdateCamera();

            yield return new WaitForSeconds(hugTime);

            if (ending != null) ending.Finish(player);
            else Debug.LogWarning("[LeylaReunion] Sahnede EndingZone yok; bitiş ekranı açılamadı.");
        }

        private void LateUpdate() => cameraController?.Tick(cameraSpeed);

        /// <summary>Kamerayı iki karakteri yandan gösterecek şekilde yerleştirir.</summary>
        private void UpdateCamera()
        {
            Vector3 a = player.transform.position;
            Vector3 b = leyla.transform.position;
            Vector3 middle = (a + b) * 0.5f;
            Vector3 line = Flat(b - a);
            if (line.sqrMagnitude < 0.001f) line = player.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, line.normalized);

            Vector3 position = middle + side * cameraSide + Vector3.up * cameraHeight;
            cameraController.Sit(position, middle + Vector3.up * lookHeight);
        }

        private static bool Play(Animator animator, string state, bool warnIfMissing)
        {
            if (animator == null) return false;
            if (!animator.HasState(0, Animator.StringToHash(state)))
            {
                if (warnIfMissing)
                    Debug.LogWarning($"[LeylaReunion] '{animator.name}' Animator'ında '{state}' adlı durum yok.");
                return false;
            }
            animator.CrossFadeInFixedTime(state, 0.3f);
            return true;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
