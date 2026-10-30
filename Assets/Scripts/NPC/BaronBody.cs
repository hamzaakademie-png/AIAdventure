using System;
using System.Collections;
using AIAdventure.Level;
using AIAdventure.LLM;
using UnityEngine;

namespace AIAdventure.NPC
{
    /// <summary>
    /// Baron modelinin üzerindeki beden dili bileşeni.
    /// - Animator'daki durumlar arasında geçiş yapar: Idle, Talk, Laugh, Angry, Nervous.
    /// - IK (ters kinematik) ile sağ elini mermiye, silaha ve şakağına götürür.
    /// - Konuşurken başını oyuncuya (kameraya) çevirir.
    /// Animator'ın Base Layer ayarlarında "IK Pass" açık olmalıdır.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class BaronBody : MonoBehaviour, IBaronBody
    {
        private const string StateIdle = "Idle";
        private const string StateTalk = "Talk";
        private const string StateLaugh = "Laugh";
        private const string StateAngry = "Angry";
        private const string StateNervous = "Nervous";

        private enum Grip { None, Bullet, GunUpright, GunAtTemple }

        [Header("Animasyon")]
        [SerializeField] private float crossFade = 0.25f;
        [SerializeField] private float talkDuration = 3f;
        [SerializeField] private float reactionDuration = 2.5f;

        [Header("Bakış")]
        [SerializeField, Range(0f, 1f)] private float lookAtWeight = 0.7f;

        [Header("Silah ve mermi")]
        [SerializeField] private Transform gun;
        [SerializeField] private Transform bullet;
        [Tooltip("Şakağın başa göre konumu (metre): sağa, yukarı, öne.")]
        [SerializeField] private Vector3 templeOffset = new Vector3(0.12f, 0.03f, 0.02f);
        [Tooltip("Silahı doldururken tuttuğu yer, başa göre (metre): sağa, aşağı, öne.")]
        [SerializeField] private Vector3 chestOffset = new Vector3(0.1f, -0.35f, 0.35f);
        [Tooltip("Silah modelinin namlusu doğru yöne bakmıyorsa buradan döndür.")]
        [SerializeField] private Vector3 gunRotationOffset = Vector3.zero;
        [SerializeField] private float reachTime = 0.6f;
        [SerializeField] private float raiseTime = 0.8f;
        [SerializeField] private float holdTime = 0.7f;

        [Header("Ölüm efekti")]
        [Tooltip("Kapalıysa kafa patlaması yerine Baron duman gibi kaybolur.")]
        [SerializeField] private bool goreEffect = true;
        [SerializeField] private Color burstColorA = new Color(0.45f, 0.02f, 0.02f);
        [SerializeField] private Color burstColorB = new Color(0.2f, 0f, 0f);
        [SerializeField] private Material burstMaterial;

        private Animator animator;
        private Transform head;
        private Transform rightHand;
        private Transform lookTarget;
        private Revolver revolver;
        private Vector3 gunHomePosition;
        private Quaternion gunHomeRotation;
        private Vector3 handTarget;
        private float handWeight;
        private Grip grip = Grip.None;
        private bool nervous;
        private bool dead;
        private Coroutine gesture;

        private string BaseState => nervous ? StateNervous : StateIdle;
        private bool Busy => grip != Grip.None || dead;
        private float Scale => transform.lossyScale.x;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (Camera.main != null) lookTarget = Camera.main.transform;

            if (head == null || rightHand == null)
                Debug.LogError("[BaronBody] Model Humanoid değil ya da Avatar atanmamış.");

            if (gun != null)
            {
                gunHomePosition = gun.position;
                gunHomeRotation = gun.rotation;
                gun.TryGetComponent(out revolver);
            }
        }

        // ---------------------------------------------------------------- Tepkiler

        public void React(PlayerIntent intent)
        {
            switch (intent)
            {
                case PlayerIntent.Insult:
                    PlayGesture(StateAngry, reactionDuration);
                    break;
                case PlayerIntent.Beg:
                case PlayerIntent.Irrelevant:
                    PlayGesture(StateLaugh, reactionDuration);
                    break;
                case PlayerIntent.Bluff:
                    PlayGesture(StateNervous, reactionDuration);
                    break;
                default:
                    PlayGesture(StateTalk, talkDuration);
                    break;
            }
        }

        public void SetNervous(bool value)
        {
            if (nervous == value) return;
            nervous = value;
            if (gesture == null && !Busy) Play(BaseState);
        }

        private void PlayGesture(string state, float duration)
        {
            if (Busy) return;
            if (gesture != null) StopCoroutine(gesture);
            gesture = StartCoroutine(GestureRoutine(state, duration));
        }

        private IEnumerator GestureRoutine(string state, float duration)
        {
            Play(state);
            yield return new WaitForSeconds(duration);
            gesture = null;
            Play(BaseState);
        }

        private void StopGesture()
        {
            if (gesture != null) StopCoroutine(gesture);
            gesture = null;
            Play(BaseState);
        }

        private void Play(string state)
        {
            if (!animator.HasState(0, Animator.StringToHash(state)))
            {
                Debug.LogWarning($"[BaronBody] Animator'da '{state}' adlı durum yok.");
                return;
            }
            animator.CrossFadeInFixedTime(state, crossFade);
        }

        // ---------------------------------------------------------------- Silah

        public IEnumerator LoadGun()
        {
            if (gun == null || bullet == null || head == null || dead) yield break;
            StopGesture();

            yield return MoveHand(() => bullet.position, 1f, reachTime);   // mermiye uzan
            grip = Grip.Bullet;                                            // mermiyi al
            yield return MoveHand(() => gun.position, 1f, raiseTime);      // silaha götür
            bullet.gameObject.SetActive(false);                            // mermi silaha girdi
            grip = Grip.GunUpright;                                        // silahı kavra
            yield return MoveHand(ChestPosition, 1f, raiseTime);           // göğsüne kaldır

            if (revolver != null) yield return revolver.SpinRoutine();     // silindiri çevir
            else yield return new WaitForSeconds(1f);

            yield return PutGunBack();
        }

        public IEnumerator RaiseGunToTemple()
        {
            if (gun == null || head == null || dead) yield break;
            StopGesture();

            yield return MoveHand(() => gun.position, 1f, reachTime);   // silaha uzan
            grip = Grip.GunAtTemple;                                    // kavra
            yield return MoveHand(TemplePosition, 1f, raiseTime);       // şakağa kaldır
            yield return new WaitForSeconds(holdTime);                  // gerilim
        }

        public IEnumerator LowerGun()
        {
            if (grip == Grip.None) yield break;
            yield return PutGunBack();
        }

        private IEnumerator PutGunBack()
        {
            yield return MoveHand(() => gunHomePosition, 1f, raiseTime);
            grip = Grip.None;
            gun.SetPositionAndRotation(gunHomePosition, gunHomeRotation);
            yield return MoveHand(() => handTarget, 0f, reachTime);
        }

        public void Die()
        {
            dead = true;
            grip = Grip.None;
            handWeight = 0f;
            if (gesture != null) StopCoroutine(gesture);
            if (gun != null) gun.SetPositionAndRotation(gunHomePosition, gunHomeRotation);
            if (revolver != null) revolver.Fire();

            if (head != null) SpawnBurst(head.position);
            if (!goreEffect)
                foreach (Renderer part in GetComponentsInChildren<Renderer>()) part.enabled = false;
        }

        private void SpawnBurst(Vector3 position)
        {
            GameObject burstObject = new GameObject("Kafa_Patlamasi");
            burstObject.transform.position = position;
            ParticleSystem burst = burstObject.AddComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = burst.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.startColor = goreEffect
                ? new ParticleSystem.MinMaxGradient(burstColorA, burstColorB)
                : new ParticleSystem.MinMaxGradient(new Color(0.15f, 0.15f, 0.15f), new Color(0.35f, 0.35f, 0.35f));

            ParticleSystem.EmissionModule emission = burst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 160) });

            ParticleSystem.ShapeModule shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            ParticleSystem.CollisionModule collision = burst.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.dampen = 0.8f;
            collision.bounce = 0.1f;

            ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            Material material = burstMaterial;
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null) material = new Material(shader);
            }
            if (material != null) particleRenderer.material = material;

            burst.Play();
            Destroy(burstObject, 10f);
        }

        // ---------------------------------------------------------------- Konumlar

        private Vector3 TemplePosition() => FromHead(templeOffset);
        private Vector3 ChestPosition() => FromHead(chestOffset);

        private Vector3 FromHead(Vector3 offset)
        {
            return head.position
                   + transform.right * (offset.x * Scale)
                   + transform.up * (offset.y * Scale)
                   + transform.forward * (offset.z * Scale);
        }

        private IEnumerator MoveHand(Func<Vector3> target, float toWeight, float duration)
        {
            Vector3 start = handWeight > 0.01f ? handTarget : rightHand.position;
            float startWeight = handWeight;

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (dead) yield break;
                float e = Mathf.SmoothStep(0f, 1f, t);
                handTarget = Vector3.Lerp(start, target(), e);
                handWeight = Mathf.Lerp(startWeight, toWeight, e);
                yield return null;
            }
            handTarget = target();
            handWeight = toWeight;
        }

        // ---------------------------------------------------------------- IK ve her kare

        private void OnAnimatorIK(int layerIndex)
        {
            if (lookTarget != null && !dead)
            {
                animator.SetLookAtWeight(lookAtWeight, 0.2f, 0.8f, 1f, 0.6f);
                animator.SetLookAtPosition(lookTarget.position);
            }

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, handTarget);
        }

        private void LateUpdate()
        {
            if (dead)
            {
                if (goreEffect && head != null) head.localScale = Vector3.one * 0.001f;
                return;
            }

            switch (grip)
            {
                case Grip.Bullet:
                    bullet.position = rightHand.position;
                    break;

                case Grip.GunUpright:
                    // Doldururken namlu yukarı bakar.
                    gun.position = rightHand.position;
                    gun.rotation = Quaternion.LookRotation(transform.up, -transform.forward) *
                                   Quaternion.Euler(gunRotationOffset);
                    break;

                case Grip.GunAtTemple:
                    // Şakaktayken namlu başa bakar.
                    gun.position = rightHand.position;
                    Vector3 toHead = head.position - gun.position;
                    if (toHead.sqrMagnitude > 0.0001f)
                        gun.rotation = Quaternion.LookRotation(toHead) * Quaternion.Euler(gunRotationOffset);
                    break;
            }
        }
    }
}
