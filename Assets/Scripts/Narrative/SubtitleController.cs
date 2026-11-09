using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>Queues subtitle lines and forwards them to the UI. Supports sequences of lines.</summary>
    public class SubtitleController : MonoBehaviour
    {
        private readonly Queue<(string text, float duration)> queue = new Queue<(string, float)>();
        private Coroutine runner;

        public bool IsBusy => runner != null;

        public void Show(string text, float duration = 3.5f)
        {
            if (string.IsNullOrEmpty(text)) return;
            queue.Enqueue((text, duration));
            if (runner == null) runner = StartCoroutine(Run());
        }

        /// <summary>Plays lines back to back. Returns a coroutine the caller can yield on.</summary>
        public Coroutine ShowSequence(IList<string> lines, float secondsPerLine = 3.5f, float gap = 0.4f)
        {
            return StartCoroutine(SequenceRoutine(lines, secondsPerLine, gap));
        }

        private IEnumerator SequenceRoutine(IList<string> lines, float per, float gap)
        {
            foreach (var l in lines)
            {
                Show(l, per);
                yield return new WaitForSeconds(per + gap);
            }
        }

        public void Clear()
        {
            queue.Clear();
            if (runner != null) StopCoroutine(runner);
            runner = null;
            UIManager.Instance?.SetSubtitle(string.Empty);
        }

        private IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                var (text, duration) = queue.Dequeue();
                UIManager.Instance?.SetSubtitle(text);
                yield return new WaitForSeconds(duration);
                UIManager.Instance?.SetSubtitle(string.Empty);
                yield return new WaitForSeconds(0.15f);
            }
            runner = null;
        }
    }
}
