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
    /// The black envelope on the table, the first thing that matters. It catches the eye without
    /// markers: a warm pool of light, a seal that glints when you are near, a soft glow while you look
    /// at it. "Press E to inspect": the view moves in, the seal breaks, the flap opens, the letter and
    /// ticket 7314 slide out, and the letter can be read until the player takes it. Happens once.
    /// </summary>
    public class EnvelopeInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private IntroductionManager manager;
        [SerializeField] private Transform flap;
        [SerializeField] private Transform letter;
        [SerializeField] private Transform ticket;
        [SerializeField] private Renderer body;
        [SerializeField] private Renderer seal;
        [SerializeField] private Light glow;

        private bool busy;
        private bool done;
        private Material bodyMat;
        private Material sealMat;
        private PlayerInteractor interactor;
        private Vector3 ticketRestPos;
        private Quaternion ticketRestRot;

        public void Configure(IntroductionManager introduction, Transform flapPivot, Transform letterSheet, Transform ticketCard,
            Renderer bodyRenderer, Renderer sealRenderer, Light glowLight)
        {
            manager = introduction;
            flap = flapPivot;
            letter = letterSheet;
            ticket = ticketCard;
            body = bodyRenderer;
            seal = sealRenderer;
            glow = glowLight;
        }

        private void Start()
        {
            interactor = FindAnyObjectByType<PlayerInteractor>();
            if (body != null) { bodyMat = body.material; bodyMat.EnableKeyword("_EMISSION"); }
            if (seal != null) { sealMat = seal.material; sealMat.EnableKeyword("_EMISSION"); }
            if (glow != null) glow.intensity = 0f;
            if (letter != null) letter.gameObject.SetActive(false);
            if (ticket != null)
            {
                // The ticket waits inside the envelope; it slides out to where it lies on the menu table.
                ticketRestPos = ticket.position;
                ticketRestRot = ticket.rotation;
                ticket.position = transform.position + Vector3.up * 0.004f;
                ticket.rotation = transform.rotation;
                ticket.gameObject.SetActive(false);
            }
            var gm = GameManager.Instance;
            if (gm != null && gm.Evidence.Has(EvidenceIds.Letter)) RestoreTaken();
        }

        public string GetInteractionPrompt() => done || busy ? string.Empty : "Press E to inspect";
        public bool CanInteract() => !done && !busy;

        public void Interact(PlayerInteractor player)
        {
            if (done || busy) return;
            StartCoroutine(InspectRoutine(player));
        }

        private void Update()
        {
            if (done) { SetGlow(0f, 0f); return; }
            bool looked = interactor != null && ReferenceEquals(interactor.CurrentTarget, this);
            float near = 0f;
            var cam = Camera.main;
            if (cam != null)
            {
                float d = Vector3.Distance(cam.transform.position, transform.position);
                near = Mathf.Clamp01((3.5f - d) / 1.5f);
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f);
            SetGlow(looked ? 1f : 0f, near * pulse);
        }

        private float glowLevel;

        private void SetGlow(float looked, float sealGlint)
        {
            glowLevel = Mathf.MoveTowards(glowLevel, looked, Time.deltaTime * 3f);
            if (bodyMat != null) bodyMat.SetColor("_EmissionColor", new Color(0.35f, 0.22f, 0.1f) * glowLevel * 0.35f);
            if (sealMat != null) sealMat.SetColor("_EmissionColor", new Color(0.8f, 0.08f, 0.05f) * (0.15f + 0.6f * sealGlint + 0.6f * glowLevel));
            if (glow != null) glow.intensity = glowLevel * 0.9f;
        }

        private IEnumerator InspectRoutine(PlayerInteractor player)
        {
            busy = true;
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            var fps = player != null ? player.Controller : null;
            if (fps != null) fps.SetControlLocked(true);
            var cam = player != null ? player.ViewCamera.transform : Camera.main.transform;
            Vector3 camLocalPos = cam.localPosition;
            Quaternion camLocalRot = cam.localRotation;

            // 1. Lean in over the envelope.
            Vector3 away = transform.position - cam.position;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;
            Vector3 closePos = transform.position + Vector3.up * 0.42f - away * 0.16f;
            Quaternion closeRot = Quaternion.LookRotation(transform.position + away * 0.03f - closePos, away);
            yield return MoveCamera(cam, cam.position, cam.rotation, closePos, closeRot, 0.9f);

            // 2. The seal breaks, the flap opens.
            gm?.Audio.Play(SfxId.Paper, 0.9f);
            if (flap != null)
            {
                Quaternion start = flap.localRotation, end = start * Quaternion.Euler(165f, 0f, 0f);
                for (float t = 0f; t < 0.7f; t += Time.deltaTime)
                {
                    flap.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / 0.7f));
                    yield return null;
                }
                flap.localRotation = end;
            }
            yield return new WaitForSeconds(0.2f);

            // 3. The letter and the ticket slide out.
            gm?.Audio.Play(SfxId.Paper, 0.7f);
            Vector3 letterStart = Vector3.zero, letterEnd = Vector3.zero, ticketStart = Vector3.zero;
            if (letter != null)
            {
                letter.gameObject.SetActive(true);
                letterStart = letter.localPosition;
                letterEnd = letterStart + new Vector3(0f, 0.01f, -0.17f);
            }
            if (ticket != null) { ticket.gameObject.SetActive(true); ticketStart = ticket.position; }
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.8f);
                if (letter != null) letter.localPosition = Vector3.Lerp(letterStart, letterEnd, k);
                if (ticket != null)
                {
                    ticket.position = Vector3.Lerp(ticketStart, ticketRestPos, k);
                    ticket.rotation = Quaternion.Slerp(transform.rotation, ticketRestRot, k);
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.4f);

            // 4. Read, for as long as you like.
            bool taken = false;
            if (ui != null) ui.Letter.Open(EvidenceManager.LetterText, () => taken = true);
            else taken = true;
            while (!taken) yield return null;

            // 5. Into the pocket: the letter and the ticket go to the evidence.
            gm?.Evidence.Discover(EvidenceIds.Ticket);
            gm?.Evidence.Discover(EvidenceIds.Letter);
            if (letter != null) letter.gameObject.SetActive(false);
            if (ticket != null) ticket.gameObject.SetActive(false);
            gm?.Subtitles.Show("Added to evidence. (TAB to review)", 3f);

            Vector3 backPos = cam.parent != null ? cam.parent.TransformPoint(camLocalPos) : camLocalPos;
            Quaternion backRot = cam.parent != null ? cam.parent.rotation * camLocalRot : camLocalRot;
            yield return MoveCamera(cam, cam.position, cam.rotation, backPos, backRot, 0.7f);
            cam.localPosition = camLocalPos;
            cam.localRotation = camLocalRot;
            if (fps != null) fps.SetControlLocked(false);

            done = true;
            busy = false;
            manager?.OnLetterCollected();
        }

        private static IEnumerator MoveCamera(Transform cam, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                cam.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k));
                yield return null;
            }
            cam.SetPositionAndRotation(toPos, toRot);
        }

        /// <summary>Scene reloaded after the letter was taken: open, empty envelope.</summary>
        private void RestoreTaken()
        {
            done = true;
            if (flap != null) flap.localRotation *= Quaternion.Euler(165f, 0f, 0f);
            if (letter != null) letter.gameObject.SetActive(false);
            if (ticket != null) ticket.gameObject.SetActive(false);
        }
    }
}
