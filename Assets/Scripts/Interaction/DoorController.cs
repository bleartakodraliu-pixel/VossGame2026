using System;
using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Player;

namespace Voss.Interaction
{
    /// <summary>
    /// Sliding door gated by a progression flag. Can optionally seal itself behind the player
    /// until a second flag is met (rooms lock while their puzzle is unsolved).
    /// Opening is driven by a coroutine with a guaranteed end state, so a door can never
    /// get stuck half-open.
    /// </summary>
    public class DoorController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform panel;
        [SerializeField] private float openHeight = 2.6f;
        [SerializeField] private float openSeconds = 1.2f;
        [SerializeField] private GameFlag requiredFlag = GameFlag.None;
        [SerializeField] private string lockedMessage = "Locked.";
        [SerializeField] private bool sealBehindPlayer;
        [SerializeField] private GameFlag sealReleaseFlag = GameFlag.None;
        [SerializeField] private bool manualOnly;
        [SerializeField] private string doorLabel = "Door";

        private DoorState state = DoorState.Closed;
        private Vector3 closedPos;
        private Coroutine motion;
        private bool sealedByRoom;
        private bool manualUnlocked;

        public event Action<DoorController> Opened;
        public DoorState State => state;
        public string Label => doorLabel;

        public void Configure(Transform doorPanel, GameFlag required, string lockedText, bool sealBehind, GameFlag sealRelease, bool manual, string label)
        {
            panel = doorPanel;
            requiredFlag = required;
            lockedMessage = lockedText;
            sealBehindPlayer = sealBehind;
            sealReleaseFlag = sealRelease;
            manualOnly = manual;
            doorLabel = label;
        }

        private void Awake()
        {
            if (panel == null) panel = transform;
            closedPos = panel.localPosition;
        }

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.State.FlagsChanged += OnFlagsChanged;
            RefreshState();
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.State.FlagsChanged -= OnFlagsChanged;
        }

        private void OnFlagsChanged()
        {
            RefreshState();
        }

        private bool RequirementMet()
        {
            if (manualOnly) return manualUnlocked;
            var gm = GameManager.Instance;
            return gm == null || GameFlags.Check(gm.State, requiredFlag);
        }

        private bool SealReleased()
        {
            var gm = GameManager.Instance;
            return gm == null || GameFlags.Check(gm.State, sealReleaseFlag);
        }

        private void RefreshState()
        {
            if (state == DoorState.Open || state == DoorState.Opening) return;
            if (sealedByRoom && SealReleased())
            {
                sealedByRoom = false;
                state = DoorState.Closed;
                GameManager.Instance?.Audio.PlayAt(SfxId.MetalMechanism, transform.position, 0.6f);
                return;
            }
            state = (RequirementMet() && !sealedByRoom) ? DoorState.Closed : DoorState.Locked;
        }

        /// <summary>For doors opened by scripted events (memory door, final exit).</summary>
        public void UnlockManually()
        {
            manualUnlocked = true;
            RefreshState();
        }

        public string GetInteractionPrompt()
        {
            switch (state)
            {
                case DoorState.Locked: return "Locked";
                case DoorState.Closed: return "Open";
                default: return string.Empty;
            }
        }

        public bool CanInteract() => state == DoorState.Locked || state == DoorState.Closed;

        public void Interact(PlayerInteractor player)
        {
            if (state == DoorState.Locked)
            {
                GiveLockedFeedback();
                return;
            }
            if (state == DoorState.Closed) Open();
        }

        private void GiveLockedFeedback()
        {
            var gm = GameManager.Instance;
            gm?.Audio.PlayAt(SfxId.DoorLock, transform.position, 0.8f);
            string msg = sealedByRoom ? "Sealed. The test is not finished." : lockedMessage;
            gm?.Subtitles.Show(msg, 2.2f);
        }

        public void Open()
        {
            if (state == DoorState.Open || state == DoorState.Opening) return;
            state = DoorState.Opening;
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Slide(closedPos + Vector3.up * openHeight, DoorState.Open));
            GameManager.Instance?.Audio.PlayAt(SfxId.DoorOpen, transform.position, 0.9f);
            Opened?.Invoke(this);
        }

        public void Close(bool sealDoor)
        {
            if (state == DoorState.Closed || state == DoorState.Locked) return;
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Slide(closedPos, sealDoor ? DoorState.Locked : DoorState.Closed));
            if (sealDoor)
            {
                sealedByRoom = true;
                GameManager.Instance?.Audio.PlayAt(SfxId.DoorSlam, transform.position, 1f);
                GameManager.Instance?.Audio.PlayAt(SfxId.DoorLock, transform.position, 0.8f);
            }
        }

        private IEnumerator Slide(Vector3 target, DoorState endState)
        {
            Vector3 start = panel.localPosition;
            float t = 0f;
            while (t < openSeconds)
            {
                t += Time.deltaTime;
                panel.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / openSeconds));
                yield return null;
            }
            panel.localPosition = target;
            state = endState;
            motion = null;
            if (endState != DoorState.Open) RefreshState();
        }

        /// <summary>Called by DoorSealTrigger when the player crosses into the room.</summary>
        public void NotifyPlayerPassed()
        {
            if (!sealBehindPlayer) return;
            if (state != DoorState.Open) return;
            if (SealReleased()) return; // puzzle already solved: no need to seal
            StartCoroutine(SealAfterDelay());
        }

        private IEnumerator SealAfterDelay()
        {
            yield return new WaitForSeconds(0.9f);
            if (state == DoorState.Open && !SealReleased()) Close(true);
        }
    }
}
