using System.Collections;
using UnityEngine;

namespace AIAdventure.Blackjack
{
    /// <summary>
    /// Masadaki 3D kart: desteden yerine uçarak gelir, gerekirse ters döner.
    /// </summary>
    public class CardVisual : MonoBehaviour
    {
        private const float Width = 0.26f;
        private const float Length = 0.36f;
        private const float Thickness = 0.004f;

        [SerializeField] private float dealDuration = 0.3f;
        [SerializeField] private float flipDuration = 0.35f;
        [SerializeField] private float arcHeight = 0.15f;

        private bool busy;
        private bool flipQueued;

        public bool IsFaceDown { get; private set; }

        public static CardVisual Create(Card card, Transform parent, Font font)
        {
            GameObject root = new GameObject($"Kart_{card}");
            root.transform.SetParent(parent, false);

            CreateLayer(root.transform, "Yuz", Vector3.zero,
                new Vector3(Width, Thickness, Length), new Color(0.97f, 0.95f, 0.90f));
            CreateLayer(root.transform, "Arka", new Vector3(0f, -Thickness * 0.6f, 0f),
                new Vector3(Width + 0.002f, 0.001f, Length + 0.002f), new Color(0.45f, 0.08f, 0.10f));

            Color ink = card.IsRed ? new Color(0.75f, 0.1f, 0.1f) : new Color(0.08f, 0.08f, 0.08f);
            float top = Thickness * 0.5f + 0.0005f;
            CreateText(root.transform, card.RankText, new Vector3(-0.085f, top, 0.125f), 0.006f, ink, font);
            CreateText(root.transform, card.SuitSymbol, new Vector3(0f, top, -0.01f), 0.014f, ink, font);

            return root.AddComponent<CardVisual>();
        }

        /// <summary>Masaya yatık duran, yukarıdan okunan 3D yazı.</summary>
        public static TextMesh CreateText(Transform parent, string text, Vector3 localPosition,
            float size, Color color, Font font, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            GameObject go = new GameObject("Yazi");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.text = text;
            mesh.fontSize = 100;
            mesh.characterSize = size;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return mesh;
        }

        private static void CreateLayer(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = name;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = position;
            layer.transform.localScale = scale;
            Destroy(layer.GetComponent<Collider>());
            layer.GetComponent<Renderer>().material.color = color;
        }

        public void Deal(Vector3 from, Vector3 to, Quaternion restRotation, float delay, bool faceDown)
        {
            StartCoroutine(DealRoutine(from, to, restRotation, delay, faceDown));
        }

        public void Flip()
        {
            if (busy) flipQueued = true;
            else StartCoroutine(FlipRoutine());
        }

        private IEnumerator DealRoutine(Vector3 from, Vector3 to, Quaternion restRotation, float delay, bool faceDown)
        {
            busy = true;
            IsFaceDown = faceDown;
            transform.SetPositionAndRotation(from, faceDown ? restRotation * Quaternion.Euler(0f, 0f, 180f) : restRotation);

            if (delay > 0f) yield return new WaitForSeconds(delay);

            for (float t = 0f; t < 1f; t += Time.deltaTime / dealDuration)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.Lerp(from, to, e);
                position.y += Mathf.Sin(e * Mathf.PI) * arcHeight;
                transform.position = position;
                yield return null;
            }
            transform.position = to;
            busy = false;

            if (flipQueued)
            {
                flipQueued = false;
                StartCoroutine(FlipRoutine());
            }
        }

        private IEnumerator FlipRoutine()
        {
            busy = true;
            IsFaceDown = !IsFaceDown;
            Quaternion start = transform.rotation;
            Quaternion end = start * Quaternion.Euler(0f, 0f, 180f);
            Vector3 basePosition = transform.position;

            for (float t = 0f; t < 1f; t += Time.deltaTime / flipDuration)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                transform.rotation = Quaternion.Slerp(start, end, e);
                transform.position = basePosition + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.08f;
                yield return null;
            }
            transform.SetPositionAndRotation(basePosition, end);
            busy = false;
        }
    }
}
