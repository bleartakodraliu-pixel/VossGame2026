using System.Collections.Generic;
using UnityEngine;

namespace Voss.Atmosphere
{
    /// <summary>
    /// A figure that is never seen moving. When the player has not looked at it for a while (and is
    /// not standing close), it is simply somewhere else, facing them. Moves a limited number of times.
    /// </summary>
    public class ShiftWhenUnseen : MonoBehaviour
    {
        [SerializeField] private List<Vector3> poses = new List<Vector3>();
        [SerializeField] private float unseenSeconds = 6f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float viewAngle = 55f;
        [SerializeField] private int maxMoves = 3;

        private int index;
        private int moves;
        private float unseen;

        /// <summary>Poses are positions in the parent's space; the first should be the starting one.</summary>
        public void Configure(List<Vector3> localPoses)
        {
            poses = localPoses;
        }

        private void Update()
        {
            var cam = Camera.main;
            if (cam == null || poses.Count < 2 || moves >= maxMoves) return;

            Vector3 to = transform.position + Vector3.up * 1.2f - cam.transform.position;
            bool watched = Vector3.Angle(cam.transform.forward, to) < viewAngle && to.magnitude < 30f;
            if (watched || to.magnitude < minDistance)
            {
                unseen = 0f;
                return;
            }

            unseen += Time.deltaTime;
            if (unseen < unseenSeconds) return;

            // Only move to a spot that is also out of view and not next to the player.
            for (int i = 1; i < poses.Count; i++)
            {
                int candidate = (index + i) % poses.Count;
                Vector3 world = transform.parent != null ? transform.parent.TransformPoint(poses[candidate]) : poses[candidate];
                Vector3 toCandidate = world + Vector3.up * 1.2f - cam.transform.position;
                if (Vector3.Angle(cam.transform.forward, toCandidate) < viewAngle + 10f || toCandidate.magnitude < minDistance) continue;

                index = candidate;
                transform.position = world;
                Vector3 face = cam.transform.position - world;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
                moves++;
                break;
            }
            unseen = 0f;
        }
    }
}
