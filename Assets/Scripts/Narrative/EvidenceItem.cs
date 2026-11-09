using System;
using UnityEngine;

namespace Voss.Narrative
{
    /// <summary>A reviewable clue. Front and back text are shown in the evidence menu.</summary>
    [Serializable]
    public class EvidenceItem
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] [TextArea] private string frontText;
        [SerializeField] [TextArea] private string backText;
        [SerializeField] private bool discovered;

        public string Id => id;
        public string Title => title;
        public string FrontText => frontText;
        public string BackText => backText;
        public bool Discovered => discovered;

        public EvidenceItem(string id, string title, string frontText, string backText)
        {
            this.id = id;
            this.title = title;
            this.frontText = frontText;
            this.backText = backText;
            discovered = false;
        }

        public void SetDiscovered(bool value) => discovered = value;
    }
}
