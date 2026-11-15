using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;

namespace Voss.Narrative
{
    /// <summary>
    /// After PERCEPTION is solved, a silhouette waits at the far end of the corridor that leads on.
    /// It watches the player, turns away, the lamp above it fails and it is gone. Investigating finds
    /// nobody, only writing on the wall. Fires exactly once.
    /// </summary>
    public class VossSilhouetteEvent : MonoBehaviour
    {
        [SerializeField] private PlayerTriggerZone triggerZone;
        [SerializeField] private Transform silhouette;
        [SerializeField] private Transform walkStart;
        [SerializeField] private Transform walkEnd;
        [SerializeField] private GameObject wallWriting;
        [SerializeField] private float walkSeconds = 2.6f;
        [SerializeField] private float watchSeconds = 1.4f;
        [SerializeField] private Light flickerLight;

        public void Configure(PlayerTriggerZone zone, Transform figure, Transform start, Transform end, GameObject writing, Light lamp = null)
        {
            triggerZone = zone;
            silhouette = figure;
            walkStart = start;
            walkEnd = end;
            wallWriting = writing;
            flickerLight = lamp;
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
            var cam = Camera.main;
            if (cam != null) FaceTowards(cam.transform.position);
            silhouette.gameObject.SetActive(true);
            gm?.Audio.PlayAt(SfxId.Creak, silhouette.position, 0.8f);

            // It watches. Then it turns its back and walks away.
            yield return new WaitForSeconds(watchSeconds);
            FaceTowards(walkEnd.position);

            float t = 0f;
            float stepTimer = 0f;
            while (t < walkSeconds)
            {
                t += Time.deltaTime;
                silhouette.position = Vector3.Lerp(walkStart.position, walkEnd.position, t / walkSeconds);
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    stepTimer = 0.55f;
                    gm?.Audio.PlayAt(SfxId.Footstep, silhouette.position, 0.6f);
                }
                yield return null;
            }

            // The lamp fails; when it returns, nobody is there.
            if (flickerLight != null)
            {
                bool wasOn = flickerLight.enabled;
                for (int i = 0; i < 5; i++)
                {
                    flickerLight.enabled = i % 2 == 1;
                    yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
                }
                flickerLight.enabled = false;
                silhouette.gameObject.SetActive(false);
                if (wallWriting != null) wallWriting.SetActive(true); // there when the light returns
                yield return new WaitForSeconds(0.6f);
                flickerLight.enabled = wasOn;
            }
            silhouette.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.5f);
            if (wallWriting != null) wallWriting.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            gm?.Audio.PlayMusic(MusicId.Drone, 5f);
        }

        private void FaceTowards(Vector3 point)
        {
            var flat = new Vector3(point.x, silhouette.position.y, point.z);
            if ((flat - silhouette.position).sqrMagnitude > 0.0001f) silhouette.LookAt(flat);
        }
    }
}
