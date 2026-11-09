using System;
using UnityEngine;
using UnityEngine.UI;

namespace Voss.UI
{
    public interface IVideoControlTarget
    {
        void Play();
        void Pause();
        void PreviousFrame();
        void NextFrame();
        void ChangeCamera();
        string GetStatusLine();
    }

    /// <summary>Playback controls shown while the player uses the performance reconstruction terminal.</summary>
    public class VideoControlsPanel
    {
        private readonly GameObject root;
        private readonly Text status;
        private IVideoControlTarget target;
        private Action onClose;

        public bool IsOpen => root.activeSelf;

        public VideoControlsPanel(Transform parent)
        {
            var panel = UIFactory.Panel(parent, "VideoControls", new Color(0f, 0f, 0f, 0f));
            root = panel.gameObject;
            panel.GetComponent<Image>().raycastTarget = false;

            var bar = UIFactory.Panel(panel, "Bar", UIFactory.PanelBg, false);
            UIFactory.Place(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(760f, 120f));

            status = UIFactory.Label(bar, "Status", "", 15, UIFactory.Brass, TextAnchor.MiddleCenter);
            UIFactory.Place(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(720f, 30f));

            var row = UIFactory.Empty(bar, "Row");
            UIFactory.Place(row, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(720f, 54f));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            UIFactory.Button(row, "Play", "PLAY", 15, () => { target?.Play(); Refresh(); });
            UIFactory.Button(row, "Pause", "PAUSE", 15, () => { target?.Pause(); Refresh(); });
            UIFactory.Button(row, "Prev", "< FRAME", 15, () => { target?.PreviousFrame(); Refresh(); });
            UIFactory.Button(row, "Next", "FRAME >", 15, () => { target?.NextFrame(); Refresh(); });
            UIFactory.Button(row, "Camera", "CHANGE CAMERA", 15, () => { target?.ChangeCamera(); Refresh(); }, new Color(0.18f, 0.15f, 0.08f, 1f));
            UIFactory.Button(row, "Close", "STEP BACK", 15, () => Close(true));

            root.SetActive(false);
        }

        public void Open(IVideoControlTarget videoTarget, Action closed)
        {
            target = videoTarget;
            onClose = closed;
            root.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            if (target != null) status.text = target.GetStatusLine();
        }

        public void HandleBack()
        {
            if (IsOpen) Close(true);
        }

        public void Close(bool notify)
        {
            root.SetActive(false);
            var cb = onClose;
            target = null;
            onClose = null;
            if (notify) cb?.Invoke();
        }
    }
}
