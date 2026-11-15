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
    /// Scene 01, after the vanishing: an envelope on a dark table. The player opens it, examines the
    /// ticket (7314 on the front, Voss's message on the back) and follows the address to the old theatre.
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

        public static readonly string[] ToTheatre =
        {
            "The address on the envelope led to the old Voss Theatre."
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
                yield return StoryCards.Play(ToTheatre, false);
                yield return ui.FadeTo(1f, 0.4f);
                ui.Story.Close();
            }
            gm?.Transitions.LoadScene(SceneNames.Arrival, 1.0f);
        }
    }
}
