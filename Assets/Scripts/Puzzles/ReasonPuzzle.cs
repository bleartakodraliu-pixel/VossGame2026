using System.Collections;
using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;

namespace Voss.Puzzles
{
    /// <summary>Pure arithmetic for Room 2 so the answer is derived, not hard-coded, and unit-testable.</summary>
    public static class ReasonPuzzleLogic
    {
        public const int MaskPlusMask = 10;
        public const int MaskPlusKey = 8;
        public const int KeyPlusBox = 7;

        public static int Mask => MaskPlusMask / 2;
        public static int Key => MaskPlusKey - Mask;
        public static int BlackBox => KeyPlusBox - Key;

        public static bool IsCorrect(string input)
        {
            return int.TryParse((input ?? string.Empty).Trim(), out int value) && value == BlackBox;
        }
    }

    /// <summary>Room 2: solve the balance equations for the Black Box. The reward is a card, not a key.</summary>
    public class ReasonPuzzle : PuzzleBase
    {
        [SerializeField] private BalanceScaleController scale;
        [SerializeField] private BlackBoxController blackBox;
        [SerializeField] private Light feedbackLight;

        public void Configure(BalanceScaleController balance, BlackBoxController box, Light feedback)
        {
            scale = balance;
            blackBox = box;
            feedbackLight = feedback;
        }

        private void Start()
        {
            if (feedbackLight != null) feedbackLight.enabled = false;
            var gm = GameManager.Instance;
            if (gm != null && gm.State.ReasonSolved)
            {
                MarkSolvedSilently();
                if (scale != null) scale.SetSettled(true);
                if (blackBox != null) blackBox.SetOpenInstant();
            }
        }

        public override bool Evaluate(string input) => ReasonPuzzleLogic.IsCorrect(input);

        protected override void ApplyStateChanges()
        {
            GameManager.Instance?.State.MarkReasonSolved();
        }

        protected override void OnWrong(string input)
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Reject);
            gm?.VossSays("An assumption costs you.", 3f);
            StartCoroutine(RedPulse());
        }

        private IEnumerator RedPulse()
        {
            if (feedbackLight == null) yield break;
            feedbackLight.color = new Color(1f, 0.08f, 0.05f);
            feedbackLight.enabled = true;
            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.deltaTime;
                feedbackLight.intensity = Mathf.Lerp(6f, 0f, t / 0.9f);
                yield return null;
            }
            feedbackLight.enabled = false;
        }

        protected override void OnCorrect()
        {
            StartCoroutine(RewardSequence());
        }

        private IEnumerator RewardSequence()
        {
            var gm = GameManager.Instance;
            gm?.Audio.Play(SfxId.Confirm);
            gm?.Audio.Play(SfxId.MetalMechanism, 0.8f);

            if (scale != null) yield return scale.Settle();
            yield return new WaitForSeconds(0.6f);

            if (blackBox != null) yield return blackBox.OpenBox();
            yield return new WaitForSeconds(1.2f);

            if (gm != null)
            {
                gm.VossSays("Interesting.", 2.2f);
                yield return new WaitForSeconds(2.4f);
                gm.VossSays("You are still looking for a key.", 3f);
                yield return new WaitForSeconds(3.2f);
                gm.VossSays("I never said there was one.", 3.2f);
            }
        }
    }
}
