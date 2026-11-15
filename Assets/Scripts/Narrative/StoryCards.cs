using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voss.Core;
using Voss.UI;

namespace Voss.Narrative
{
    /// <summary>
    /// Plays story lines over the black story card, each fading in, holding long enough to read and
    /// fading out. The caller opens and closes the card so it can hide the switch behind the fade.
    /// </summary>
    public static class StoryCards
    {
        public const string SkipHint = "SPACE   skip";

        /// <param name="onLine">Called as each line begins (sound cues).</param>
        /// <param name="skipped">Set to true if the player skipped.</param>
        public static IEnumerator Play(IList<string> lines, bool skippable, Action<int> onLine = null, Action skipped = null)
        {
            var ui = UIManager.Instance;
            if (ui == null) yield break;
            for (int i = 0; i < lines.Count; i++)
            {
                onLine?.Invoke(i);
                float hold = 1.6f + lines[i].Length * 0.045f;
                float total = 0.8f + hold + 0.6f;
                for (float t = 0f; t < total; t += Time.deltaTime)
                {
                    if (skippable && VossInput.SkipPressed())
                    {
                        ui.Story.SetText(string.Empty, 0f);
                        skipped?.Invoke();
                        yield break;
                    }
                    float a = t < 0.8f ? t / 0.8f : (t > 0.8f + hold ? 1f - (t - 0.8f - hold) / 0.6f : 1f);
                    ui.Story.SetText(lines[i], a);
                    yield return null;
                }
                ui.Story.SetText(string.Empty, 0f);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}
