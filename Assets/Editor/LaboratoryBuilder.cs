using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Atmosphere;
using Voss.Audio;
using Voss.Core;
using Voss.Interaction;
using Voss.Narrative;
using Voss.Puzzles;
using Voss.UI;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// 02_VossLaboratory as a hub: the Main Hall (elevator, experiment terminal, observation window)
    /// with three chamber doors side by side on its north wall - PERCEPTION, REASON, MEMORY - each
    /// opening onto its own corridor and sealed room. The doors unlock one by one as the player comes
    /// back to the hall after each test. After MEMORY, a section of the hall's west wall rises: behind
    /// it, the final experiment (performance screen, final question, the exit to the theatre).
    /// </summary>
    public static class LaboratoryBuilder
    {
        private const float H = 5f;                    // hall height
        private const float RoomH = 4.5f;
        private const float CorrH = 3.2f;              // corridor height
        private static readonly string[] RoomNames = { "PERCEPTION", "REASON", "MEMORY" };

        // Every room is built in the same local frame: entrance door at z 7 (in the hall's north wall),
        // corridor z 7..11, room z 11..23, x -4.2..4.2. Spaced 11 m apart so PERCEPTION's mirror niches
        // never touch REASON.
        private static readonly Vector3[] RoomOrigin = { new Vector3(-11f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(11f, 0f, 0f) };
        private static readonly Vector3 HiddenDoorAt = new Vector3(-16f, 0f, -3f);

        private static readonly Color Cold = new Color(0.55f, 0.7f, 1f);
        private static readonly Color Brass = new Color(0.78f, 0.62f, 0.32f);

        private static Transform root;
        private static readonly Transform[] rooms = new Transform[3];

        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.045f, 0.035f, 0.035f));
            K.Fog(new Color(0.035f, 0.02f, 0.02f), 0.04f);
            root = K.Group(null, "VossLaboratory").transform;
            K.GlobalVolume(root, K.HorrorProfile("Laboratory", 0.9f, 0.4f, 0.35f, new Color(1f, 0.92f, 0.88f)));

            BuildHallShell();
            BuildRoomShells();

            var hallLights = BuildHallLights(out var emergencyLights);
            var entrance = BuildEntrance(hallLights, emergencyLights);
            var doors = BuildEntranceDoors();

            var perception = BuildPerceptionRoom();
            var reason = BuildReasonRoom(out var reasonLights);
            var memoryTerminal = BuildMemoryRoom(out var memoryLights, out var memoryPuzzle);

            var hiddenDoor = BuildFinalRoom(out var principles, out var finalLights, out var chamber, out var chamberProjection);
            var video = BuildPerformanceScreens();
            var finalQuestion = BuildFinalQuestionTerminal();
            // The projector above the seats: at the very end it puts Voss back in the glass, for a moment.
            var projectorLight = K.SpotLight(root.Find("FinalExperiment"), "RevealProjector", new Vector3(-17.6f, 4.6f, -2.5f),
                ChamberAt + new Vector3(0f, 1.3f, 0f), new Color(0.9f, 0.95f, 1f), 7f, 14f, 22f);
            BuildSilhouetteEvent(root.Find("HallLights/HallLight_S").GetComponent<Light>());

            // MEMORY keypad, beside MEMORY's door in the hall.
            float mx = RoomOrigin[2].x + 1.45f;
            var keypad = K.Box(root, "MemoryDoorKeypad", new Vector3(mx, 1.4f, 6.8f), new Vector3(0.3f, 0.42f, 0.08f), K.Mats.Brass);
            K.Text3D(root, "KeypadLabel", "7 8 9\n4 5 6\n1 2 3\n  0", new Vector3(mx, 1.4f, 6.75f), Vector3.back, 0.05f, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            keypad.AddComponent<MemoryDoorController>().Configure(doors[2]);

            // Managers
            var managers = K.Group(root, "Managers");
            var puzzleManager = managers.AddComponent<PuzzleManager>();
            puzzleManager.Configure(perception, reason, memoryPuzzle);

            var reveal = managers.AddComponent<FinalRevealController>();
            var player = K.Player(root, new Vector3(0f, 0f, -8.6f), 0f);
            reveal.Configure(projectorLight, chamberProjection, chamber, finalLights, video, player.GetComponent<Voss.Player.FirstPersonController>());
            finalQuestion.Configure(reveal, video, finalQuestion.GetComponentInChildren<TextMesh>());

            // REASON's door opens the moment PERCEPTION is solved (no waiting for the card or the hall).
            var toReason = K.Group(managers.transform, "Reveal_REASON").AddComponent<DoorRevealSequence>();
            toReason.Configure(doors[1], GameFlag.ReasonUnlocked, null, new List<Light> { rooms[1].Find("CorridorLight").GetComponent<Light>() },
                null, null, "The second door is open. REASON is waiting.");
            toReason.SetDelay(0f);
            // MEMORY's way is revealed when the player walks back into the hall from REASON.
            var reasonGate = K.TriggerBox(root, "HallGate_REASON", new Vector3(RoomOrigin[1].x, 1.2f, 6.0f), new Vector3(2.4f, 2.4f, 2.0f)).gameObject.AddComponent<PlayerTriggerZone>();
            var toMemory = K.Group(managers.transform, "Reveal_MEMORY").AddComponent<DoorRevealSequence>();
            toMemory.Configure(null, GameFlag.MemoryUnlocked, EvidenceIds.Backwards, new List<Light> { rooms[2].Find("CorridorLight").GetComponent<Light>() },
                null, null, "One door remains. You already know the number.");
            toMemory.ConfigureGate(reasonGate, false);
            // Taking the BACKWARDS card plunges REASON into darkness for a moment.
            K.Group(managers.transform, "Blackout_REASON").AddComponent<EvidenceBlackout>().Configure(EvidenceIds.Backwards, reasonLights,
                K.Group(rooms[1], "WhisperAt", new Vector3(0f, 1.6f, 11.5f)).transform);

            // Status lamps beside the three doors.
            var completed = new[] { GameFlag.PerceptionSolved, GameFlag.ReasonSolved, GameFlag.MetaCodeSolved };
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3(RoomOrigin[i].x + 1.45f, 2.85f, 6.8f);
                var lampGroup = K.Group(root, "DoorStatus_" + RoomNames[i], at).transform;
                K.Box(lampGroup, "Housing", Vector3.zero, new Vector3(0.18f, 0.22f, 0.06f), K.Mats.BlackMetal, false);
                var bulb = K.Prim(lampGroup, "Bulb", PrimitiveType.Sphere, new Vector3(0f, 0f, -0.05f), Vector3.one * 0.12f, K.Mats.Porcelain, false);
                var light = K.PointLight(lampGroup, "Light", new Vector3(0f, 0f, -0.35f), new Color(1f, 0.1f, 0.06f), 0.9f, 3.5f);
                // MEMORY's door opens by keypad; its lamp turns warm when the way is revealed (corridor light on).
                var signal = i == 2 ? rooms[2].Find("CorridorLight").GetComponent<Light>() : null;
                lampGroup.gameObject.AddComponent<DoorStatusLamp>().Configure(doors[i], signal, completed[i], light, bulb.GetComponent<Renderer>());
            }

            var finale = managers.AddComponent<FinalInvestigationController>();
            finale.Configure(perception, reasonLights, memoryLights, hallLights, principles[0], principles[1], principles[2], video, finalQuestion,
                new List<DoorController> { hiddenDoor }, finalLights);
            memoryPuzzle.Configure(memoryTerminal, memoryLights, finale);

            managers.AddComponent<GameRestartController>();
            entrance.transform.SetParent(managers.transform, true);

            var hallWhispers = BuildAtmosphere();
            LaboratoryProps.Build(root, rooms);
            LaboratoryDressing.Build(root, rooms);

            // The hall changes after every test.
            var reds = new List<Light>(emergencyLights);
            var underground = root.Find("Entrance/Underground");
            if (underground != null)
                foreach (Transform t in underground)
                    if (t.name == "RedCageLamp" && t.Find("Light") != null) reds.Add(t.Find("Light").GetComponent<Light>());
            var monitors = new List<TextMesh>();
            for (int i = 0; i < 3; i++)
            {
                var m = root.Find("Entrance/ControlRoom/MonitorText_" + i);
                if (m != null) monitors.Add(m.GetComponent<TextMesh>());
            }
            managers.AddComponent<HallProgression>().Configure(root.Find("HallLights/HallLight_N").GetComponent<Light>(), reds, hallWhispers, monitors);

            BuildShadowZones(managers);
        }

        /// <summary>
        /// Only the shadow-casting lights of the area the player is in cast shadows (see ShadowZones).
        /// Each room's area reaches a little into the hall, so looking in through its door shows them too.
        /// </summary>
        private static void BuildShadowZones(GameObject managers)
        {
            List<Light> Lights(params string[] paths)
            {
                var list = new List<Light>();
                foreach (var p in paths)
                {
                    var t = root.Find(p);
                    if (t == null) { Debug.LogWarning("[Voss] No shadow light at " + p); continue; }
                    list.Add(t.GetComponent<Light>());
                }
                return list;
            }
            var zones = managers.AddComponent<ShadowZones>();
            zones.AddZone("Hall", new Bounds(new Vector3(0f, 2.5f, -1.5f), new Vector3(34f, 6f, 19f)),
                Lights("HallLights/HallLight_W", "HallLights/HallLight_E", "Entrance/Underground/StageCorner/StageSpot"));
            var roomLights = new[] { "PERCEPTIONRoom/PerceptionLight_A", "REASONRoom/ScaleLight", "MEMORYRoom/MemoryLight_A" };
            for (int i = 0; i < 3; i++)
                zones.AddZone(RoomNames[i], new Bounds(new Vector3(RoomOrigin[i].x, 2.5f, 14.5f), new Vector3(9.5f, 6f, 21f)), Lights(roomLights[i]));
            zones.AddZone("Final", new Bounds(new Vector3(-23f, 2.5f, -2.5f), new Vector3(16f, 6f, 10f)),
                Lights("FinalExperiment/ChamberLight", "FinalExperiment/RevealProjector"));
        }

        // ========================================================== atmosphere

        /// <summary>
        /// Unreliable lamps, hard shadows, dust, room tone and distant noises, whispers in MEMORY, a hall
        /// that starts whispering later (returned, enabled by HallProgression), and applause in the room
        /// behind the hidden door.
        /// </summary>
        private static GameObject BuildAtmosphere()
        {
            foreach (var path in new[] { "HallLights/HallLight_W", "HallLights/HallLight_E", "PERCEPTIONRoom/PerceptionLight_A", "REASONRoom/ScaleLight", "MEMORYRoom/MemoryLight_A" })
            {
                var l = root.Find(path)?.GetComponent<Light>();
                if (l != null) l.shadows = LightShadows.Soft;
            }

            Flicker("HallLights/HallLight_N", 0.1f, new Vector2(5f, 12f), 0.5f);
            Flicker("REASONRoom/ReasonLight_A", 0.06f, new Vector2(10f, 20f), 0.2f);
            Flicker("MEMORYRoom/MemoryLight_B", 0.12f, new Vector2(4f, 10f), 0.45f);
            Flicker("FinalExperiment/FinalLight_B", 0.12f, new Vector2(3f, 8f), 0.4f);

            var dust = new Color(0.9f, 0.88f, 0.8f, 0.25f);
            K.Dust(root.Find("Entrance"), "Dust", new Vector3(0f, 2.5f, 0f), new Vector3(30f, 4.5f, 13f), 14f, dust);
            for (int i = 0; i < 3; i++) K.Dust(rooms[i], "Dust", new Vector3(0f, 2.2f, 17f), new Vector3(8f, 4f, 11f), 6f, dust);
            K.Dust(root.Find("FinalExperiment"), "Dust", new Vector3(-23f, 2.4f, -2.5f), new Vector3(13f, 4.5f, 8.5f), 6f, dust);

            K.Group(root, "Soundscape").AddComponent<AmbientSoundscape>().Configure(
                new[] { SfxId.Drip, SfxId.Drip, SfxId.Creak, SfxId.Creak, SfxId.Knock, SfxId.DistantSteps },
                new Vector2(9f, 22f), new Vector2(4f, 9f), 1f, true, 0.3f);

            var whisperZone = K.TriggerBox(rooms[2], "WhisperZone", new Vector3(0f, 2f, 17f), new Vector3(8.4f, 4f, 12f));
            whisperZone.gameObject.AddComponent<AmbientSoundscape>().Configure(new[] { SfxId.Whisper }, new Vector2(6f, 14f), new Vector2(1.5f, 3f), 1f);

            // After REASON the hall itself begins to whisper (switched on by HallProgression).
            var hallWhispers = K.TriggerBox(root.Find("Entrance"), "HallWhispers", new Vector3(0f, 2.5f, 0f), new Vector3(32f, 5f, 14f));
            hallWhispers.gameObject.AddComponent<AmbientSoundscape>().Configure(new[] { SfxId.Whisper, SfxId.DistantSteps }, new Vector2(8f, 16f), new Vector2(2f, 5f), 1f);

            // Behind the hidden door: applause for a performance nobody is watching.
            var scares = K.Group(root, "Scares").transform;
            var applauseAt = K.Group(scares, "ApplauseAhead", new Vector3(-22f, 1.5f, -3f));
            K.TriggerBox(scares, "ApplauseTrigger", new Vector3(-17.2f, 1.2f, -3f), new Vector3(1.6f, 2.4f, 2.4f)).gameObject
                .AddComponent<ScareTrigger>().Configure(GameFlag.MetaCodeSolved, null, true, SfxId.Applause, applauseAt.transform, 1f, null);
            return hallWhispers.gameObject;
        }

        private static void Flicker(string path, float wobble, Vector2 interval, float dropout)
        {
            var t = root.Find(path);
            if (t == null) { Debug.LogWarning("[Voss] No light at " + path); return; }
            t.gameObject.AddComponent<LightFlicker>().Configure(wobble, interval, dropout);
        }

        // ================================================================ shell

        private static void BuildHallShell()
        {
            var hall = K.Group(root, "Entrance").transform;
            var wall = K.Mats.Cracked;
            K.FloorAndCeiling(hall, "Hall", new Vector2(-16f, -7f), new Vector2(16f, 7f), H, K.Mats.WetFloor, K.Mats.ConcreteCeiling);
            // South wall with elevator gap
            K.Wall(hall, "Wall_South", true, -7f, -16f, 16f, H, wall, 0f, 3f, 3f);
            // North wall: the three chamber doors, all visible from the hall
            K.WallWithGaps(hall, "Wall_North", true, 7f, -16f, 16f, H, wall,
                new Vector3(RoomOrigin[0].x, 2f, 3f), new Vector3(RoomOrigin[1].x, 2f, 3f), new Vector3(RoomOrigin[2].x, 2f, 3f));
            // West wall: the opening of the hidden door (closed by a panel of the same wall)
            K.Wall(hall, "Wall_West", false, -16f, -7f, 7f, H, wall, HiddenDoorAt.z, 2f, 3f);
            // East wall with the observation window (z -1..3, sill 1.1, head 2.4); the control room is behind it.
            K.Segment(hall, "Wall_East_S", false, 16f, -7f, -1f, 0f, H, wall);
            K.Segment(hall, "Wall_East_N", false, 16f, 3f, 7f, 0f, H, wall);
            K.Segment(hall, "Wall_East_Sill", false, 16f, -1f, 3f, 0f, 1.1f, wall);
            K.Segment(hall, "Wall_East_Head", false, 16f, -1f, 3f, 2.4f, H, wall);
            K.Box(hall, "ObservationGlass", new Vector3(16f, 1.75f, 1f), new Vector3(0.04f, 1.3f, 4f), K.Mats.DarkGlass);
            K.Box(hall, "WindowFrame_T", new Vector3(15.83f, 2.42f, 1f), new Vector3(0.06f, 0.06f, 4.1f), K.Mats.Steel, false);
            K.Box(hall, "WindowFrame_B", new Vector3(15.8f, 1.1f, 1f), new Vector3(0.12f, 0.05f, 4.1f), K.Mats.Steel, false);
            K.Box(hall, "WindowFrame_L", new Vector3(15.83f, 1.75f, -1.02f), new Vector3(0.06f, 1.36f, 0.06f), K.Mats.Steel, false);
            K.Box(hall, "WindowFrame_R", new Vector3(15.83f, 1.75f, 3.02f), new Vector3(0.06f, 1.36f, 0.06f), K.Mats.Steel, false);
            K.Box(hall, "WindowFrame_M", new Vector3(15.83f, 1.75f, 1f), new Vector3(0.05f, 1.3f, 0.05f), K.Mats.Steel, false);

            // Structural columns
            foreach (float cx in new[] { -14f, -4f, 8f, 14f })
                K.SurfaceBox(hall, "Column_S", new Vector3(cx, H * 0.5f, -6.75f), new Vector3(0.4f, H, 0.4f), K.Mats.Cracked);
            foreach (float cx in new[] { -15f, -5f, 5f, 15f })
                K.SurfaceBox(hall, "Column_N", new Vector3(cx, H * 0.5f, 6.75f), new Vector3(0.4f, H, 0.4f), K.Mats.Cracked);

            // Elevator car
            var elev = K.Group(hall, "Elevator").transform;
            K.FloorAndCeiling(elev, "Elevator", new Vector2(-1.5f, -10f), new Vector2(1.5f, -7f), 3f, K.Mats.BlackMetal, K.Mats.BlackMetal);
            K.Wall(elev, "Elevator_Back", true, -10f, -1.5f, 1.5f, 3f, K.Mats.BlackMetal);
            K.Wall(elev, "Elevator_W", false, -1.5f, -10f, -7f, 3f, K.Mats.BlackMetal);
            K.Wall(elev, "Elevator_E", false, 1.5f, -10f, -7f, 3f, K.Mats.BlackMetal);
            K.Box(elev, "ElevatorLamp", new Vector3(0f, 2.9f, -8.5f), new Vector3(0.6f, 0.05f, 0.6f), K.Mats.SignRed, false);
        }

        /// <summary>The three chambers, each behind its own door and corridor, sealed off from the others.</summary>
        private static void BuildRoomShells()
        {
            for (int i = 0; i < 3; i++)
            {
                var grp = K.Group(root, RoomNames[i] + "Room", RoomOrigin[i]).transform;
                rooms[i] = grp;

                // Entrance corridor z 7..11 (its mouth is the hall's north wall).
                K.FloorAndCeiling(grp, "Corridor", new Vector2(-1.2f, 7f), new Vector2(1.2f, 11f), CorrH, K.Mats.WetFloor, K.Mats.ConcreteCeiling);
                K.Wall(grp, "Corridor_W", false, -1.2f, 7f, 11f, CorrH, K.Mats.Cracked);
                K.Wall(grp, "Corridor_E", false, 1.2f, 7f, 11f, CorrH, K.Mats.Cracked);

                // Room z 11..23. PERCEPTION is an art-deco mirror hall, REASON an old study, MEMORY raw concrete.
                var wall = i == 0 ? K.Mats.Navy : i == 1 ? K.Mats.WarmPlaster : K.Mats.Cracked;
                var floor = i == 0 ? K.Mats.Marble : i == 1 ? K.Mats.StoneFloor : K.Mats.WetFloor;
                var ceiling = i == 0 ? K.Mats.NavyCeiling : K.Mats.ConcreteCeiling;
                K.FloorAndCeiling(grp, "Room", new Vector2(-4.2f, 11f), new Vector2(4.2f, 23f), RoomH, floor, ceiling);
                K.Wall(grp, "Room_South", true, 11f, -4.2f, 4.2f, RoomH, wall, 0f, 2.4f, 3.2f);
                if (i == 0)
                {
                    // Perception: side walls carry mirror niches at z 14 and 20, the far wall the statue niche.
                    var niches = new[] { new Vector3(14f, 1.8f, 2.6f), new Vector3(20f, 1.8f, 2.6f) };
                    K.WallWithGaps(grp, "Room_W", false, -4.2f, 11f, 23f, RoomH, wall, niches);
                    K.WallWithGaps(grp, "Room_E", false, 4.2f, 11f, 23f, RoomH, wall, niches);
                    K.Wall(grp, "Room_North", true, 23f, -4.2f, 4.2f, RoomH, wall, 0f, 1.8f, 2.6f);
                }
                else
                {
                    K.Wall(grp, "Room_W", false, -4.2f, 11f, 23f, RoomH, wall);
                    K.Wall(grp, "Room_E", false, 4.2f, 11f, 23f, RoomH, wall);
                    K.Wall(grp, "Room_North", true, 23f, -4.2f, 4.2f, RoomH, wall);
                }
            }
        }

        // ============================================================== lights

        private static List<Light> BuildHallLights(out List<Light> emergency)
        {
            var g = K.Group(root, "HallLights").transform;
            var list = new List<Light>
            {
                K.PointLight(g, "HallLight_W", new Vector3(-8f, 4.3f, 0f), Cold, 1.3f, 13f),
                K.PointLight(g, "HallLight_E", new Vector3(8f, 4.3f, 0f), Cold, 1.3f, 13f),
                K.PointLight(g, "HallLight_S", new Vector3(0f, 4.3f, -4f), Cold, 1.0f, 11f),
                K.PointLight(g, "HallLight_N", new Vector3(0f, 4.3f, 4f), Cold, 1.0f, 11f),
                K.PointLight(g, "ElevatorLight", new Vector3(0f, 2.7f, -8.5f), new Color(1f, 0.85f, 0.65f), 0.9f, 5f),
            };
            var red = new Color(1f, 0.12f, 0.08f);
            emergency = new List<Light>
            {
                K.PointLight(g, "Emergency_W", new Vector3(-9f, 4.2f, 0f), red, 2.2f, 11f),
                K.PointLight(g, "Emergency_C", new Vector3(0f, 4.2f, 0f), red, 2.2f, 11f),
                K.PointLight(g, "Emergency_E", new Vector3(9f, 4.2f, 0f), red, 2.2f, 11f),
            };
            return list;
        }

        // ============================================================ entrance

        private static LabEntranceSequence BuildEntrance(List<Light> hallLights, List<Light> emergencyLights)
        {
            var hall = root.Find("Entrance");
            var elev = hall.Find("Elevator");

            // Elevator doors on pivots so the sequence can slide them by a plain offset.
            var leftPivot = K.Group(elev, "ElevatorDoorLeft", new Vector3(-0.75f, 1.5f, -7f));
            K.Box(leftPivot.transform, "Panel", Vector3.zero, new Vector3(1.5f, 3f, 0.15f), K.Mats.Steel);
            var rightPivot = K.Group(elev, "ElevatorDoorRight", new Vector3(0.75f, 1.5f, -7f));
            K.Box(rightPivot.transform, "Panel", Vector3.zero, new Vector3(1.5f, 3f, 0.15f), K.Mats.Steel);

            var zone = K.TriggerBox(hall, "ExitElevatorZone", new Vector3(0f, 1.2f, -4.8f), new Vector3(3.5f, 2.4f, 2.2f)).gameObject.AddComponent<PlayerTriggerZone>();

            // Experiment terminal: large monitor on the south wall.
            var monitor = K.Box(hall, "ExperimentTerminal", new Vector3(4.2f, 2.4f, -6.82f), new Vector3(3.4f, 1.9f, 0.16f), K.Mats.Screen);
            K.Box(monitor.transform, "Bezel", new Vector3(0f, 0f, -0.1f), new Vector3(1.06f, 1.08f, 0.4f), K.Mats.BlackMetal, false);
            var screen = K.Text3D(hall, "ExperimentTerminalText", "", new Vector3(4.2f, 2.4f, -6.72f), Vector3.forward, 0.17f, new Color(0.85f, 0.3f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var terminal = monitor.AddComponent<TerminalController>();
            terminal.Configure(screen, null, LabEntranceSequence.MonitorText, "", "", 4, "");

            var seqGo = K.Group(root, "LabEntranceSequence");
            var seq = seqGo.AddComponent<LabEntranceSequence>();
            seq.Configure(leftPivot.transform, rightPivot.transform, zone, hallLights, emergencyLights, terminal);
            return seq;
        }

        // =============================================================== doors

        /// <summary>
        /// The three chamber doors, side by side in the hall's north wall. PERCEPTION is open from the
        /// start; REASON is unlocked by its reveal, MEMORY by the keypad. Each seals behind the player
        /// until its test is solved, then opens again from the inside.
        /// </summary>
        private static DoorController[] BuildEntranceDoors()
        {
            var doors = new DoorController[3];
            var flags = new[] { GameFlag.PerceptionUnlocked, GameFlag.None, GameFlag.None };
            var releases = new[] { GameFlag.PerceptionSolved, GameFlag.ReasonSolved, GameFlag.MetaCodeSolved };
            var lockedMsgs = new[] { "Locked.", "Sealed. Complete PERCEPTION first.", "Sealed. Use the keypad beside the door." };

            for (int i = 0; i < 3; i++)
            {
                var grp = rooms[i];
                var door = MakeDoor(grp, "Door_" + RoomNames[i], new Vector3(0f, 0f, 7f), RoomNames[i], true);
                door.Configure(door.transform.Find("Panel"), flags[i], lockedMsgs[i], true, releases[i], i > 0, RoomNames[i]);

                var sealZone = K.TriggerBox(grp, "SealTrigger", new Vector3(0f, 1.2f, 10f), new Vector3(2.2f, 2.4f, 1.6f));
                sealZone.gameObject.AddComponent<DoorSealTrigger>().Bind(door);
                doors[i] = door;
            }
            return doors;
        }

        /// <summary>
        /// Black sliding door in a brass frame. "Panel" is the moving part; the label faces the side
        /// the player approaches from (local -z), above the frame in a tall wall or on the panel otherwise.
        /// </summary>
        private static DoorController MakeDoor(Transform parent, string name, Vector3 localPos, string label, bool tallWall)
        {
            var doorRoot = K.Group(parent, name, localPos);
            var mover = K.Group(doorRoot.transform, "Panel");
            K.Box(mover.transform, "Slab", new Vector3(0f, 1.5f, 0f), new Vector3(2f, 3f, 0.25f), K.Mats.DoorPaint);
            K.Box(mover.transform, "Window", new Vector3(0.45f, 2.05f, 0f), new Vector3(0.42f, 0.55f, 0.27f), K.Mats.DarkGlass, false);
            K.Box(mover.transform, "WindowFrame", new Vector3(0.45f, 2.05f, 0f), new Vector3(0.5f, 0.63f, 0.262f), K.Mats.Steel, false);
            K.Box(mover.transform, "KickPlate", new Vector3(0f, 0.2f, 0f), new Vector3(1.9f, 0.32f, 0.27f), K.Mats.Steel, false);
            K.Box(mover.transform, "PushBar", new Vector3(0f, 1.05f, -0.17f), new Vector3(1.3f, 0.05f, 0.06f), K.Mats.Steel, false);
            K.Box(mover.transform, "PushBarBack", new Vector3(0f, 1.05f, 0.17f), new Vector3(1.3f, 0.05f, 0.06f), K.Mats.Steel, false);
            if (!string.IsNullOrEmpty(label))
            {
                if (tallWall)
                    K.Text3D(doorRoot.transform, "Label", label, new Vector3(0f, 3.75f, -0.2f), Vector3.back, 0.4f, Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
                else
                    K.Text3D(mover.transform, "Label", label, new Vector3(0f, 2.55f, -0.14f), Vector3.back, 0.2f, Brass, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            K.Box(doorRoot.transform, "Frame_L", new Vector3(-1.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 0.4f), K.Mats.Steel);
            K.Box(doorRoot.transform, "Frame_R", new Vector3(1.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 0.4f), K.Mats.Steel);
            return doorRoot.AddComponent<DoorController>();
        }

        // ========================================================== perception

        private static PerceptionPuzzle BuildPerceptionRoom()
        {
            const float dx = 0f; // room-local frame
            var grp = rooms[0];
            var puzzleGo = K.Group(grp, "PerceptionPuzzle");
            var puzzle = puzzleGo.AddComponent<PerceptionPuzzle>();
            puzzle.SetPuzzleId("perception");

            var displays = new List<MirrorDisplay>
            {
                Exhibit(grp, puzzle, "Antique Clock", new Vector3(dx - 4.2f, 0f, 14f), Vector3.right, false, BuildClock, "The hands have stopped at 7:14."),
                Exhibit(grp, puzzle, "Voss Portrait", new Vector3(dx - 4.2f, 0f, 20f), Vector3.right, false, BuildPortrait, "Looking is automatic. Observing is a decision."),
                Exhibit(grp, puzzle, "Statue", new Vector3(dx, 0f, 23f), Vector3.back, false, BuildStatue, "A figure with one arm raised."),
                Exhibit(grp, puzzle, "Plant", new Vector3(dx + 4.2f, 0f, 14f), Vector3.left, true, BuildPlant, "Every leaf leans the same way. Even in the glass."),
                Exhibit(grp, puzzle, "Geometric Sculpture", new Vector3(dx + 4.2f, 0f, 20f), Vector3.left, false, BuildGeometry, "Two bars. One corner."),
            };

            // Hidden compartment: a small brass cabinet on the east wall beside the Plant's mirror.
            var cab = K.Group(grp, "HiddenREADCompartment", new Vector3(dx + 4.2f, 1.35f, 16.6f));
            K.Box(cab.transform, "Back", new Vector3(-0.02f, 0f, 0f), new Vector3(0.04f, 0.8f, 1.1f), K.Mats.Niche);
            K.Box(cab.transform, "Top", new Vector3(-0.25f, 0.4f, 0f), new Vector3(0.5f, 0.04f, 1.1f), K.Mats.BlackMetal);
            K.Box(cab.transform, "Bottom", new Vector3(-0.25f, -0.4f, 0f), new Vector3(0.5f, 0.04f, 1.1f), K.Mats.BlackMetal);
            K.Box(cab.transform, "SideA", new Vector3(-0.25f, 0f, 0.55f), new Vector3(0.5f, 0.8f, 0.04f), K.Mats.BlackMetal);
            K.Box(cab.transform, "SideB", new Vector3(-0.25f, 0f, -0.55f), new Vector3(0.5f, 0.8f, 0.04f), K.Mats.BlackMetal);
            var compartmentDoor = K.Box(cab.transform, "CompartmentDoor", new Vector3(-0.52f, 0f, 0f), new Vector3(0.05f, 0.84f, 1.14f), K.Mats.Brass);
            var card = K.Group(cab.transform, "READCard", new Vector3(-0.2f, -0.2f, 0f));
            K.Box(card.transform, "CardBody", Vector3.zero, new Vector3(0.006f, 0.12f, 0.18f), K.Mats.Card);
            K.Text3D(card.transform, "Text", "READ", new Vector3(-0.004f, 0f, 0f), Vector3.left, 0.035f, new Color(0.1f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            card.AddComponent<EvidencePickup>().Configure(EvidenceIds.Read, "READ. One word. Nothing else on the card.");
            K.PointLight(cab.transform, "CabinetLight", new Vector3(-0.3f, 0.2f, 0f), new Color(1f, 0.9f, 0.7f), 0.6f, 1.5f);

            var lights = new List<Light>
            {
                // Cold moonlight blue, like the mirror hall in the concept art; the chandelier glows gold.
                K.PointLight(grp, "PerceptionLight_A", new Vector3(dx - 2.5f, 4f, 14f), new Color(0.62f, 0.74f, 1f), 1.5f, 10f),
                K.PointLight(grp, "PerceptionLight_B", new Vector3(dx + 2.5f, 4f, 20f), new Color(0.62f, 0.74f, 1f), 1.5f, 10f),
                K.PointLight(grp, "CorridorLight", new Vector3(dx, 3f, 9f), new Color(0.55f, 0.7f, 1f), 0.7f, 6f),
            };
            var feedback = K.PointLight(grp, "FeedbackLight", new Vector3(dx, 3.6f, 17f), Color.red, 0f, 14f);
            feedback.enabled = false;

            puzzle.Configure(displays, lights, feedback, compartmentDoor.transform, card);
            return puzzle;
        }

        /// <summary>Niche in the wall, glass pane at the wall plane, pedestal + object in front, "reflection" copy behind.</summary>
        private static MirrorDisplay Exhibit(Transform grp, PerceptionPuzzle puzzle, string id, Vector3 wallPoint, Vector3 normal,
            bool impossible, Action<Transform> buildObject, string note)
        {
            var ex = K.Group(grp, "Exhibit_" + id.Replace(" ", ""), wallPoint);
            var rot = Quaternion.LookRotation(normal, Vector3.up);
            ex.transform.localRotation = rot; // relative to the (possibly rotated) room
            var t = ex.transform;

            // Niche behind the wall plane (local -z), 1.8 wide, 2.6 high, 1.6 deep
            K.Box(t, "Niche_Back", new Vector3(0f, 1.3f, -1.65f), new Vector3(1.9f, 2.7f, 0.1f), K.Mats.Niche);
            K.Box(t, "Niche_L", new Vector3(-0.95f, 1.3f, -0.85f), new Vector3(0.1f, 2.7f, 1.7f), K.Mats.Niche);
            K.Box(t, "Niche_R", new Vector3(0.95f, 1.3f, -0.85f), new Vector3(0.1f, 2.7f, 1.7f), K.Mats.Niche);
            K.Box(t, "Niche_Top", new Vector3(0f, 2.65f, -0.85f), new Vector3(1.9f, 0.1f, 1.7f), K.Mats.Niche);
            K.Box(t, "Niche_Floor", new Vector3(0f, -0.05f, -0.85f), new Vector3(1.9f, 0.1f, 1.7f), K.Mats.Niche);

            // Mirror pane (the thing the player selects)
            var pane = K.Box(t, "MirrorPane", new Vector3(0f, 1.3f, 0f), new Vector3(1.8f, 2.6f, 0.04f), K.Mats.Mirror);
            K.Box(t, "MirrorFrame_L", new Vector3(-0.97f, 1.3f, 0.02f), new Vector3(0.14f, 2.74f, 0.14f), K.Mats.Gold, false);
            K.Box(t, "MirrorFrame_R", new Vector3(0.97f, 1.3f, 0.02f), new Vector3(0.14f, 2.74f, 0.14f), K.Mats.Gold, false);
            K.Box(t, "MirrorFrame_T", new Vector3(0f, 2.67f, 0.02f), new Vector3(2.08f, 0.14f, 0.14f), K.Mats.Gold, false);
            K.Box(t, "MirrorFrame_B", new Vector3(0f, -0.02f, 0.02f), new Vector3(2.08f, 0.08f, 0.14f), K.Mats.Gold, false);
            // Art-deco fan crest above the frame.
            var crest = K.Group(t, "Crest", new Vector3(0f, 2.74f, 0.04f)).transform;
            K.Prim(crest, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.02f, 0.22f), K.Mats.Gold, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int r = 0; r < 9; r++)
            {
                float ang = -64f + r * 16f;
                var ray = K.Box(crest, "Ray", Quaternion.Euler(0f, 0f, ang) * new Vector3(0f, 0.2f, 0f), new Vector3(0.03f, 0.4f, 0.03f), K.Mats.Gold, false);
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
            K.Box(crest, "Base", new Vector3(0f, 0.02f, 0f), new Vector3(0.9f, 0.04f, 0.05f), K.Mats.Gold, false);
            K.Text3D(t, "Plaque", id.ToUpperInvariant(), new Vector3(0f, 0.3f, 0.07f), Vector3.forward, 0.08f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            // Real object on a pedestal, 1.2 m into the room
            var objRoot = K.Group(t, "Object", new Vector3(0f, 0f, 1.2f));
            K.Box(objRoot.transform, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f), K.Mats.BlackMetal);
            var prop = K.Group(objRoot.transform, "Prop");
            buildObject(prop.transform);
            var inspect = objRoot.AddComponent<InspectableObject>();
            inspect.Prompt = "Inspect";
            inspect.Note = note;
            inspect.InspectRoot = prop.transform;
            var bc = objRoot.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.3f, 0f);
            bc.size = new Vector3(0.8f, 0.9f, 0.8f);

            // "Reflection": copy placed mirror-symmetrically behind the pane. A real mirror flips depth
            // (local z); the impossible one is flipped left-right instead, so its handedness is wrong.
            var refRoot = K.Group(t, "Reflection", new Vector3(0f, 0f, -1.2f));
            K.Box(refRoot.transform, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f), K.Mats.BlackMetal, false);
            buildObject(refRoot.transform);
            foreach (var c in refRoot.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            refRoot.transform.localScale = impossible ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, 1f, -1f);
            K.PointLight(t, "NicheLight", new Vector3(0f, 2.3f, -0.9f), new Color(0.75f, 0.85f, 1f), 0.8f, 3f);

            var display = pane.AddComponent<MirrorDisplay>();
            display.Configure(id, impossible, refRoot.transform, pane.GetComponent<Renderer>(), puzzle);
            return display;
        }

        // Object builders work in the exhibit's local frame: +z toward the room, +x along the wall.
        private static void BuildClock(Transform r)
        {
            var face = K.Prim(r, "ClockFace", PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.55f, 0.03f, 0.55f), K.Mats.Paper, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            K.Prim(r, "ClockRim", PrimitiveType.Cylinder, new Vector3(0f, 1.3f, -0.02f), new Vector3(0.62f, 0.02f, 0.62f), K.Mats.Brass, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            K.Box(r, "Stand", new Vector3(0f, 1.0f, 0f), new Vector3(0.12f, 0.2f, 0.12f), K.Mats.Brass, false);
            K.Text3D(r, "Time", "7:14", new Vector3(0f, 1.3f, 0.025f), Vector3.forward, 0.06f, new Color(0.1f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            // one hand pointing to the viewer's left = asymmetric
            K.Box(r, "Hand", new Vector3(-0.14f, 1.42f, 0.03f), new Vector3(0.22f, 0.02f, 0.01f), K.Mats.BlackMetal, false).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        }

        private static void BuildPortrait(Transform r)
        {
            K.Box(r, "Frame", new Vector3(0f, 1.45f, 0f), new Vector3(0.72f, 0.92f, 0.06f), K.Mats.Brass, false);
            K.Box(r, "Canvas", new Vector3(0f, 1.45f, 0.035f), new Vector3(0.62f, 0.82f, 0.01f), K.Mats.Velvet, false);
            K.Prim(r, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0.1f), new Vector3(0.26f, 0.32f, 0.2f), K.Mats.Silhouette, false);
            K.Box(r, "Shoulders", new Vector3(0f, 1.2f, 0.08f), new Vector3(0.45f, 0.22f, 0.12f), K.Mats.Silhouette, false);
            // eyes offset so he looks slightly away
            K.Prim(r, "EyeL", PrimitiveType.Sphere, new Vector3(-0.02f, 1.53f, 0.2f), new Vector3(0.035f, 0.035f, 0.035f), K.Mats.Paper, false);
            K.Prim(r, "EyeR", PrimitiveType.Sphere, new Vector3(0.09f, 1.53f, 0.2f), new Vector3(0.035f, 0.035f, 0.035f), K.Mats.Paper, false);
            K.Text3D(r, "Name", "E. VOSS", new Vector3(0f, 1.1f, 0.045f), Vector3.forward, 0.05f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            K.Text3D(r, "Inscription", "Looking is automatic.\nObserving is a decision.", new Vector3(0f, 0.6f, 0.31f), Vector3.forward, 0.035f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Italic, 48);
        }

        private static void BuildStatue(Transform r)
        {
            K.Box(r, "Body", new Vector3(0f, 1.18f, 0f), new Vector3(0.26f, 0.56f, 0.2f), K.Mats.Paper, false);
            K.Prim(r, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.58f, 0f), new Vector3(0.2f, 0.2f, 0.2f), K.Mats.Paper, false);
            K.Box(r, "ArmDown", new Vector3(-0.17f, 1.15f, 0f), new Vector3(0.07f, 0.45f, 0.07f), K.Mats.Paper, false);
            K.Box(r, "ArmRaised", new Vector3(0.2f, 1.5f, 0f), new Vector3(0.07f, 0.5f, 0.07f), K.Mats.Paper, false);
        }

        private static void BuildPlant(Transform r)
        {
            K.Prim(r, "Pot", PrimitiveType.Cylinder, new Vector3(0f, 1.03f, 0f), new Vector3(0.32f, 0.13f, 0.32f), K.Mats.Wood, false);
            var stem = K.Box(r, "Stem", new Vector3(0.08f, 1.4f, 0f), new Vector3(0.03f, 0.55f, 0.03f), K.Mats.Accent, false);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            K.Prim(r, "Leaf1", PrimitiveType.Sphere, new Vector3(0.22f, 1.5f, 0f), new Vector3(0.3f, 0.07f, 0.14f), K.Mats.Accent, false).transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            K.Prim(r, "Leaf2", PrimitiveType.Sphere, new Vector3(0.18f, 1.34f, 0.06f), new Vector3(0.26f, 0.06f, 0.12f), K.Mats.Accent, false).transform.localRotation = Quaternion.Euler(10f, 0f, 20f);
            K.Prim(r, "Leaf3", PrimitiveType.Sphere, new Vector3(0.26f, 1.64f, -0.04f), new Vector3(0.22f, 0.05f, 0.1f), K.Mats.Accent, false).transform.localRotation = Quaternion.Euler(-10f, 0f, 30f);
        }

        private static void BuildGeometry(Transform r)
        {
            K.Box(r, "BarVertical", new Vector3(-0.15f, 1.2f, 0f), new Vector3(0.1f, 0.5f, 0.1f), K.Mats.Brass, false);
            K.Box(r, "BarHorizontal", new Vector3(0.05f, 1.0f, 0f), new Vector3(0.5f, 0.1f, 0.1f), K.Mats.Brass, false);
            K.Prim(r, "Sphere", PrimitiveType.Sphere, new Vector3(0.28f, 1.12f, 0f), new Vector3(0.14f, 0.14f, 0.14f), K.Mats.BlackMetal, false);
        }

        // ============================================================== reason

        private static ReasonPuzzle BuildReasonRoom(out List<Light> lights)
        {
            const float dx = 0f; // room-local frame
            var grp = rooms[1];
            var puzzleGo = K.Group(grp, "ReasonPuzzle");
            var puzzle = puzzleGo.AddComponent<ReasonPuzzle>();
            puzzle.SetPuzzleId("reason");

            // Balance scale
            var scaleRoot = K.Group(grp, "BalanceScale", new Vector3(dx, 0f, 17f));
            K.Prim(scaleRoot.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(1.3f, 0.08f, 1.3f), K.Mats.OldWood);
            K.Prim(scaleRoot.transform, "BaseTrim", PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f), new Vector3(0.9f, 0.02f, 0.9f), K.Mats.Brass, false);
            K.Box(scaleRoot.transform, "Column", new Vector3(0f, 1.0f, 0f), new Vector3(0.14f, 1.7f, 0.14f), K.Mats.Brass);
            var beamPivot = K.Group(scaleRoot.transform, "BeamPivot", new Vector3(0f, 1.9f, 0f));
            K.Box(beamPivot.transform, "Beam", Vector3.zero, new Vector3(2.6f, 0.07f, 0.07f), K.Mats.Brass, false);
            for (int s = -1; s <= 1; s += 2)
            {
                K.Box(beamPivot.transform, s < 0 ? "ChainL" : "ChainR", new Vector3(s * 1.2f, -0.35f, 0f), new Vector3(0.02f, 0.7f, 0.02f), K.Mats.BlackMetal, false);
                K.Prim(beamPivot.transform, s < 0 ? "PanL" : "PanR", PrimitiveType.Cylinder, new Vector3(s * 1.2f, -0.72f, 0f), new Vector3(0.5f, 0.02f, 0.5f), K.Mats.Brass, false);
                if (s < 0)
                {
                    var mask = K.Group(beamPivot.transform, "Mask", new Vector3(-1.2f, -0.64f, 0f));
                    K.Prim(mask.transform, "Face", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.26f, 0.14f, 0.3f), K.Mats.Paper);
                    K.Prim(mask.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.06f, 0.05f, 0.1f), new Vector3(0.05f, 0.03f, 0.05f), K.Mats.Silhouette, false);
                    K.Prim(mask.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.06f, 0.05f, 0.1f), new Vector3(0.05f, 0.03f, 0.05f), K.Mats.Silhouette, false);
                    var mi = mask.AddComponent<InspectableObject>(); mi.Prompt = "Inspect mask"; mi.Note = "A mask. It weighs exactly what it appears to.";
                }
                else
                {
                    var key = K.Group(beamPivot.transform, "Key", new Vector3(1.2f, -0.69f, 0f));
                    K.Box(key.transform, "Shaft", new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.03f, 0.3f), K.Mats.Brass);
                    K.Box(key.transform, "Bow", new Vector3(0f, 0f, 0.18f), new Vector3(0.09f, 0.03f, 0.08f), K.Mats.Brass, false);
                    K.Box(key.transform, "Bit", new Vector3(0.03f, 0f, -0.1f), new Vector3(0.04f, 0.03f, 0.06f), K.Mats.Brass, false);
                    var ki = key.AddComponent<InspectableObject>(); ki.Prompt = "Inspect key"; ki.Note = "A key. There is no lock in this room that fits it.";
                }
            }
            var scaleCtrl = scaleRoot.AddComponent<BalanceScaleController>();
            scaleCtrl.Configure(beamPivot.transform);

            // Black Box on its own pedestal
            var boxRoot = K.Group(grp, "BlackBox", new Vector3(dx + 3f, 0f, 19f));
            // Old wooden work table under the box (collider keeps the old footprint).
            K.Box(boxRoot.transform, "TableTop", new Vector3(0f, 0.77f, 0f), new Vector3(1.2f, 0.06f, 0.9f), K.Mats.OldWood, false);
            foreach (float lx in new[] { -0.52f, 0.52f })
                foreach (float lz in new[] { -0.38f, 0.38f })
                    K.Box(boxRoot.transform, "TableLeg", new Vector3(lx, 0.37f, lz), new Vector3(0.08f, 0.74f, 0.08f), K.Mats.OldWood, false);
            var tableCol = K.Group(boxRoot.transform, "TableCollider", new Vector3(0f, 0.4f, 0f)).AddComponent<BoxCollider>();
            tableCol.size = new Vector3(1.2f, 0.8f, 0.9f);
            var body = K.Box(boxRoot.transform, "Body", new Vector3(0f, 1.0f, 0f), new Vector3(0.6f, 0.4f, 0.6f), K.Mats.BlackMetal);
            // Iron bands, brass corners, an emblem and a keyhole that fits no key.
            foreach (float bx in new[] { -0.18f, 0.18f })
                K.Box(boxRoot.transform, "Band", new Vector3(bx, 1.0f, 0f), new Vector3(0.05f, 0.41f, 0.61f), K.Mats.Rust, false);
            foreach (float cx in new[] { -0.29f, 0.29f })
                foreach (float cy in new[] { 0.82f, 1.18f })
                    foreach (float cz in new[] { -0.29f, 0.29f })
                        K.Box(boxRoot.transform, "Corner", new Vector3(cx, cy, cz), new Vector3(0.05f, 0.05f, 0.05f), K.Mats.Brass, false);
            var emblem = K.Group(boxRoot.transform, "Emblem", new Vector3(0f, 1.02f, -0.305f)).transform;
            K.Prim(emblem, "Ring", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.16f, 0.005f, 0.16f), K.Mats.Brass, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            K.Prim(emblem, "RingInner", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.002f), new Vector3(0.13f, 0.005f, 0.13f), K.Mats.BlackMetal, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int e = 0; e < 3; e++)
            {
                float a0 = 90f + e * 120f, a1 = 90f + (e + 1) * 120f;
                Vector3 p0 = Quaternion.Euler(0f, 0f, a0) * Vector3.right * 0.05f, p1 = Quaternion.Euler(0f, 0f, a1) * Vector3.right * 0.05f;
                var side = K.Box(emblem, "Tri", (p0 + p1) * 0.5f + new Vector3(0f, 0f, -0.004f), new Vector3((p1 - p0).magnitude, 0.008f, 0.004f), K.Mats.Brass, false);
                side.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg);
            }
            K.Box(boxRoot.transform, "KeyPlate", new Vector3(0f, 0.9f, -0.305f), new Vector3(0.06f, 0.08f, 0.006f), K.Mats.Brass, false);
            K.Box(boxRoot.transform, "Keyhole", new Vector3(0f, 0.9f, -0.31f), new Vector3(0.012f, 0.03f, 0.004f), K.Mats.Silhouette, false);
            var lidPivot = K.Group(boxRoot.transform, "LidPivot", new Vector3(0f, 1.21f, 0.3f));
            K.Box(lidPivot.transform, "Lid", new Vector3(0f, 0.02f, -0.3f), new Vector3(0.62f, 0.04f, 0.62f), K.Mats.BlackMetal, false);
            K.Box(lidPivot.transform, "Lock", new Vector3(0f, -0.02f, -0.6f), new Vector3(0.08f, 0.08f, 0.04f), K.Mats.Brass, false);
            var bcard = K.Group(boxRoot.transform, "BACKWARDSCard", new Vector3(0f, 1.21f, 0f));
            K.Box(bcard.transform, "CardBody", Vector3.zero, new Vector3(0.18f, 0.004f, 0.11f), K.Mats.Card);
            K.Text3D(bcard.transform, "Text", "BACKWARDS", new Vector3(0f, 0.003f, 0f), Vector3.up, 0.025f, new Color(0.1f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, 48, Vector3.forward);
            bcard.AddComponent<EvidencePickup>().Configure(EvidenceIds.Backwards, "BACKWARDS. The box held a word, not a key.");
            var boxCtrl = body.AddComponent<BlackBoxController>();
            boxCtrl.Configure(lidPivot.transform, bcard);

            // Equation display on the north wall
            K.Box(grp, "EquationDisplay", new Vector3(dx, 2.4f, 22.82f), new Vector3(3.4f, 1.9f, 0.1f), K.Mats.Screen);
            K.Text3D(grp, "EquationText", "MASK + MASK = 10\nMASK + KEY = 8\nKEY + BLACK BOX = 7\n\nBLACK BOX = ?", new Vector3(dx, 2.4f, 22.74f), Vector3.back, 0.22f, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            // Answer terminal
            var term = K.Group(grp, "AnswerTerminal", new Vector3(dx - 3f, 0f, 19f));
            K.Box(term.transform, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1.0f, 0.5f), K.Mats.OldWood);
            var pad = K.Box(term.transform, "Keypad", new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.2f, 0.4f), K.Mats.Brass);
            K.Text3D(term.transform, "Label", "ENTER VALUE", new Vector3(0f, 1.205f, 0f), Vector3.up, 0.05f, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 48, Vector3.forward);
            pad.AddComponent<KeypadController>().Configure(puzzle, "Enter Code", "REASON", "What is the value of the BLACK BOX?", 2, "AN ASSUMPTION COSTS YOU.");

            lights = new List<Light>
            {
                K.PointLight(grp, "ReasonLight_A", new Vector3(dx - 2.5f, 3.6f, 15f), new Color(1f, 0.72f, 0.45f), 1.4f, 9f),
                K.PointLight(grp, "ReasonLight_B", new Vector3(dx + 2.5f, 3.6f, 20f), new Color(1f, 0.72f, 0.45f), 1.4f, 9f),
                K.PointLight(grp, "CorridorLight", new Vector3(dx, 3f, 9f), new Color(1f, 0.75f, 0.5f), 0.7f, 6f),
                K.PointLight(grp, "ScaleLight", new Vector3(dx, 3.2f, 17f), new Color(1f, 0.85f, 0.6f), 0.9f, 5f),
            };
            var feedback = K.PointLight(grp, "FeedbackLight", new Vector3(dx, 3.6f, 17f), Color.red, 0f, 14f);
            feedback.enabled = false;

            puzzle.Configure(scaleCtrl, boxCtrl, feedback);
            return puzzle;
        }

        // ============================================================== memory

        private static TerminalController BuildMemoryRoom(out List<Light> lights, out MemoryPuzzle puzzle)
        {
            const float dx = 0f; // room-local frame
            var grp = rooms[2];
            var puzzleGo = K.Group(grp, "MemoryPuzzle");
            puzzle = puzzleGo.AddComponent<MemoryPuzzle>();
            puzzle.SetPuzzleId("memory");

            var term = K.Group(grp, "FinalTerminal", new Vector3(dx, 0f, 17f));
            K.Box(term.transform, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1.0f, 0.5f), K.Mats.BlackMetal);
            var screenGroup = K.Group(term.transform, "Screen", new Vector3(0f, 1.45f, 0f));
            screenGroup.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            K.Box(screenGroup.transform, "ScreenBody", Vector3.zero, new Vector3(1.3f, 0.8f, 0.1f), K.Mats.Screen);
            var screenText = K.Text3D(screenGroup.transform, "ScreenText", "", new Vector3(0f, 0f, -0.055f), Vector3.back, 0.06f, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var terminal = screenGroup.AddComponent<TerminalController>();
            terminal.Configure(screenText, puzzle,
                new[] { "FINAL TEST", "YOU ALREADY HAVE EVERYTHING YOU NEED.", "WHAT WAS THE FIRST THING I GAVE YOU?" },
                "FINAL TEST", "WHAT WAS THE FIRST THING I GAVE YOU?", 4, "LOOK BACKWARD.");

            // The two cards, exhibited on opposite walls
            var readPlate = K.Group(grp, "READDisplay", new Vector3(dx - 4.05f, 1.6f, 15f));
            K.Box(readPlate.transform, "Plate", Vector3.zero, new Vector3(0.06f, 0.5f, 0.8f), K.Mats.Card);
            K.Text3D(readPlate.transform, "Text", "READ", new Vector3(0.035f, 0f, 0f), Vector3.right, 0.1f, new Color(0.1f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var ri = readPlate.AddComponent<InspectableObject>(); ri.Prompt = "Examine"; ri.Note = "READ. The first word he gave you here.";
            var backPlate = K.Group(grp, "BACKWARDSDisplay", new Vector3(dx + 4.05f, 1.6f, 19f));
            K.Box(backPlate.transform, "Plate", Vector3.zero, new Vector3(0.06f, 0.5f, 0.8f), K.Mats.Card);
            K.Text3D(backPlate.transform, "Text", "BACKWARDS", new Vector3(-0.035f, 0f, 0f), Vector3.left, 0.08f, new Color(0.1f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var bi = backPlate.AddComponent<InspectableObject>(); bi.Prompt = "Examine"; bi.Note = "BACKWARDS. The second word. There was never a key.";

            lights = new List<Light>
            {
                K.PointLight(grp, "MemoryLight_A", new Vector3(dx, 4f, 14f), new Color(0.8f, 0.82f, 0.85f), 0.8f, 10f),
                K.PointLight(grp, "MemoryLight_B", new Vector3(dx, 4f, 20f), new Color(0.8f, 0.82f, 0.85f), 0.8f, 10f),
                K.PointLight(grp, "CorridorLight", new Vector3(dx, 3f, 9f), new Color(0.55f, 0.7f, 1f), 0.7f, 6f),
                K.PointLight(grp, "TerminalLight", new Vector3(dx, 2.6f, 16.2f), new Color(0.7f, 0.8f, 1f), 0.7f, 4f),
            };
            return terminal;
        }

        // ========================================================== silhouette

        /// <summary>
        /// Coming out of PERCEPTION, the player sees him for the first time: a figure in the elevator
        /// mouth across the hall, watching, then walking away. The lamp above fails; writing remains.
        /// </summary>
        private static void BuildSilhouetteEvent(Light lamp)
        {
            var a = K.Group(root, "SilhouetteEvent").transform;
            var fig = K.Group(a, "VossSilhouette", new Vector3(0f, 0f, -6.2f));
            K.Prim(fig.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), K.Mats.Silhouette, false);
            K.Prim(fig.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.12f, 0.36f), K.Mats.Silhouette, false);
            K.Prim(fig.transform, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.02f, 0.5f), K.Mats.Silhouette, false);
            var start = K.Group(a, "WalkStart", new Vector3(0f, 0f, -6.2f));
            var end = K.Group(a, "WalkEnd", new Vector3(2.6f, 0f, -6.2f));
            var writing = K.Text3D(a, "WallWriting", "DID YOU SEE ME?\nOR DID YOU EXPECT TO?", new Vector3(0f, 4.05f, -6.83f), Vector3.forward, 0.16f, new Color(0.6f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var zone = K.TriggerBox(a, "SilhouetteTrigger", new Vector3(RoomOrigin[0].x, 1.2f, 5.4f), new Vector3(2.4f, 2.4f, 1.6f)).gameObject.AddComponent<PlayerTriggerZone>();
            var ev = a.gameObject.AddComponent<VossSilhouetteEvent>();
            ev.Configure(zone, fig.transform, start.transform, end.transform, writing.gameObject, lamp);
        }

        // ======================================================= final experiment

        private const float FinalH = 5f;
        private static readonly Vector3 ChamberAt = new Vector3(-23f, 0f, -2.5f);

        /// <summary>
        /// The theatre behind the hall's west wall (x -30..-16, z -7..2). Until MEMORY is solved it does
        /// not exist: its door is a section of the cracked wall, with no handle and no prompt. The finale
        /// raises it. Inside: the glass chamber on a round stage in the middle of the room, two screens
        /// with the recordings behind it, a few rows of seats, the principles on the north wall.
        /// </summary>
        private static DoorController BuildFinalRoom(out GameObject[] principles, out List<Light> lights, out Transform chamber, out GameObject chamberProjection)
        {
            var hall = root.Find("Entrance");
            var f = K.Group(root, "FinalExperiment").transform;
            var wall = K.Mats.Cracked;
            K.FloorAndCeiling(f, "Final", new Vector2(-30f, -7f), new Vector2(-16f, 2f), FinalH, K.Mats.Marble, K.Mats.NavyCeiling);
            K.Wall(f, "Final_N", true, 2f, -30f, -16f, FinalH, wall);
            K.Wall(f, "Final_W", false, -30f, -7f, 2f, FinalH, wall);
            K.Wall(f, "Final_S", true, -7f, -30f, -16f, FinalH, wall);

            // The hidden door: a slab of the hall wall itself (built in the hall's space so its texture
            // continues the wall's), a hair thinner so it can slide up into the wall above it.
            var mover = K.Group(hall, "HiddenDoorPanel", HiddenDoorAt).transform;
            var slab = K.SurfaceBox(hall, "WallSection", HiddenDoorAt + new Vector3(0f, 1.5f, 0f), new Vector3(0.26f, 3f, 2f), wall);
            slab.transform.SetParent(mover, true);
            // The controller sits apart from the slab, so looking at the wall shows no prompt.
            var doorGo = K.Group(hall, "HiddenDoor", HiddenDoorAt);
            var door = doorGo.AddComponent<DoorController>();
            door.Configure(mover, GameFlag.None, "", false, GameFlag.None, true, "HIDDEN DOOR");

            var burst = K.Dust(hall, "HiddenDoorDust", HiddenDoorAt + new Vector3(0.3f, 2.2f, 0f), new Vector3(0.4f, 1.5f, 2f), 0f, new Color(0.85f, 0.82f, 0.75f, 0.7f));
            var main = burst.main;
            main.loop = false;
            main.playOnAwake = false;
            main.prewarm = false;
            main.duration = 2f;
            main.startLifetime = 4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
            main.gravityModifier = 0.05f;
            burst.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 220), new ParticleSystem.Burst(0.6f, 120) });
            doorGo.AddComponent<HiddenDoorFx>().Configure(door, burst, new List<Light>());

            chamber = BuildGlassChamber(f, out chamberProjection);

            lights = new List<Light>
            {
                K.PointLight(f, "FinalLight_A", new Vector3(-18.6f, 4.5f, -3f), new Color(1f, 0.82f, 0.62f), 1.0f, 8f),
                K.PointLight(f, "FinalLight_B", new Vector3(-27.6f, 4.4f, -2.5f), Cold, 0.7f, 7f),
                K.SpotLight(f, "ChamberLight", ChamberAt + new Vector3(0f, 4.85f, 0f), ChamberAt + new Vector3(0f, 0.3f, 0f), new Color(0.75f, 0.85f, 1f), 5f, 7f, 50f),
                K.PointLight(f, "FinalRed_N", new Vector3(-18.5f, 3f, 1.3f), new Color(1f, 0.12f, 0.08f), 1.2f, 5f),
                K.PointLight(f, "FinalRed_S", new Vector3(-27.5f, 3f, -6.3f), new Color(1f, 0.12f, 0.08f), 1.0f, 5f),
            };

            // Three short rows of seats facing the chamber, an aisle down the middle from the door.
            for (int row = 0; row < 3; row++)
                foreach (float z in new[] { -6.2f, -5.4f, -4.6f, -1.55f, -0.75f, 0.05f, 0.85f })
                {
                    if (row == 1 && Mathf.Approximately(z, -0.75f)) continue; // one seat is missing
                    Seat(f, new Vector3(-19.8f + row * 1.2f, 0f, z));
                }

            // Curtains: either side of the screens, and along the side walls.
            foreach (float z in new[] { -6.55f, 1.55f })
                K.SurfaceBox(f, "Curtain_W", new Vector3(-29.75f, 2.5f, z), new Vector3(0.14f, 5f, 0.8f), K.Mats.Curtain, false);
            K.SurfaceBox(f, "Pelmet", new Vector3(-29.75f, 4.7f, -2.5f), new Vector3(0.18f, 0.6f, 9f), K.Mats.Curtain, false);
            foreach (float x in new[] { -28.8f, -26.2f, -19.8f, -17.2f })
            {
                K.SurfaceBox(f, "Curtain_N", new Vector3(x, 2.5f, 1.78f), new Vector3(0.9f, 5f, 0.14f), K.Mats.Curtain, false);
                K.SurfaceBox(f, "Curtain_S", new Vector3(x, 2.5f, -6.78f), new Vector3(0.9f, 5f, 0.14f), K.Mats.Curtain, false);
            }
            K.Box(f, "Trim_N", new Vector3(-23f, 4.2f, 1.83f), new Vector3(14f, 0.06f, 0.04f), K.Mats.Gold, false);
            K.Box(f, "Trim_S", new Vector3(-23f, 4.2f, -6.83f), new Vector3(14f, 0.06f, 0.04f), K.Mats.Gold, false);

            // The three principles, one under another on the north wall, in test order.
            var words = FinalInvestigationController.FinalWords;
            var color = new Color(0.9f, 0.85f, 0.7f);
            principles = new[]
            {
                K.Text3D(f, "Principle_Perception", words[0], new Vector3(-23f, 3.5f, 1.83f), Vector3.back, 0.2f, color, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject,
                K.Text3D(f, "Principle_Reason", words[1], new Vector3(-23f, 3.0f, 1.83f), Vector3.back, 0.2f, color, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject,
                K.Text3D(f, "Principle_Memory", words[2], new Vector3(-23f, 2.5f, 1.83f), Vector3.back, 0.2f, color, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject,
            };
            foreach (var p in principles) p.SetActive(false);
            return door;
        }

        private static void Seat(Transform parent, Vector3 at)
        {
            var seat = K.Group(parent, "Seat", at).transform;
            seat.localRotation = Quaternion.Euler(0f, -90f, 0f); // facing the chamber (-x)
            K.Box(seat, "Base", new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.44f, 0.5f), K.Mats.BlackMetal);
            K.SurfaceBox(seat, "Cushion", new Vector3(0f, 0.5f, 0.02f), new Vector3(0.52f, 0.12f, 0.48f), K.Mats.Velvet, false);
            K.SurfaceBox(seat, "Back", new Vector3(0f, 0.85f, 0.26f), new Vector3(0.52f, 0.7f, 0.1f), K.Mats.Velvet, false);
            K.Box(seat, "ArmL", new Vector3(-0.28f, 0.6f, 0f), new Vector3(0.05f, 0.08f, 0.5f), K.Mats.Brass, false);
            K.Box(seat, "ArmR", new Vector3(0.28f, 0.6f, 0f), new Vector3(0.05f, 0.08f, 0.5f), K.Mats.Brass, false);
        }

        /// <summary>
        /// The glass chamber of The Impossible Exit on its round stage. Empty, as it always was; the
        /// silhouette inside is only light, shown by the projector at the very end.
        /// </summary>
        private static Transform BuildGlassChamber(Transform f, out GameObject projection)
        {
            var c = K.Group(f, "GlassChamber", ChamberAt).transform;
            K.Prim(c, "Dais", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(3.4f, 0.15f, 3.4f), K.Mats.OldWood, false);
            K.Prim(c, "DaisRim", PrimitiveType.Cylinder, new Vector3(0f, 0.305f, 0f), new Vector3(3.46f, 0.01f, 3.46f), K.Mats.Gold, false);
            var daisCol = K.Group(c, "DaisCollider", new Vector3(0f, 0.15f, 0f)).AddComponent<BoxCollider>();
            daisCol.size = new Vector3(2.6f, 0.3f, 2.6f);

            const float y0 = 0.3f, h = 2.5f, hw = 0.75f;
            K.Box(c, "ChamberBase", new Vector3(0f, y0 + 0.04f, 0f), new Vector3(1.7f, 0.08f, 1.7f), K.Mats.Brass);
            K.Box(c, "ChamberTop", new Vector3(0f, y0 + h, 0f), new Vector3(1.7f, 0.1f, 1.7f), K.Mats.Brass);
            foreach (float x in new[] { -hw, hw })
                foreach (float z in new[] { -hw, hw })
                    K.Box(c, "Post", new Vector3(x, y0 + h * 0.5f, z), new Vector3(0.07f, h, 0.07f), K.Mats.Brass, false);
            K.Box(c, "Glass_E", new Vector3(hw, y0 + h * 0.5f, 0f), new Vector3(0.02f, h - 0.1f, 1.44f), K.Mats.Glass);
            K.Box(c, "Glass_W", new Vector3(-hw, y0 + h * 0.5f, 0f), new Vector3(0.02f, h - 0.1f, 1.44f), K.Mats.Glass);
            K.Box(c, "Glass_N", new Vector3(0f, y0 + h * 0.5f, hw), new Vector3(1.44f, h - 0.1f, 0.02f), K.Mats.Glass);
            K.Box(c, "Glass_S", new Vector3(0f, y0 + h * 0.5f, -hw), new Vector3(1.44f, h - 0.1f, 0.02f), K.Mats.Glass);
            K.Text3D(c, "Plaque", "THE IMPOSSIBLE EXIT", new Vector3(1.73f, 0.16f, 0f), Vector3.right, 0.06f, new Color(0.85f, 0.7f, 0.38f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            // Voss, made of projector light: a translucent glowing figure in hat and coat.
            var glow = K.Transparent("ProjectionGlow", new Color(0.6f, 0.78f, 1f, 0.6f), 0f);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", new Color(0.45f, 0.65f, 1f) * 0.9f);
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // else URP strips _EMISSION
            UnityEditor.EditorUtility.SetDirty(glow);
            var figure = K.Group(c, "ProjectedVoss", new Vector3(0f, y0, 0f));
            K.Prim(figure.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), glow, false);
            K.Prim(figure.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.12f, 0.36f), glow, false);
            K.Prim(figure.transform, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.02f, 0.5f), glow, false);
            foreach (var r in figure.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            figure.SetActive(false);
            projection = figure;
            return c;
        }

        /// <summary>
        /// Two screens on the west wall, either side of the chamber as seen from the seats: Camera A
        /// (the audience) and Camera B (stage left), fed from a miniature stage far below the lab.
        /// </summary>
        private static VideoInvestigationController BuildPerformanceScreens()
        {
            var f = root.Find("FinalExperiment");
            var group = K.Group(f, "PerformanceScreens");
            const float x = -29.8f;
            var gold = new Color(0.78f, 0.62f, 0.32f);
            var surfaces = new Renderer[2];
            var captions = new TextMesh[2];
            var titles = new[] { "CAMERA A — AUDIENCE", "CAMERA B — STAGE LEFT" };
            var zs = new[] { -4.8f, -0.2f };
            for (int i = 0; i < 2; i++)
            {
                float z = zs[i];
                K.Box(group.transform, "ScreenFrame_" + (i == 0 ? "A" : "B"), new Vector3(x, 2.7f, z), new Vector3(0.14f, 2.12f, 3.62f), K.Mats.BlackMetal);
                surfaces[i] = K.Quad(group.transform, "ScreenSurface_" + (i == 0 ? "A" : "B"), new Vector3(x + 0.08f, 2.7f, z), new Vector2(3.4f, 1.91f), Vector3.right, K.Mats.Screen).GetComponent<Renderer>();
                K.Text3D(group.transform, "ScreenTitle_" + (i == 0 ? "A" : "B"), titles[i], new Vector3(x + 0.1f, 3.95f, z), Vector3.right, 0.15f, gold, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
                captions[i] = K.Text3D(group.transform, "Caption_" + (i == 0 ? "A" : "B"), "", new Vector3(x + 0.1f, 1.38f, z), Vector3.right, 0.1f, gold, TextAnchor.MiddleCenter, FontStyle.Normal, 48);
                captions[i].GetComponent<WorldText>().FitWithin(new Vector2(3.4f, 0.42f));
            }
            K.Text3D(group.transform, "PerformanceTitle", "THE IMPOSSIBLE EXIT — RECONSTRUCTION", new Vector3(x + 0.1f, 4.4f, -2.5f), Vector3.right, 0.17f, gold, TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            // Diorama far below the lab, rendered only by its two cameras.
            var d = K.Group(root, "PerformanceDiorama", new Vector3(0f, -60f, 0f)).transform;
            K.Box(d, "StageFloor", new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f), K.Mats.Wood, false);
            K.Box(d, "Backdrop", new Vector3(0f, 2f, -4.6f), new Vector3(12f, 4f, 0.2f), K.Mats.Velvet, false);
            K.Box(d, "Chamber", new Vector3(0.5f, 1.2f, 0.4f), new Vector3(1.6f, 2.4f, 1.6f), K.Mats.DarkGlass, false);
            K.Box(d, "ChamberTop", new Vector3(0.5f, 2.42f, 0.4f), new Vector3(1.7f, 0.06f, 1.7f), K.Mats.Brass, false);
            K.Box(d, "ChamberBase", new Vector3(0.5f, 0.03f, 0.4f), new Vector3(1.8f, 0.06f, 1.8f), K.Mats.Brass, false);
            // The curtain of darkness: hides stage-left from the audience camera, not from camera B.
            K.Box(d, "DarkCurtain", new Vector3(-1.8f, 1.6f, -0.3f), new Vector3(2.8f, 3.2f, 0.05f), K.Mats.Silhouette, false);
            var projection = K.Quad(d, "ProjectedSilhouette", new Vector3(0.5f, 1.0f, 0.4f), new Vector2(0.8f, 1.9f), Vector3.forward, K.Mats.Projection);
            var voss = K.Group(d, "VossFigure", new Vector3(-3f, 0f, 0.5f));
            K.Prim(voss.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), K.Mats.Silhouette, false);
            K.Prim(voss.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.12f, 0.36f), K.Mats.Silhouette, false);
            K.Prim(voss.transform, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.02f, 0.5f), K.Mats.Silhouette, false);

            var stageLights = new List<Light>
            {
                K.SpotLight(d, "StageSpot_Chamber", new Vector3(0.5f, 3.6f, 3f), new Vector3(0.5f, 1.2f, 0.4f), new Color(1f, 0.95f, 0.85f), 18f, 12f, 45f),
                K.SpotLight(d, "StageSpot_Front", new Vector3(-2f, 3.6f, 3f), new Vector3(-1.5f, 1f, 0.5f), new Color(1f, 0.9f, 0.75f), 10f, 12f, 55f),
                K.PointLight(d, "BackstageGlow", new Vector3(-1.5f, 1.2f, -2.5f), new Color(0.45f, 0.55f, 0.8f), 1.1f, 6f),
            };

            var camA = DioramaCamera(d, "CameraA_Audience", new Vector3(0f, 1.7f, 6.5f), new Vector3(0.5f, 1.2f, 0.4f), 40f);
            var camB = DioramaCamera(d, "CameraB_Secondary", new Vector3(-4f, 1.8f, -4f), new Vector3(0.5f, 1.2f, 0.3f), 55f);

            // One controller for both screens: looking at either frame finds it as their parent.
            var ctrl = group.AddComponent<VideoInvestigationController>();
            ctrl.Configure(voss.transform, projection.GetComponent<Renderer>(), stageLights, camA, camB, surfaces[0], surfaces[1], captions[0], captions[1]);
            return ctrl;
        }

        private static Camera DioramaCamera(Transform parent, string name, Vector3 localPos, Vector3 localLookAt, float fov)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.LookAt(parent.TransformPoint(localLookAt));
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.enabled = false;
            return cam;
        }

        /// <summary>The last terminal, at the front of the seats, its face turned to the aisle.</summary>
        private static FinalQuestionTerminal BuildFinalQuestionTerminal()
        {
            var f = root.Find("FinalExperiment");
            // Beside the aisle, turned toward it, so nothing stands between the seats and the chamber.
            var t = K.Group(f, "FinalQuestionTerminal", new Vector3(-20.7f, 0f, -1.9f));
            t.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
            K.Box(t.transform, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 1.0f, 0.5f), K.Mats.BlackMetal);
            var screen = K.Group(t.transform, "Screen", new Vector3(0f, 1.3f, 0f));
            screen.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            K.Box(screen.transform, "ScreenBody", Vector3.zero, new Vector3(1.0f, 0.6f, 0.1f), K.Mats.Screen);
            K.Text3D(screen.transform, "ScreenText", "", new Vector3(0f, 0f, -0.055f), Vector3.back, 0.05f, new Color(0.85f, 0.3f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            return screen.AddComponent<FinalQuestionTerminal>();
        }
    }
}
