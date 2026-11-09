using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;

namespace Voss.Narrative
{
    /// <summary>
    /// After PERCEPTION is solved and the player steps back into the hall, a silhouette walks
    /// around the far corner. Investigating finds nobody, only writing on the wall. Fires exactly once.
    /// </summary>
    public class VossSilhouetteEvent : MonoBehaviour
    {
        [SerializeField] private PlayerTriggerZone triggerZone;
        [SerializeField] private Transform silhouette;
        [SerializeField] private Transform walkStart;
        [SerializeField] private Transform walkEnd;
        [SerializeField] private GameObject wallWriting;
        [SerializeField] private float walkSeconds = 2.6f;

        public void Configure(PlayerTriggerZone zone, Transform figure, Transform start, Transform end, GameObject writing)
        {
            triggerZone = zone;
            silhouette = figure;
            walkStart = start;
            walkEnd = end;
            wallWriting = writing;
        }

        private void Start()
        {
            if (silhouette != null) silhouette.gameObject.SetActive(false);
            if (wallWriting != null) wallWriting.SetActive(false);
            if (triggerZone != null)
            {
                triggerZone.Once = false; // we gate on game state ourselves so an early crossing does not consume it
                triggerZone.OnPlayerEnter.AddListener(TryPlay);
            }
            var gm = GameManager.Instance;
            if (gm != null && gm.State.SilhouetteEventPlayed && wallWriting != null) wallWriting.SetActive(true);
        }

        private void TryPlay()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (!gm.State.PerceptionSolved || gm.State.SilhouetteEventPlayed) return;
            gm.State.MarkSilhouetteEventPlayed();
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            var gm = GameManager.Instance;
            if (silhouette == null || walkStart == null || walkEnd == null) yield break;

            gm?.Audio.StopMusic(0.8f); // silence before the sighting
            yield return new WaitForSeconds(0.4f);

            silhouette.position = walkStart.position;
            silhouette.LookAt(new Vector3(walkEnd.position.x, silhouette.position.y, walkEnd.position.z));
            silhouette.gameObject.SetActive(true);

            float t = 0f;
            float stepTimer = 0f;
            while (t < walkSeconds)
            {
                t += Time.deltaTime;
                silhouette.position = Vector3.Lerp(walkStart.position, walkEnd.position, t / walkSeconds);
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    stepTimer = 0.5f;
                    gm?.Audio.PlayAt(SfxId.Footstep, silhouette.position, 0.5f);
                }
                yield return null;
            }
            silhouette.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.5f);
            if (wallWriting != null) wallWriting.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            gm?.Audio.PlayMusic(MusicId.Drone, 5f);
        }
    }
}
