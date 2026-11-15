using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Player;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Scene 01. A prologue over black tells who Voss was and what happened at his last show. Then an
    /// envelope on a dark table: the player opens it, examines the ticket (7314 on the front, Voss's
    /// message on the back) and follows the address to the laboratory. The number is shown, never explained.
    /// </summary>
    public class InvitationSequence : MonoBehaviour
    {
        [SerializeField] private Transform envelopeLid;
        [SerializeField] private Transform ticket;
        [SerializeField] private RelayInteractable envelopeInteract;
        [SerializeField] private RelayInteractable ticketInteract;
        [SerializeField] private PlayerInteractor player;
        [SerializeField] private float lidOpenAngle = 150f;
        [SerializeField] private float ticketRise = 0.08f;

        private bool opened;
        private bool examined;
        private Vector3 ticketStart;

        /// <summary>Once per session: a restart after failing goes straight to the table.</summary>
        private static bool prologueSeen;

        public static readonly string[] Prologue =
        {
            "Twenty years ago, the illusionist E. Voss gave his last performance.",
            "It was called THE IMPOSSIBLE EXIT.",
            "In front of four hundred people, he stepped into a sealed glass chamber.",
            "The lights went out for seven seconds.",
            "When they came back on, the chamber was empty.",
            "The police searched the stage for weeks. They found nothing.",
            "Since then, a few people from that audience have received a black envelope.",
            "None of them ever explained what they found.",
            "Tonight, one arrived for you."
        };

        public static readonly string[] ToLaboratory =
        {
            "The address on the envelope led to the old theatre.",
            "Beneath the stage, an elevator was waiting."
        };

        public void Configure(Transform lid, Transform ticketTransform, RelayInteractable envelope, RelayInteractable ticketRelay, PlayerInteractor interactor)
        {
            envelopeLid = lid;
            ticket = ticketTransform;
            envelopeInteract = envelope;
            ticketInteract = ticketRelay;
            player = interactor;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui != null) { ui.EvidenceEnabled = false; ui.SetCursorFree(false); }
            gm?.State.SetStage(GameStage.Introduction);
            gm?.Audio.PlayMusic(MusicId.Drone, 4f);

            if (ticket != null) { ticketStart = ticket.localPosition; ticket.gameObject.SetActive(false); }
            if (envelopeInteract != null)
            {
                envelopeInteract.Prompt = "Open envelope";
                envelopeInteract.OnInteract.AddListener(OpenEnvelope);
            }
            if (ticketInteract != null)
            {
                ticketInteract.Prompt = "Examine ticket";
                ticketInteract.Interactable = false;
                ticketInteract.OnInteract.AddListener(ExamineTicket);
            }
            if (player != null && player.Controller != null) player.Controller.MovementEnabled = false;

            if (!prologueSeen && ui != null) StartCoroutine(PrologueRoutine());
        }

        private IEnumerator PrologueRoutine()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (envelopeInteract != null) envelopeInteract.Interactable = false;
            ui.Story.Open(StoryCards.SkipHint);
            while (gm != null && gm.Transitions.IsTransitioning) yield return null; // let the scene fade-in finish
            yield return new WaitForSeconds(0.8f);

            yield return StoryCards.Play(Prologue, true, i =>
            {
                if (gm == null) return;
                if (i == 0) gm.Audio.Play(SfxId.Projector, 0.6f);
                else if (i == 2) gm.Audio.Play(SfxId.Applause, 0.35f);
                else if (i == 3) gm.Audio.Play(SfxId.PowerDown, 0.7f);
                else if (i == 8) gm.Audio.Play(SfxId.Knock, 0.9f);
            });
            prologueSeen = true;

            // Hide the switch to the table behind the fade.
            yield return ui.FadeTo(1f, 0.6f);
            ui.Story.Close();
            yield return new WaitForSeconds(0.4f);
            yield return ui.FadeTo(0f, 1.6f);
            if (envelopeInteract != null) envelopeInteract.Interactable = true;
        }

        private void OpenEnvelope()
        {
            if (opened) return;
            opened = true;
            if (envelopeInteract != null) envelopeInteract.Interactable = false;
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Paper, 0.9f);
            if (envelopeLid != null)
            {
                Quaternion start = envelopeLid.localRotation;
                Quaternion end = Quaternion.Euler(lidOpenAngle, 0f, 0f);
                float t = 0f;
                while (t < 1.1f)
                {
                    t += Time.deltaTime;
                    envelopeLid.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / 1.1f));
                    yield return null;
                }
                envelopeLid.localRotation = end;
            }
            yield return new WaitForSeconds(0.3f);
            if (ticket != null)
            {
                ticket.gameObject.SetActive(true);
                Vector3 end = ticketStart + Vector3.up * ticketRise;
                float t = 0f;
                while (t < 0.9f)
                {
                    t += Time.deltaTime;
                    ticket.localPosition = Vector3.Lerp(ticketStart, end, Mathf.SmoothStep(0f, 1f, t / 0.9f));
                    yield return null;
                }
                ticket.localPosition = end;
            }
            gm?.Audio.Play(SfxId.Paper, 0.6f);
            gm?.Evidence.Discover(EvidenceIds.Ticket);
            if (ticketInteract != null) ticketInteract.Interactable = true;

            // Somebody knocks, behind you. There is nobody else in the house.
            yield return new WaitForSeconds(1.2f);
            if (player != null) gm?.Audio.PlayAt(SfxId.Knock, player.transform.position - player.transform.forward * 2.5f + Vector3.up * 1.4f, 1f);
        }

        private void ExamineTicket()
        {
            if (examined || player == null || player.Inspection == null) return;
            examined = true;
            if (ticketInteract != null) ticketInteract.Interactable = false;
            GameManager.Instance?.Audio.Play(SfxId.Paper, 0.8f);
            player.Inspection.BeginInspection(ticket, 1.1f, OnTicketExamined);
        }

        private void OnTicketExamined()
        {
            StartCoroutine(LeaveForLaboratory());
        }

        private IEnumerator LeaveForLaboratory()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (player != null && player.Controller != null) player.Controller.SetControlLocked(true);
            yield return new WaitForSeconds(0.8f);
            gm?.Audio.StopMusic(1.5f);

            if (ui != null)
            {
                yield return ui.FadeTo(1f, 1.0f);
                ui.Story.Open(null);
                yield return ui.FadeTo(0f, 0.3f);
                yield return StoryCards.Play(ToLaboratory, false, i => { if (i == 1) gm?.Audio.Play(SfxId.Elevator, 0.8f); });
                yield return ui.FadeTo(1f, 0.4f);
                ui.Story.Close();
            }
            gm?.Transitions.LoadScene(SceneNames.Laboratory, 1.0f);
        }
    }
}
