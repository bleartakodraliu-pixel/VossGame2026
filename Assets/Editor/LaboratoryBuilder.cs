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
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// 02_VossLaboratory: entrance hall with elevator and terminal, then PERCEPTION, REASON and MEMORY
    /// strung one after another along corridors with blind turns, so the player only ever sees the
    /// room they are in. A sealed corridor leads from MEMORY back to the hall for the finale: the
    /// performance screen and its off-screen diorama, the final question terminal and the final exit.
    /// </summary>
    public static class LaboratoryBuilder
    {
        private const float H = 5f;                    // hall height
        private const float RoomH = 4.5f;
        private const float CorrH = 3.2f;              // corridor height
        private const float ExitX = 2.8f;              // local x of the far-wall exit in PERCEPTION and REASON
        private const float WayBackX = 12f;            // the corridor back from MEMORY meets the hall here
        private static readonly string[] RoomNames = { "PERCEPTION", "REASON", "MEMORY" };

        // Every room is built in the same local frame: entrance door at z 7, corridor z 7..11,
        // room z 11..23, x -4.2..4.2. These placements chain the rooms without overlap.
        //   PERCEPTION  world x -12.2..-3.8, z 11..23   (entered from the hall)
        //   REASON      world x  -2.2.. 6.2, z 31..43   (corridors A, B)
        //   MEMORY      world x   7.8..16.2, z 31..43   (corridors C, D; entered from the north)
        //   way back    world x  10.8..13.2, z  7..31   (corridor E, opened by the finale)
        private static readonly Vector3[] RoomOrigin = { new Vector3(-8f, 0f, 0f), new Vector3(2f, 0f, 20f), new Vector3(12f, 0f, 54f) };
        private static readonly float[] RoomYaw = { 0f, 0f, 180f };

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
            BuildConnectors();

            var hallLights = BuildHallLights(out var emergencyLights);
            var toReason = RouteLights("Lights_ToReason", new Vector3(-5.2f, 2.9f, 25.2f), new Vector3(-1.2f, 2.9f, 25.8f), new Vector3(2f, 2.9f, 25.8f));
            var toMemory = RouteLights("Lights_ToMemory", new Vector3(4.8f, 2.9f, 45f), new Vector3(4.8f, 2.9f, 48.2f), new Vector3(8.6f, 2.9f, 48.2f), new Vector3(12f, 2.9f, 48.2f));
            var wayBack = RouteLights("Lights_WayBack", new Vector3(WayBackX, 2.9f, 28f), new Vector3(WayBackX, 2.9f, 21f), new Vector3(WayBackX, 2.9f, 14f), new Vector3(WayBackX, 2.9f, 9f));
            var entrance = BuildEntrance(hallLights, emergencyLights);
            var doors = BuildEntranceDoors();

            var perception = BuildPerceptionRoom();
            var reason = BuildReasonRoom(out var reasonLights);
            var memoryTerminal = BuildMemoryRoom(out var memoryLights, out var memoryPuzzle);

            var perceptionExit = BuildRoomExit(0, ExitX, "Sealed. The mirrors are not finished with you.", out var perceptionLamp);
            var reasonExit = BuildRoomExit(1, ExitX, "Sealed. The scale has not settled.", out var reasonLamp);
            var memoryExit = BuildRoomExit(2, 0f, "Sealed. The experiment is not over.", out _);
            var hallReturn = BuildHallReturnDoor();
            var principles = BuildPrinciples();

            BuildSilhouetteEvent(toReason[2]);
            var video = BuildPerformanceScreen();
            var finalQuestion = BuildFinalQuestionTerminal();
            var exitDoor = BuildFinalExit(out var exitSign, out var projectorLight);

            // MEMORY keypad, in the last bend before MEMORY's door.
            var keypad = K.Box(root, "MemoryDoorKeypad", new Vector3(13.0f, 1.4f, 48.4f), new Vector3(0.08f, 0.42f, 0.3f), K.Mats.Brass);
            K.Text3D(root, "KeypadLabel", "7 8 9\n4 5 6\n1 2 3\n  0", new Vector3(12.95f, 1.4f, 48.4f), Vector3.left, 0.05f, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            keypad.AddComponent<MemoryDoorController>().Configure(doors[2]);

            // Managers
            var managers = K.Group(root, "Managers");
            var puzzleManager = managers.AddComponent<PuzzleManager>();
            puzzleManager.Configure(perception, reason, memoryPuzzle);

            var reveal = managers.AddComponent<FinalRevealController>();
            var player = K.Player(root, new Vector3(0f, 0f, -8.6f), 0f);
            reveal.Configure(exitDoor, projectorLight, exitSign, player.GetComponent<Voss.Player.FirstPersonController>());
            finalQuestion.Configure(reveal, video, finalQuestion.GetComponentInChildren<TextMesh>());

            // The way on opens only after each test is solved and its card has been taken.
            K.Group(managers.transform, "Reveal_ToReason").AddComponent<DoorRevealSequence>().Configure(perceptionExit, GameFlag.ReasonUnlocked,
                EvidenceIds.Read, toReason, null, perceptionLamp, "A door you never noticed. Observe more carefully.");
            K.Group(managers.transform, "Reveal_ToMemory").AddComponent<DoorRevealSequence>().Configure(reasonExit, GameFlag.MemoryUnlocked,
                EvidenceIds.Backwards, toMemory, reasonLights, reasonLamp, "Keep walking. Do not look behind you.");

            var finale = managers.AddComponent<FinalInvestigationController>();
            finale.Configure(perception, reasonLights, memoryLights, hallLights, principles[0], principles[1], principles[2], video, finalQuestion,
                new List<DoorController> { memoryExit, hallReturn }, wayBack);
            memoryPuzzle.Configure(memoryTerminal, memoryLights, finale);

            managers.AddComponent<GameRestartController>();
            entrance.transform.SetParent(managers.transform, true);

            BuildAtmosphere(toMemory);
            LaboratoryProps.Build(root, rooms);
            LaboratoryDressing.Build(root, rooms);
        }

        // ========================================================== atmosphere

        /// <summary>
        /// Everything that makes the place feel inhabited by something: unreliable lamps, hard shadows,
        /// dust in the air, room tone and distant noises, whispers in MEMORY, and two scripted scares
        /// on the way (lamps dying behind the player, applause where there is no audience).
        /// </summary>
        private static void BuildAtmosphere(List<Light> toMemory)
        {
            // Hard shadows from a few key lamps only (shadowed point lights are expensive).
            foreach (var path in new[] { "HallLights/HallLight_W", "HallLights/HallLight_E", "PERCEPTIONRoom/PerceptionLight_A", "REASONRoom/ScaleLight", "MEMORYRoom/MemoryLight_A" })
            {
                var l = root.Find(path)?.GetComponent<Light>();
                if (l != null) l.shadows = LightShadows.Soft;
            }

            // Unreliable lamps.
            Flicker("HallLights/HallLight_N", 0.1f, new Vector2(5f, 12f), 0.5f);
            Flicker("HallLights/VestibuleLight", 0.15f, new Vector2(3f, 9f), 0.4f);
            Flicker("Lights_ToReason/Lamp_1", 0.12f, new Vector2(4f, 10f), 0.35f);
            Flicker("Lights_WayBack/Lamp_1", 0.12f, new Vector2(3f, 8f), 0.5f);
            Flicker("Lights_WayBack/Lamp_3", 0.1f, new Vector2(5f, 11f), 0.3f);
            Flicker("REASONRoom/ReasonLight_A", 0.06f, new Vector2(10f, 20f), 0.2f);
            Flicker("MEMORYRoom/MemoryLight_B", 0.12f, new Vector2(4f, 10f), 0.45f);

            // Dust hanging in the air.
            var dust = new Color(0.9f, 0.88f, 0.8f, 0.25f);
            K.Dust(root.Find("Entrance"), "Dust", new Vector3(0f, 2.5f, 0f), new Vector3(30f, 4.5f, 13f), 14f, dust);
            for (int i = 0; i < 3; i++) K.Dust(rooms[i], "Dust", new Vector3(0f, 2.2f, 17f), new Vector3(8f, 4f, 11f), 6f, dust);

            // Room tone everywhere, with drips, creaks, knocks and steps somewhere out of sight.
            K.Group(root, "Soundscape").AddComponent<AmbientSoundscape>().Configure(
                new[] { SfxId.Drip, SfxId.Drip, SfxId.Creak, SfxId.Creak, SfxId.Knock, SfxId.DistantSteps },
                new Vector2(9f, 22f), new Vector2(4f, 9f), 1f, true, 0.3f);

            // MEMORY whispers to whoever stands in it.
            var whisperZone = K.TriggerBox(rooms[2], "WhisperZone", new Vector3(0f, 2f, 17f), new Vector3(8.4f, 4f, 12f));
            whisperZone.gameObject.AddComponent<AmbientSoundscape>().Configure(new[] { SfxId.Whisper }, new Vector2(6f, 14f), new Vector2(1.5f, 3f), 1f);

            var scares = K.Group(root, "Scares").transform;

            // Turning into D: the lamps behind the player die one by one, then footsteps where they were.
            var stepsAt = K.Group(scares, "StepsBehind", new Vector3(4.8f, 1f, 44.5f));
            K.TriggerBox(scares, "LampsDieBehind", new Vector3(6.8f, 1.2f, 48.2f), new Vector3(1.4f, 2.4f, 2.4f)).gameObject
                .AddComponent<ScareTrigger>().Configure(GameFlag.MemoryUnlocked, new List<Light> { toMemory[0], toMemory[1] },
                    true, SfxId.DistantSteps, stepsAt.transform, 1f, null);

            // The way back: applause from the dark ahead, for a performance nobody is watching.
            var applauseAt = K.Group(scares, "ApplauseAhead", new Vector3(WayBackX, 2f, 25f));
            K.TriggerBox(scares, "ApplauseTrigger", new Vector3(WayBackX, 1.2f, 29f), new Vector3(2.2f, 2.4f, 1.6f)).gameObject
                .AddComponent<ScareTrigger>().Configure(GameFlag.MetaCodeSolved, null, true, SfxId.Applause, applauseAt.transform, 1f, null);
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
            // North wall: PERCEPTION's door, and the unmarked door the way back will use
            K.WallWithGaps(hall, "Wall_North", true, 7f, -16f, 16f, H, wall,
                new Vector3(RoomOrigin[0].x, 2f, 3f), new Vector3(WayBackX, 2f, 3f));
            // West wall with final exit gap
            K.Wall(hall, "Wall_West", false, -16f, -7f, 7f, H, wall, -3f, 2f, 3f);
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

        /// <summary>The three rooms, each in its own placed and rotated group (see RoomOrigin).</summary>
        private static void BuildRoomShells()
        {
            for (int i = 0; i < 3; i++)
            {
                var grp = K.Group(root, RoomNames[i] + "Room", RoomOrigin[i]).transform;
                grp.localRotation = Quaternion.Euler(0f, RoomYaw[i], 0f);
                rooms[i] = grp;

                // Entrance corridor z 7..11. PERCEPTION's mouth is the hall wall; the others get a doorway wall.
                K.FloorAndCeiling(grp, "Corridor", new Vector2(-1.2f, 7f), new Vector2(1.2f, 11f), CorrH, K.Mats.WetFloor, K.Mats.ConcreteCeiling);
                K.Wall(grp, "Corridor_W", false, -1.2f, 7f, 11f, CorrH, K.Mats.Cracked);
                K.Wall(grp, "Corridor_E", false, 1.2f, 7f, 11f, CorrH, K.Mats.Cracked);
                if (i > 0) K.Wall(grp, "Doorway", true, 7f, -1.2f, 1.2f, CorrH, K.Mats.Cracked, 0f, 2f, 3f);

                // Room z 11..23. PERCEPTION is an art-deco mirror hall, REASON an old study, MEMORY raw concrete.
                var wall = i == 0 ? K.Mats.Navy : i == 1 ? K.Mats.WarmPlaster : K.Mats.Cracked;
                var floor = i == 0 ? K.Mats.Marble : i == 1 ? K.Mats.StoneFloor : K.Mats.WetFloor;
                var ceiling = i == 0 ? K.Mats.NavyCeiling : K.Mats.ConcreteCeiling;
                K.FloorAndCeiling(grp, "Room", new Vector2(-4.2f, 11f), new Vector2(4.2f, 23f), RoomH, floor, ceiling);
                K.Wall(grp, "Room_South", true, 11f, -4.2f, 4.2f, RoomH, wall, 0f, 2.4f, 3.2f);
                if (i == 0)
                {
                    // Perception: side walls carry mirror niches at z 14 and 20; the far wall the statue niche and the exit.
                    var niches = new[] { new Vector3(14f, 1.8f, 2.6f), new Vector3(20f, 1.8f, 2.6f) };
                    K.WallWithGaps(grp, "Room_W", false, -4.2f, 11f, 23f, RoomH, wall, niches);
                    K.WallWithGaps(grp, "Room_E", false, 4.2f, 11f, 23f, RoomH, wall, niches);
                    K.WallWithGaps(grp, "Room_North", true, 23f, -4.2f, 4.2f, RoomH, wall,
                        new Vector3(0f, 1.8f, 2.6f), new Vector3(ExitX, 2f, 3f));
                }
                else
                {
                    K.Wall(grp, "Room_W", false, -4.2f, 11f, 23f, RoomH, wall);
                    K.Wall(grp, "Room_E", false, 4.2f, 11f, 23f, RoomH, wall);
                    K.Wall(grp, "Room_North", true, 23f, -4.2f, 4.2f, RoomH, wall, i == 1 ? ExitX : 0f, 2f, 3f);
                }
            }
        }

        /// <summary>
        /// World-space corridors between the rooms. Each bend is a dead-end wall straight ahead,
        /// so nothing past the next turn is visible from a doorway.
        /// </summary>
        private static void BuildConnectors()
        {
            var c = K.Group(root, "Connectors").transform;
            var m = K.Mats.Cracked;

            // A: out of PERCEPTION's far wall, north, then the turn east.
            Slab(c, "A", -6.4f, -4.0f, 23f, 27f);
            K.Wall(c, "A_W", false, -6.4f, 23f, 27f, CorrH, m);
            K.Wall(c, "A_N", true, 27f, -6.4f, -4.0f, CorrH, m);
            K.Wall(c, "A_E", false, -4.0f, 23f, 24.6f, CorrH, m);
            // B: east to REASON's door (in the north side of the last bend).
            Slab(c, "B", -4.0f, 3.2f, 24.6f, 27f);
            K.Wall(c, "B_S", true, 24.6f, -4.0f, 3.2f, CorrH, m);
            K.Wall(c, "B_N", true, 27f, -4.0f, 0.8f, CorrH, m);
            K.Wall(c, "B_E", false, 3.2f, 24.6f, 27f, CorrH, m);
            // C: out of REASON's far wall, north, then the turn east.
            Slab(c, "C", 3.6f, 6.0f, 43f, 49.4f);
            K.Wall(c, "C_W", false, 3.6f, 43f, 49.4f, CorrH, m);
            K.Wall(c, "C_N", true, 49.4f, 3.6f, 6.0f, CorrH, m);
            K.Wall(c, "C_E", false, 6.0f, 43f, 47f, CorrH, m);
            // D: east to MEMORY's door (in the south side of the last bend), keypad on the end wall.
            Slab(c, "D", 6.0f, 13.2f, 47f, 49.4f);
            K.Wall(c, "D_N", true, 49.4f, 6.0f, 13.2f, CorrH, m);
            K.Wall(c, "D_E", false, 13.2f, 47f, 49.4f, CorrH, m);
            K.Wall(c, "D_S", true, 47f, 6.0f, 10.8f, CorrH, m);
            // E: the way back, from MEMORY's sealed exit straight down to the hall.
            Slab(c, "E", WayBackX - 1.2f, WayBackX + 1.2f, 7f, 31f);
            K.Wall(c, "E_W", false, WayBackX - 1.2f, 7f, 31f, CorrH, m);
            K.Wall(c, "E_E", false, WayBackX + 1.2f, 7f, 31f, CorrH, m);
        }

        private static void Slab(Transform parent, string name, float x0, float x1, float z0, float z1)
        {
            K.FloorAndCeiling(parent, name, new Vector2(x0, z0), new Vector2(x1, z1), CorrH, K.Mats.WetFloor, K.Mats.ConcreteCeiling);
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
                K.PointLight(g, "VestibuleLight", new Vector3(-17.5f, 2.8f, -3f), Cold, 0.6f, 5f),
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

        /// <summary>Ceiling lamps along a corridor. They stay dark until the corridor is revealed.</summary>
        private static List<Light> RouteLights(string name, params Vector3[] positions)
        {
            var g = K.Group(root, name).transform;
            var list = new List<Light>();
            for (int i = 0; i < positions.Length; i++)
            {
                list.Add(K.PointLight(g, "Lamp_" + i, positions[i], Cold, 0.6f, 6f));
            }
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

        private static DoorController[] BuildEntranceDoors()
        {
            var doors = new DoorController[3];
            var flags = new[] { GameFlag.PerceptionUnlocked, GameFlag.ReasonUnlocked, GameFlag.None };
            var releases = new[] { GameFlag.PerceptionSolved, GameFlag.ReasonSolved, GameFlag.MetaCodeSolved };
            var lockedMsgs = new[] { "Locked.", "Sealed. Complete PERCEPTION first.", "Sealed. Use the keypad beside the door." };

            for (int i = 0; i < 3; i++)
            {
                var grp = rooms[i];
                // PERCEPTION's door sits in the tall hall wall; the others sit under a corridor ceiling.
                var door = MakeDoor(grp, "Door_" + RoomNames[i], new Vector3(0f, 0f, 7f), RoomNames[i], i == 0);
                door.Configure(door.transform.Find("Panel"), flags[i], lockedMsgs[i], true, releases[i], i == 2, RoomNames[i]);

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

        /// <summary>Far-wall door that leads on. Manual: a reveal sequence or the finale opens it.</summary>
        private static DoorController BuildRoomExit(int i, float localX, string lockedMessage, out Light lamp)
        {
            var door = MakeDoor(rooms[i], "Exit_" + RoomNames[i], new Vector3(localX, 0f, 23f), null, true);
            door.Configure(door.transform.Find("Panel"), GameFlag.None, lockedMessage, false, GameFlag.None, true, RoomNames[i] + " EXIT");
            K.Box(door.transform, "LampHousing", new Vector3(0f, 3.5f, -0.2f), new Vector3(0.3f, 0.12f, 0.12f), K.Mats.BlackMetal, false);
            lamp = null;
            if (i == 2)
            {
                // MEMORY's far door looks like the way out. It is, eventually.
                var sign = K.Box(door.transform, "ExitSign", new Vector3(0f, 3.75f, -0.2f), new Vector3(0.9f, 0.3f, 0.08f), K.Mats.SignRed, false);
                K.Text3D(door.transform, "ExitSignText", "EXIT", new Vector3(0f, 3.75f, -0.25f), Vector3.back, 0.16f, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
                K.PointLight(sign.transform.parent, "ExitGlow", new Vector3(0f, 3.5f, -0.6f), new Color(1f, 0.15f, 0.1f), 0.7f, 3.5f);
            }
            else
            {
                lamp = K.PointLight(door.transform, "DoorLamp", new Vector3(0f, 3.35f, -0.4f), new Color(1f, 0.12f, 0.08f), 0.9f, 3.5f);
            }
            return door;
        }

        private static DoorController BuildHallReturnDoor()
        {
            var door = MakeDoor(root.Find("Entrance"), "Door_WayBack", new Vector3(WayBackX, 0f, 7f), null, true);
            door.Configure(door.transform.Find("Panel"), GameFlag.None, "It does not open from this side.", false, GameFlag.None, true, "WAY BACK");
            K.Box(door.transform, "LampHousing", new Vector3(0f, 3.5f, -0.2f), new Vector3(0.3f, 0.12f, 0.12f), K.Mats.BlackMetal, false);
            K.PointLight(door.transform, "DoorLamp", new Vector3(0f, 3.35f, -0.4f), new Color(1f, 0.12f, 0.08f), 0.6f, 3f);
            return door;
        }

        /// <summary>The three principles, written along the way back. Walking south they read in test order.</summary>
        private static GameObject[] BuildPrinciples()
        {
            var e = root.Find("Connectors");
            var words = FinalInvestigationController.FinalWords;
            var color = new Color(0.9f, 0.85f, 0.7f);
            var result = new[]
            {
                K.Text3D(e, "Principle_Perception", words[0], new Vector3(WayBackX - 1.03f, 2.1f, 26f), Vector3.right, 0.22f, color, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject,
                K.Text3D(e, "Principle_Reason", words[1], new Vector3(WayBackX + 1.03f, 2.1f, 19f), Vector3.left, 0.22f, color, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject,
                K.Text3D(e, "Principle_Memory", words[2], new Vector3(WayBackX - 1.03f, 2.1f, 12f), Vector3.right, 0.22f, color, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject,
            };
            foreach (var p in result) p.SetActive(false);
            return result;
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

        /// <summary>Waits at the far end of corridor B, in front of REASON's door; the trigger is the turn out of A.</summary>
        private static void BuildSilhouetteEvent(Light lamp)
        {
            var a = K.Group(root, "SilhouetteEvent").transform;
            var fig = K.Group(a, "VossSilhouette", new Vector3(2f, 0f, 25.3f));
            K.Prim(fig.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), K.Mats.Silhouette, false);
            K.Prim(fig.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.12f, 0.36f), K.Mats.Silhouette, false);
            K.Prim(fig.transform, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.02f, 0.5f), K.Mats.Silhouette, false);
            var start = K.Group(a, "WalkStart", new Vector3(2f, 0f, 25.3f));
            var end = K.Group(a, "WalkEnd", new Vector3(2f, 0f, 26.6f));
            var writing = K.Text3D(a, "WallWriting", "DID YOU SEE ME?\nOR DID YOU EXPECT TO?", new Vector3(3.03f, 1.9f, 25.8f), Vector3.left, 0.16f, new Color(0.6f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var zone = K.TriggerBox(a, "SilhouetteTrigger", new Vector3(-2.9f, 1.2f, 25.8f), new Vector3(1.4f, 2.4f, 2.4f)).gameObject.AddComponent<PlayerTriggerZone>();
            var ev = a.gameObject.AddComponent<VossSilhouetteEvent>();
            ev.Configure(zone, fig.transform, start.transform, end.transform, writing.gameObject, lamp);
        }

        // ============================================================== finale

        private static VideoInvestigationController BuildPerformanceScreen()
        {
            var hall = root.Find("Entrance");
            var frame = K.Box(hall, "PerformanceScreen", new Vector3(-9f, 2.5f, -6.85f), new Vector3(4.8f, 2.9f, 0.14f), K.Mats.BlackMetal);
            var screen = K.Quad(hall, "PerformanceScreenSurface", new Vector3(-9f, 2.5f, -6.76f), new Vector2(4.5f, 2.55f), Vector3.forward, K.Mats.Screen);
            var caption = K.Text3D(hall, "PerformanceCaption", "", new Vector3(-9f, 0.85f, -6.76f), Vector3.forward, 0.12f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Normal, 48);
            K.Text3D(hall, "PerformanceTitle", "THE IMPOSSIBLE EXIT — RECONSTRUCTION", new Vector3(-9f, 4.15f, -6.76f), Vector3.forward, 0.18f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);

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

            var ctrl = frame.AddComponent<VideoInvestigationController>();
            ctrl.Configure(voss.transform, projection.GetComponent<Renderer>(), stageLights, camA, camB, screen.GetComponent<Renderer>(), caption);
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

        private static FinalQuestionTerminal BuildFinalQuestionTerminal()
        {
            var hall = root.Find("Entrance");
            var t = K.Group(hall, "FinalQuestionTerminal", new Vector3(-11.5f, 0f, -5.4f));
            K.Box(t.transform, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 1.0f, 0.5f), K.Mats.BlackMetal);
            var screen = K.Group(t.transform, "Screen", new Vector3(0f, 1.4f, 0f));
            screen.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            K.Box(screen.transform, "ScreenBody", Vector3.zero, new Vector3(1.1f, 0.7f, 0.1f), K.Mats.Screen);
            K.Text3D(screen.transform, "ScreenText", "", new Vector3(0f, 0f, -0.055f), Vector3.back, 0.05f, new Color(0.85f, 0.3f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var terminal = screen.AddComponent<FinalQuestionTerminal>();
            return terminal;
        }

        private static DoorController BuildFinalExit(out GameObject exitSign, out Light projectorLight)
        {
            var hall = root.Find("Entrance");
            var v = K.Group(root, "FinalExit").transform;
            // Vestibule behind the west wall gap (z -4..-2)
            K.FloorAndCeiling(v, "Vestibule", new Vector2(-19.5f, -4f), new Vector2(-16f, -2f), 3.2f, K.Mats.WetFloor, K.Mats.ConcreteCeiling);
            K.Wall(v, "Vestibule_N", true, -2f, -19.5f, -16f, 3.2f, K.Mats.Cracked);
            K.Wall(v, "Vestibule_S", true, -4f, -19.5f, -16f, 3.2f, K.Mats.Cracked);
            K.Wall(v, "Vestibule_W", false, -19.5f, -4f, -2f, 3.2f, K.Mats.Cracked);

            var doorRoot = K.Group(v, "Door_FinalExit", new Vector3(-16f, 0f, -3f));
            var panel = K.Box(doorRoot.transform, "Panel", new Vector3(0f, 1.5f, 0f), new Vector3(0.25f, 3f, 2f), K.Mats.BlackMetal);
            K.Box(doorRoot.transform, "Frame_A", new Vector3(0f, 1.6f, -1.1f), new Vector3(0.4f, 3.2f, 0.2f), K.Mats.Brass);
            K.Box(doorRoot.transform, "Frame_B", new Vector3(0f, 1.6f, 1.1f), new Vector3(0.4f, 3.2f, 0.2f), K.Mats.Brass);
            var door = doorRoot.AddComponent<DoorController>();
            door.Configure(panel.transform, GameFlag.FinalExitUnlocked, "Sealed. The experiment is not over.", false, GameFlag.None, true, "FINAL EXIT");

            exitSign = K.Group(doorRoot.transform, "ExitSign", new Vector3(0.3f, 3.5f, 0f));
            K.Box(exitSign.transform, "SignBody", Vector3.zero, new Vector3(0.08f, 0.3f, 0.9f), K.Mats.SignRed, false);
            K.Text3D(exitSign.transform, "Text", "EXIT", new Vector3(0.05f, 0f, 0f), Vector3.right, 0.08f, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 48);

            var exitZone = K.TriggerBox(v, "TheatreExitTrigger", new Vector3(-18.6f, 1.2f, -3f), new Vector3(1.4f, 2.4f, 1.8f));
            exitZone.gameObject.AddComponent<SceneExitTrigger>().Configure(SceneNames.Theatre, GameFlag.FinalExitUnlocked);

            projectorLight = K.SpotLight(hall, "RevealProjector", new Vector3(-3f, 4.6f, 5f), new Vector3(-9f, 2.5f, -6.8f), new Color(0.9f, 0.95f, 1f), 6f, 20f, 30f);
            return door;
        }
    }
}
