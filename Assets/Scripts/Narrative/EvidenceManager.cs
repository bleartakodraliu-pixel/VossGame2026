using System;
using System.Collections.Generic;
using UnityEngine;
using Voss.Core;

namespace Voss.Narrative
{
    /// <summary>Tracks discovered clues. Persistent; reset on restart.</summary>
    public class EvidenceManager : MonoBehaviour
    {
        private readonly List<EvidenceItem> items = new List<EvidenceItem>();

        public event Action<EvidenceItem> EvidenceDiscovered;

        /// <summary>The letter found in the black envelope at the start of the game.</summary>
        public const string LetterText =
            "Twenty years ago, I performed my final illusion.\n\n" +
            "Four hundred people watched me disappear.\n\n" +
            "The world believed I was gone forever.\n\n" +
            "But you were there.\n\n" +
            "You watched me disappear.\n\n" +
            "Now discover how.\n\n" +
            "- E. Voss";
        public IReadOnlyList<EvidenceItem> Items => items;

        private void Awake()
        {
            BuildDefaults();
        }

        private void BuildDefaults()
        {
            items.Clear();
            items.Add(new EvidenceItem(EvidenceIds.Ticket, "Voss Ticket",
                "VOSS\nPRIVATE PERFORMANCE\n7314",
                "You watched me disappear.\nNow discover how."));
            items.Add(new EvidenceItem(EvidenceIds.Letter, "Letter from E. Voss",
                LetterText,
                "Ticket 7314 was folded inside."));
            items.Add(new EvidenceItem(EvidenceIds.Read, "Card: READ",
                "READ",
                "Found behind the impossible mirror."));
            items.Add(new EvidenceItem(EvidenceIds.Backwards, "Card: BACKWARDS",
                "BACKWARDS",
                "Found inside the Black Box.\nThere was no key."));
        }

        public void Discover(string id)
        {
            var item = Find(id);
            if (item == null || item.Discovered) return;
            item.SetDiscovered(true);
            EvidenceDiscovered?.Invoke(item);
        }

        public bool Has(string id)
        {
            var item = Find(id);
            return item != null && item.Discovered;
        }

        public EvidenceItem Find(string id)
        {
            foreach (var i in items) if (i.Id == id) return i;
            return null;
        }

        public List<EvidenceItem> GetDiscovered()
        {
            var list = new List<EvidenceItem>();
            foreach (var i in items) if (i.Discovered) list.Add(i);
            return list;
        }

        public void ResetEvidence()
        {
            foreach (var i in items) i.SetDiscovered(false);
        }
    }
}
