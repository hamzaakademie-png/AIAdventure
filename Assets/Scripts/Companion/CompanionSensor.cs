using System.Collections.Generic;
using UnityEngine;

namespace AIAdventure.Companion
{
    /// <summary>
    /// Companion'ın önüne yelpaze şeklinde Raycast atarak engellere olan
    /// mesafeyi ve açıyı ölçer. Sahne görünümünde ışınlar çizilir:
    /// kırmızı = engele çarptı, yeşil = boş.
    /// </summary>
    public class CompanionSensor : MonoBehaviour
    {
        public struct Reading
        {
            public float Angle;
            public float Distance;
            public bool Hit;
        }

        [SerializeField] private int rayCount = 5;
        [SerializeField] private float fieldOfView = 90f;
        [SerializeField] private float range = 3f;
        [SerializeField] private float eyeHeight = 0.2f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private readonly List<Reading> readings = new List<Reading>();

        public IReadOnlyList<Reading> Readings => readings;
        public float ClosestDistance { get; private set; } = float.PositiveInfinity;
        public float ClosestAngle { get; private set; }

        public bool HasObstacleWithin(float distance) => ClosestDistance <= distance;

        private void Update()
        {
            Scan();
        }

        private void Scan()
        {
            readings.Clear();
            ClosestDistance = float.PositiveInfinity;
            ClosestAngle = 0f;

            Vector3 origin = transform.position + Vector3.up * eyeHeight;

            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount == 1 ? 0.5f : i / (float)(rayCount - 1);
                float angle = Mathf.Lerp(-fieldOfView / 2f, fieldOfView / 2f, t);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

                bool hit = Physics.Raycast(origin, direction, out RaycastHit info, range,
                    obstacleMask, QueryTriggerInteraction.Ignore);
                float distance = hit ? info.distance : range;

                readings.Add(new Reading { Angle = angle, Distance = distance, Hit = hit });

                if (hit && distance < ClosestDistance)
                {
                    ClosestDistance = distance;
                    ClosestAngle = angle;
                }
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            foreach (Reading r in readings)
            {
                Vector3 direction = Quaternion.Euler(0f, r.Angle, 0f) * transform.forward;
                Gizmos.color = r.Hit ? Color.red : Color.green;
                Gizmos.DrawLine(origin, origin + direction * r.Distance);
            }
        }
    }
}
