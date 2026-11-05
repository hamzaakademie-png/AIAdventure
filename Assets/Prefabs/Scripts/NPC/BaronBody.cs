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
    /// - Oyun başında IK ile mermiyi alıp silaha koyar.
    /// - Kendi sırası gelince "SilahDaya" animasyonunu oynatır: silahı masadan alır,
    ///   şakağına dayar, masaya geri koyar. Silah, animasyonun belirli anları arasında
    ///   sağ ele yapışık kalır.
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

        private enum Grip { None, Bullet, GunLoading, GunInHand }

        [Header("Animasyon")]
        [SerializeField] private float crossFade = 0.25f;
        [SerializeField] private float talkDuration = 3f;
        [SerializeField] private float reactionDuration = 2.5f;

        [Header("Bakış")]
        [SerializeField, Range(0f, 1f)] private float lookAtWeight = 0.7f;

        [Header("Silah ve mermi")]
        [SerializeField] private Transform gun;
        [SerializeField] private Transform bullet;
        [Tooltip("Silahı doldururken tuttuğu yer, başa göre (metre): sağa, aşağı, öne.")]
        [SerializeField] private Vector3 chestOffset = new Vector3(0.1f, -0.35f, 0.35f);
        [SerializeField] private float reachTime = 0.6f;
        [SerializeField] private float raiseTime = 0.8f;

        [Header("Silahı şakağa dayama animasyonu")]
        [Tooltip("Animator'da bu animasyonun bulunduğu katman (Base Layer = 0).")]
        [SerializeField] private int gunLayer = 1;
        [SerializeField] private string gunState = "SilahDaya";
        [Tooltip("Animasyonun hangi anında silah ele alınır (0 = başı, 1 = sonu).")]
        [SerializeField, Range(0f, 1f)] private float grabTime = 0.25f;
        [Tooltip("Silah şakaktayken tetiğin çekildiği an.")]
        [SerializeField, Range(0f, 1f)] private float triggerTime = 0.55f;
        [Tooltip("Silahın masaya bırakıldığı an.")]
        [SerializeField, Range(0f, 1f)] private float releaseTime = 0.85f;

        [Header("Silahın eldeki duruşu")]
        [Tooltip("Silahın sağ ele göre konumu (metre, elin kendi ekseninde).")]
        [SerializeField] private Vector3 gripPosition = Vector3.zero;
        [Tooltip("Silahın sağ ele göre açısı (derece).")]
        [SerializeField] private Vector3 gripRotation = Vector3.zero;
        [Tooltip("AYAR İÇİN: Play modunda açınca silah sürekli elde durur, değerleri canlı ayarlayabilirsin.")]
        [SerializeField] private bool gripPreview;

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
        private bool gunAnimationPlaying;
        private Coroutine gesture;

        private string BaseState => nervous ? StateNervous : StateIdle;
        private bool Busy => grip != Grip.None || gunAnimationPlaying || dead;
        private float Scale => transform.lossyScale.x;
        private bool HasGunLayer => gunLayer > 0 && gunLayer < animator.layerCount;

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

            if (HasGunLayer) animator.SetLayerWeight(gunLayer, 0f);
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

        // ---------------------------------------------------------------- Silahı doldurma (oyun başı, IK)

        public IEnumerator LoadGun()
        {
            if (gun == null || bullet == null || head == null || dead) yield break;
            StopGesture();

            yield return MoveHand(() => bullet.position, 1f, reachTime);     // mermiye uzan
            grip = Grip.Bullet;                                              // mermiyi al
            yield return MoveHand(() => gunHomePosition, 1f, raiseTime);     // silaha götür
            bullet.gameObject.SetActive(false);                              // mermi silaha girdi
            grip = Grip.GunLoading;                                          // silahı kavra
            yield return MoveHand(ChestPosition, 1f, raiseTime);             // göğsüne kaldır

            if (revolver != null) yield return revolver.SpinRoutine();       // silindiri çevir
            else yield return new WaitForSeconds(1f);

            yield return MoveHand(() => gunHomePosition, 1f, raiseTime);     // masaya indir
            grip = Grip.None;
            ResetGun();
            yield return MoveHand(() => handTarget, 0f, reachTime);          // eli çek
        }

        // ---------------------------------------------------------------- Silahı şakağa dayama (animasyon)

        /// <summary>
        /// Animasyonu başlatır; silah şakaktayken (triggerTime) geri döner.
        /// Bu sırada masa tetiği çeker ya da Baron'un eli titrer.
        /// </summary>
        public IEnumerator RaiseGunToTemple()
        {
            if (gun == null || dead) yield break;
            StopGesture();

            if (!HasGunLayer || !animator.HasState(gunLayer, Animator.StringToHash(gunState)))
            {
                Debug.LogWarning($"[BaronBody] '{gunState}' animasyonu bulunamadı (katman {gunLayer}).");
                yield return new WaitForSeconds(1.5f);
                yield break;
            }

            gunAnimationPlaying = true;
            animator.Play(gunState, gunLayer, 0f);
            yield return FadeLayer(1f, 0.25f);

            yield return WaitUntilTime(grabTime);
            grip = Grip.GunInHand;                       // silah ele yapışır

            yield return WaitUntilTime(triggerTime);     // silah şakakta
        }

        /// <summary>Animasyonun geri kalanını oynatır: silah masaya bırakılır.</summary>
        public IEnumerator LowerGun()
        {
            if (!gunAnimationPlaying) yield break;

            yield return WaitUntilTime(releaseTime);
            grip = Grip.None;
            ResetGun();                                  // silah masadaki yerine

            yield return WaitUntilTime(0.98f);
            yield return FadeLayer(0f, 0.3f);
            gunAnimationPlaying = false;
        }

        private IEnumerator WaitUntilTime(float normalizedTime)
        {
            while (!dead && HasGunLayer)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(gunLayer);
                if (!info.IsName(gunState) || info.normalizedTime >= normalizedTime) yield break;
                yield return null;
            }
        }

        private IEnumerator FadeLayer(float to, float duration)
        {
            if (!HasGunLayer) yield break;
            float from = animator.GetLayerWeight(gunLayer);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (dead) yield break;
                animator.SetLayerWeight(gunLayer, Mathf.Lerp(from, to, t));
                yield return null;
            }
            animator.SetLayerWeight(gunLayer, to);
        }

        private void ResetGun()
        {
            if (gun != null) gun.SetPositionAndRotation(gunHomePosition, gunHomeRotation);
        }

        // ---------------------------------------------------------------- Ölüm

        public void Die()
        {
            dead = true;
            grip = Grip.None;
            handWeight = 0f;
            gunAnimationPlaying = false;
            if (HasGunLayer) animator.SetLayerWeight(gunLayer, 0f);
            if (gesture != null) StopCoroutine(gesture);
            ResetGun();
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

        // ---------------------------------------------------------------- Konumlar ve IK

        private Vector3 ChestPosition()
        {
            return head.position
                   + transform.right * (chestOffset.x * Scale)
                   + transform.up * (chestOffset.y * Scale)
                   + transform.forward * (chestOffset.z * Scale);
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

                case Grip.GunLoading:
                    // Doldururken namlu yukarı bakar.
                    gun.position = rightHand.position;
                    gun.rotation = Quaternion.LookRotation(transform.up, -transform.forward);
                    break;

                case Grip.GunInHand:
                    PlaceGunInHand();
                    break;

                default:
                    if (gripPreview && gun != null) PlaceGunInHand();
                    break;
            }
        }

        /// <summary>
        /// AYAR ARACI: Silahı Scene'de elle Baron'un avucuna yerleştir, sonra BaronBody'nin
        /// ⋮ menüsünden bunu seç. Silahın ele göre konumu ve açısı hesaplanıp kaydedilir
        /// ve Console'a yazılır (Play modundaysan oyunu durdurunca bu sayıları elle gir).
        /// </summary>
        [ContextMenu("Silahın El Konumunu Kaydet")]
        private void CaptureGripFromCurrentGun()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (rightHand == null) rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (gun == null || rightHand == null)
            {
                Debug.LogError("[BaronBody] Gun alanı boş ya da sağ el bulunamadı.");
                return;
            }

            Quaternion inverse = Quaternion.Inverse(rightHand.rotation);
            gripPosition = inverse * (gun.position - rightHand.position);
            gripRotation = (inverse * gun.rotation).eulerAngles;
            gripPreview = false;

            Debug.Log($"[BaronBody] KAYDEDİLDİ  Grip Position: {gripPosition.x:F3}, {gripPosition.y:F3}, {gripPosition.z:F3}" +
                      $"   Grip Rotation: {gripRotation.x:F1}, {gripRotation.y:F1}, {gripRotation.z:F1}");
        }

        /// <summary>Silahı sağ elin içine, ayarlanan konum ve açıyla yerleştirir.</summary>
        private void PlaceGunInHand()
        {
            gun.SetPositionAndRotation(rightHand.position + rightHand.rotation * gripPosition,
                rightHand.rotation * Quaternion.Euler(gripRotation));
        }
    }
}
