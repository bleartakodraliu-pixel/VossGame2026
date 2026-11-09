using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Interaction;
using Voss.Narrative;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>01_Invitation: a dark table, a black envelope with a V seal, an old ticket.</summary>
    public static class InvitationBuilder
    {
        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.015f, 0.015f, 0.02f));
            var root = K.Group(null, "Invitation");

            // Room shell (barely visible, keeps the space from feeling like a void)
            K.Box(root.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 8f), K.Mats.Floor);
            K.Box(root.transform, "BackWall", new Vector3(0f, 1.5f, 3f), new Vector3(8f, 3f, 0.2f), K.Mats.Wall);

            // Table
            K.Box(root.transform, "Table", new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.0f), K.Mats.Wood);

            // Envelope
            var envelope = K.Group(root.transform, "Envelope", new Vector3(0f, 0.8f, 0f));
            var body = K.Box(envelope.transform, "EnvelopeBody", new Vector3(0f, 0.006f, 0f), new Vector3(0.24f, 0.012f, 0.16f), K.Mats.BlackMetal);
            var lidPivot = K.Group(envelope.transform, "LidPivot", new Vector3(0f, 0.013f, 0.08f));
            K.Box(lidPivot.transform, "Flap", new Vector3(0f, 0f, -0.045f), new Vector3(0.24f, 0.004f, 0.09f), K.Mats.BlackMetal, false);
            K.Prim(lidPivot.transform, "WaxSeal", PrimitiveType.Cylinder, new Vector3(0f, 0.004f, -0.07f), new Vector3(0.035f, 0.003f, 0.035f), K.Mats.Brass, false);
            K.Text3D(lidPivot.transform, "SealV", "V", new Vector3(0f, 0.0075f, -0.07f), Vector3.up, 0.008f, new Color(0.15f, 0.1f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Bold, 64, Vector3.forward);
            var envRelay = body.AddComponent<RelayInteractable>();

            // Ticket (hidden until the envelope opens)
            var ticket = K.Group(envelope.transform, "VossTicket", new Vector3(0f, 0.014f, -0.01f));
            K.Box(ticket.transform, "TicketCard", Vector3.zero, new Vector3(0.16f, 0.004f, 0.07f), K.Mats.Paper);
            K.Text3D(ticket.transform, "Front", "VOSS\nPRIVATE PERFORMANCE\n7314", new Vector3(0f, 0.0025f, 0f), Vector3.up, 0.012f, new Color(0.12f, 0.08f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Bold, 64, Vector3.forward);
            K.Text3D(ticket.transform, "Back", "You watched me disappear.\nNow find out how.", new Vector3(0f, -0.0025f, 0f), Vector3.down, 0.010f, new Color(0.12f, 0.08f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Italic, 64, Vector3.forward);
            var ticketRelay = ticket.AddComponent<RelayInteractable>();

            // Lighting: one hard key from above-left, a faint cold fill.
            K.SpotLight(root.transform, "KeyLight", new Vector3(-0.8f, 2.2f, -0.6f), new Vector3(0f, 0.8f, 0f), new Color(1f, 0.93f, 0.8f), 10f, 6f, 55f);
            K.PointLight(root.transform, "ColdFill", new Vector3(1.5f, 1.2f, 1.0f), new Color(0.5f, 0.65f, 0.9f), 0.35f, 5f);

            // Player: seated viewpoint, look only.
            var player = K.Player(root.transform, new Vector3(0f, 0f, -0.9f), 0f, movement: false);
            var cam = player.GetComponentInChildren<Camera>();
            cam.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);
            cam.fieldOfView = 55f;

            var seq = root.AddComponent<InvitationSequence>();
            seq.Configure(lidPivot.transform, ticket.transform, envRelay, ticketRelay, player);
        }
    }
}
