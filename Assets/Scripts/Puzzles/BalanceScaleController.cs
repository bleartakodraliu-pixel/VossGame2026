using System.Collections;
using UnityEngine;

namespace Voss.Puzzles
{
    /// <summary>Animates the antique balance beam: idle tilt, then settles level when the puzzle is solved.</summary>
    public class BalanceScaleController : MonoBehaviour
    {
        [SerializeField] private Transform beam;
        [SerializeField] private float idleTilt = 9f;
        [SerializeField] private float settleSeconds = 2.4f;

        private bool settled;
        private float idlePhase;

        public void Configure(Transform beamTransform) { beam = beamTransform; }

        public void SetSettled(bool value)
        {
            settled = value;
            if (beam != null && value) beam.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (settled || beam == null) return;
            idlePhase += Time.deltaTime * 0.35f;
            float z = idleTilt + Mathf.Sin(idlePhase) * 1.5f;
            beam.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        public Coroutine Settle()
        {
            return StartCoroutine(SettleRoutine());
        }

        private IEnumerator SettleRoutine()
        {
            settled = true;
            if (beam == null) yield break;
            Quaternion start = beam.localRotation;
            float t = 0f;
            while (t < settleSeconds)
            {
                t += Time.deltaTime;
                float k = t / settleSeconds;
                // Overshoot a little, like a real balance coming to rest.
                float wobble = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * 4f;
                beam.localRotation = Quaternion.Slerp(start, Quaternion.identity, Mathf.SmoothStep(0f, 1f, k)) * Quaternion.Euler(0f, 0f, wobble);
                yield return null;
            }
            beam.localRotation = Quaternion.identity;
        }
    }
}
