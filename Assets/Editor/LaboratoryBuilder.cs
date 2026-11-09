using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voss.Core;
using Voss.Interaction;
using Voss.Narrative;
using Voss.Puzzles;
using K = Voss.EditorTools.VossBuildKit;

namespace Voss.EditorTools
{
    /// <summary>
    /// 02_VossLaboratory: entrance hall with elevator and terminal, three rooms (PERCEPTION, REASON,
    /// MEMORY) behind labelled doors, the silhouette corridor, the performance screen and its
    /// off-screen diorama, the final question terminal and the final exit.
    /// </summary>
    public static class LaboratoryBuilder
    {
        private const float H = 5f;                    // hall height
        private const float RoomH = 4.5f;
        private static readonly float[] DoorX = { -10f, 0f, 10f };
        private static readonly string[] RoomNames = { "PERCEPTION", "REASON", "MEMORY" };

        private static Transform root;

        public static void Build(Scene scene)
        {
            K.DarkAmbient(new Color(0.03f, 0.035f, 0.045f));
            root = K.Group(null, "VossLaboratory").transform;

            BuildHallShell();
            BuildCorridorsAndRoomShells();
            BuildAlcove();

            var hallLights = BuildHallLights(out var emergencyLights);
            var entrance = BuildEntrance(hallLights, emergencyLights);
            var doors = BuildDoors();

            var perception = BuildPerceptionRoom(out var perceptionPrinciple);
            var reason = BuildReasonRoom(out var reasonLights, out var reasonPrinciple);
            var memoryTerminal = BuildMemoryRoom(out var memoryLights, out var memoryPrinciple, out var memoryPuzzle);
            BuildSilhouetteEvent();
            var video = BuildPerformanceScreen();
            var finalQuestion = BuildFinalQuestionTerminal();
            var exitDoor = BuildFinalExit(out var exitSign, out var projectorLight);

            // Memory door keypad
            var keypad = K.Box(root, "MemoryDoorKeypad", new Vector3(11.5f, 1.4f, 6.8f), new Vector3(0.3f, 0.42f, 0.08f), K.Mats.Brass);
            K.Text3D(root, "KeypadLabel", "7 8 9\n4 5 6\n1 2 3\n  0", new Vector3(11.5f, 1.4f, 6.75f), Vector3.back, 0.05f, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            keypad.AddComponent<MemoryDoorController>().Configure(doors[2]);

            // Managers
            var managers = K.Group(root, "Managers");
            var puzzleManager = managers.AddComponent<PuzzleManager>();
            puzzleManager.Configure(perception, reason, memoryPuzzle);

            var reveal = managers.AddComponent<FinalRevealController>();
            var player = K.Player(root, new Vector3(0f, 0f, -8.6f), 0f);
            reveal.Configure(exitDoor, projectorLight, exitSign, player.GetComponent<Voss.Player.FirstPersonController>());
            finalQuestion.Configure(reveal, video, finalQuestion.GetComponentInChildren<TextMesh>());

            var finale = managers.AddComponent<FinalInvestigationController>();
            finale.Configure(perception, reasonLights, memoryLights, hallLights, perceptionPrinciple, reasonPrinciple, memoryPrinciple, video, finalQuestion, doors[2]);
            memoryPuzzle.Configure(memoryTerminal, memoryLights, finale);

            managers.AddComponent<GameRestartController>();
            entrance.transform.SetParent(managers.transform, true);
        }

        // ================================================================ shell

        private static void BuildHallShell()
        {
            var hall = K.Group(root, "Entrance").transform;
            K.FloorAndCeiling(hall, "Hall", new Vector2(-16f, -7f), new Vector2(16f, 7f), H, K.Mats.Floor, K.Mats.Ceiling);
            // South wall with elevator gap
            K.Wall(hall, "Wall_South", true, -7f, -16f, 16f, H, K.Mats.Wall, 0f, 3f, 3f);
            // North wall with three door gaps
            K.Wall(hall, "Wall_North_W", true, 7f, -16f, -5f, H, K.Mats.Wall, -10f, 2f, 3f);
            K.Wall(hall, "Wall_North_C", true, 7f, -5f, 5f, H, K.Mats.Wall, 0f, 2f, 3f);
            K.Wall(hall, "Wall_North_E", true, 7f, 5f, 16f, H, K.Mats.Wall, 10f, 2f, 3f);
            // West wall with final exit gap
            K.Wall(hall, "Wall_West", false, -16f, -7f, 7f, H, K.Mats.Wall, -3f, 2f, 3f);
            // East wall with alcove opening
            K.Wall(hall, "Wall_East", false, 16f, -7f, 7f, H, K.Mats.Wall, -4.5f, 3f, 3.4f);

            // Brass trim line and structural columns for scale
            foreach (float cx in new[] { -14f, -4f, 8f, 14f })
                K.Box(hall, "Column_S", new Vector3(cx, H * 0.5f, -6.75f), new Vector3(0.4f, H, 0.4f), K.Mats.BlackMetal);
            foreach (float cx in new[] { -15f, -5f, 5f, 15f })
                K.Box(hall, "Column_N", new Vector3(cx, H * 0.5f, 6.75f), new Vector3(0.4f, H, 0.4f), K.Mats.BlackMetal);
            K.Box(hall, "TrimNorth", new Vector3(0f, 3.3f, 6.82f), new Vector3(32f, 0.06f, 0.06f), K.Mats.Brass, false);
            K.Box(hall, "TrimSouth", new Vector3(0f, 3.3f, -6.82f), new Vector3(32f, 0.06f, 0.06f), K.Mats.Brass, false);

            // Elevator car
            var elev = K.Group(hall, "Elevator").transform;
            K.FloorAndCeiling(elev, "Elevator", new Vector2(-1.5f, -10f), new Vector2(1.5f, -7f), 3f, K.Mats.BlackMetal, K.Mats.BlackMetal);
            K.Wall(elev, "Elevator_Back", true, -10f, -1.5f, 1.5f, 3f, K.Mats.BlackMetal);
            K.Wall(elev, "Elevator_W", false, -1.5f, -10f, -7f, 3f, K.Mats.BlackMetal);
            K.Wall(elev, "Elevator_E", false, 1.5f, -10f, -7f, 3f, K.Mats.BlackMetal);
            K.Box(elev, "ElevatorLamp", new Vector3(0f, 2.9f, -8.5f), new Vector3(0.6f, 0.05f, 0.6f), K.Mats.SignRed, false);
        }

        private static void BuildCorridorsAndRoomShells()
        {
            for (int i = 0; i < 3; i++)
            {
                float dx = DoorX[i];
                var grp = K.Group(root, RoomNames[i] + "Room").transform;
                // corridor z 7..11
                K.FloorAndCeiling(grp, "Corridor", new Vector2(dx - 1.2f, 7f), new Vector2(dx + 1.2f, 11f), 3.2f, K.Mats.Floor, K.Mats.Ceiling);
                K.Wall(grp, "Corridor_W", false, dx - 1.2f, 7f, 11f, 3.2f, K.Mats.Wall);
                K.Wall(grp, "Corridor_E", false, dx + 1.2f, 7f, 11f, 3.2f, K.Mats.Wall);
                // room z 11..23
                K.FloorAndCeiling(grp, "Room", new Vector2(dx - 4.2f, 11f), new Vector2(dx + 4.2f, 23f), RoomH, K.Mats.Floor, K.Mats.Ceiling);
                K.Wall(grp, "Room_South", true, 11f, dx - 4.2f, dx + 4.2f, RoomH, K.Mats.Wall, dx, 2.4f, 3.2f);
                K.Wall(grp, "Room_North", true, 23f, dx - 4.2f, dx + 4.2f, RoomH, K.Mats.Wall);
                if (i == 0)
                {
                    // Perception: east/west walls carry mirror niches (gaps at z 14 and 20)
                    K.Wall(grp, "Room_W1", false, dx - 4.2f, 11f, 17f, RoomH, K.Mats.Wall, 14f, 1.8f, 2.6f);
                    K.Wall(grp, "Room_W2", false, dx - 4.2f, 17f, 23f, RoomH, K.Mats.Wall, 20f, 1.8f, 2.6f);
                    K.Wall(grp, "Room_E1", false, dx + 4.2f, 11f, 17f, RoomH, K.Mats.Wall, 14f, 1.8f, 2.6f);
                    K.Wall(grp, "Room_E2", false, dx + 4.2f, 17f, 23f, RoomH, K.Mats.Wall, 20f, 1.8f, 2.6f);
                }
                else
                {
                    K.Wall(grp, "Room_W", false, dx - 4.2f, 11f, 23f, RoomH, K.Mats.Wall);
                    K.Wall(grp, "Room_E", false, dx + 4.2f, 11f, 23f, RoomH, K.Mats.Wall);
                }
            }
            // Perception north wall niche: rebuild north wall with a gap for the centre exhibit
            var p = root.Find("PERCEPTIONRoom");
            var north = p.Find("Room_North");
            if (north != null) UnityEngine.Object.DestroyImmediate(north.gameObject);
            K.Wall(p, "Room_North", true, 23f, DoorX[0] - 4.2f, DoorX[0] + 4.2f, RoomH, K.Mats.Wall, DoorX[0], 1.8f, 2.6f);
        }

        private static void BuildAlcove()
        {
            var a = K.Group(root, "SilhouetteCorridor").transform;
            // Leg A: x 16..22, z -6..-3
            K.FloorAndCeiling(a, "LegA", new Vector2(16f, -6f), new Vector2(22f, -3f), 3.4f, K.Mats.Floor, K.Mats.Ceiling);
            K.Wall(a, "LegA_S", true, -6f, 16f, 22f, 3.4f, K.Mats.Wall);
            K.Wall(a, "LegA_N", true, -3f, 16f, 19f, 3.4f, K.Mats.Wall);
            K.Wall(a, "LegA_E", false, 22f, -6f, 3f, 3.4f, K.Mats.Wall);
            // Leg B: x 19..22, z -3..3
            K.FloorAndCeiling(a, "LegB", new Vector2(19f, -3f), new Vector2(22f, 3f), 3.4f, K.Mats.Floor, K.Mats.Ceiling);
            K.Wall(a, "LegB_W", false, 19f, -3f, 3f, 3.4f, K.Mats.Wall);
            K.Wall(a, "LegB_N", true, 3f, 19f, 22f, 3.4f, K.Mats.Wall);
        }

        // ============================================================== lights

        private static List<Light> BuildHallLights(out List<Light> emergency)
        {
            var g = K.Group(root, "HallLights").transform;
            var cold = new Color(0.55f, 0.7f, 1f);
            var list = new List<Light>
            {
                K.PointLight(g, "HallLight_W", new Vector3(-8f, 4.3f, 0f), cold, 1.3f, 13f),
                K.PointLight(g, "HallLight_E", new Vector3(8f, 4.3f, 0f), cold, 1.3f, 13f),
                K.PointLight(g, "HallLight_S", new Vector3(0f, 4.3f, -4f), cold, 1.0f, 11f),
                K.PointLight(g, "HallLight_N", new Vector3(0f, 4.3f, 4f), cold, 1.0f, 11f),
                K.PointLight(g, "AlcoveLight_A", new Vector3(18.5f, 3f, -4.5f), new Color(0.9f, 0.75f, 0.55f), 0.7f, 7f),
                K.PointLight(g, "AlcoveLight_B", new Vector3(20.5f, 3f, 0.5f), new Color(0.9f, 0.75f, 0.55f), 0.6f, 7f),
                K.PointLight(g, "VestibuleLight", new Vector3(-17.5f, 2.8f, -3f), cold, 0.6f, 5f),
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
            K.Box(leftPivot.transform, "Panel", Vector3.zero, new Vector3(1.5f, 3f, 0.15f), K.Mats.Brass);
            var rightPivot = K.Group(elev, "ElevatorDoorRight", new Vector3(0.75f, 1.5f, -7f));
            K.Box(rightPivot.transform, "Panel", Vector3.zero, new Vector3(1.5f, 3f, 0.15f), K.Mats.Brass);

            var zone = K.TriggerBox(hall, "ExitElevatorZone", new Vector3(0f, 1.2f, -4.8f), new Vector3(3.5f, 2.4f, 2.2f)).gameObject.AddComponent<PlayerTriggerZone>();

            // Experiment terminal: large monitor on the south wall.
            var monitor = K.Box(hall, "ExperimentTerminal", new Vector3(4.2f, 2.4f, -6.82f), new Vector3(3.4f, 1.9f, 0.16f), K.Mats.Screen);
            K.Box(monitor.transform, "Bezel", new Vector3(0f, 0f, -0.1f), new Vector3(1.06f, 1.08f, 0.4f), K.Mats.BlackMetal, false);
            var screen = K.Text3D(hall, "ExperimentTerminalText", "", new Vector3(4.2f, 2.4f, -6.72f), Vector3.forward, 0.2f, new Color(0.85f, 0.3f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold, 48);
            var terminal = monitor.AddComponent<TerminalController>();
            terminal.Configure(screen, null, LabEntranceSequence.MonitorText, "", "", 4, "");

            var seqGo = K.Group(root, "LabEntranceSequence");
            var seq = seqGo.AddComponent<LabEntranceSequence>();
            seq.Configure(leftPivot.transform, rightPivot.transform, zone, hallLights, emergencyLights, terminal);
            return seq;
        }

        // =============================================================== doors

        private static DoorController[] BuildDoors()
        {
            var doors = new DoorController[3];
            var flags = new[] { GameFlag.PerceptionUnlocked, GameFlag.ReasonUnlocked, GameFlag.None };
            var releases = new[] { GameFlag.PerceptionSolved, GameFlag.ReasonSolved, GameFlag.MetaCodeSolved };
            var lockedMsgs = new[] { "Locked.", "Sealed. Complete PERCEPTION first.", "Sealed. Use the keypad beside the door." };

            for (int i = 0; i < 3; i++)
            {
                float dx = DoorX[i];
                var grp = root.Find(RoomNames[i] + "Room");
                var doorRoot = K.Group(grp, "Door_" + RoomNames[i], new Vector3(dx, 0f, 7f));
                var panel = K.Box(doorRoot.transform, "Panel", new Vector3(0f, 1.5f, 0f), new Vector3(2f, 3f, 0.25f), K.Mats.BlackMetal);
                K.Box(panel.transform, "Handle", new Vector3(0.3f, 0f, -0.6f), new Vector3(0.06f, 0.25f, 0.2f), K.Mats.Brass, false);
                var door = doorRoot.AddComponent<DoorController>();
                door.Configure(panel.transform, flags[i], lockedMsgs[i], true, releases[i], i == 2, RoomNames[i]);

                K.Text3D(doorRoot.transform, "Label", RoomNames[i], new Vector3(0f, 3.75f, -0.2f), Vector3.back, 0.4f, new Color(0.78f, 0.62f, 0.32f), TextAnchor.MiddleCenter, FontStyle.Bold);
                K.Box(doorRoot.transform, "Frame_L", new Vector3(-1.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 0.4f), K.Mats.Brass);
                K.Box(doorRoot.transform, "Frame_R", new Vector3(1.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 0.4f), K.Mats.Brass);

                var sealZone = K.TriggerBox(grp, "SealTrigger", new Vector3(dx, 1.2f, 10f), new Vector3(2.2f, 2.4f, 1.6f));
                sealZone.gameObject.AddComponent<DoorSealTrigger>().Bind(door);

                var principle = K.Text3D(grp, "Principle", FinalInvestigationController.FinalWords[i], new Vector3(dx, 3.7f, 22.8f), Vector3.back, 0.32f, new Color(0.9f, 0.85f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Bold);
                principle.gameObject.SetActive(false);
                doors[i] = door;
            }
            return doors;
        }

        private static GameObject Principle(int i) => root.Find(RoomNames[i] + "Room").Find("Principle").gameObject;

        // ========================================================== perception

        private static PerceptionPuzzle BuildPerceptionRoom(out GameObject principle)
        {
            float dx = DoorX[0];
            var grp = root.Find("PERCEPTIONRoom");
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
                K.PointLight(grp, "PerceptionLight_A", new Vector3(dx - 2.5f, 4f, 14f), new Color(1f, 0.85f, 0.65f), 1.4f, 10f),
                K.PointLight(grp, "PerceptionLight_B", new Vector3(dx + 2.5f, 4f, 20f), new Color(1f, 0.85f, 0.65f), 1.4f, 10f),
                K.PointLight(grp, "CorridorLight", new Vector3(dx, 3f, 9f), new Color(0.55f, 0.7f, 1f), 0.7f, 6f),
            };
            var feedback = K.PointLight(grp, "FeedbackLight", new Vector3(dx, 3.6f, 17f), Color.red, 0f, 14f);
            feedback.enabled = false;

            puzzle.Configure(displays, lights, feedback, compartmentDoor.transform, card);
            principle = Principle(0);
            return puzzle;
        }

        /// <summary>Niche in the wall, glass pane at the wall plane, pedestal + object in front, "reflection" copy behind.</summary>
        private static MirrorDisplay Exhibit(Transform grp, PerceptionPuzzle puzzle, string id, Vector3 wallPoint, Vector3 normal,
            bool impossible, Action<Transform> buildObject, string note)
        {
            var ex = K.Group(grp, "Exhibit_" + id.Replace(" ", ""), wallPoint);
            var rot = Quaternion.LookRotation(normal, Vector3.up);
            ex.transform.rotation = rot;
            var t = ex.transform;

            // Niche behind the wall plane (local -z), 1.8 wide, 2.6 high, 1.6 deep
            K.Box(t, "Niche_Back", new Vector3(0f, 1.3f, -1.65f), new Vector3(1.9f, 2.7f, 0.1f), K.Mats.Niche);
            K.Box(t, "Niche_L", new Vector3(-0.95f, 1.3f, -0.85f), new Vector3(0.1f, 2.7f, 1.7f), K.Mats.Niche);
            K.Box(t, "Niche_R", new Vector3(0.95f, 1.3f, -0.85f), new Vector3(0.1f, 2.7f, 1.7f), K.Mats.Niche);
            K.Box(t, "Niche_Top", new Vector3(0f, 2.65f, -0.85f), new Vector3(1.9f, 0.1f, 1.7f), K.Mats.Niche);
            K.Box(t, "Niche_Floor", new Vector3(0f, -0.05f, -0.85f), new Vector3(1.9f, 0.1f, 1.7f), K.Mats.Niche);

            // Mirror pane (the thing the player selects)
            var pane = K.Box(t, "MirrorPane", new Vector3(0f, 1.3f, 0f), new Vector3(1.8f, 2.6f, 0.04f), K.Mats.Mirror);
            K.Box(t, "MirrorFrame_L", new Vector3(-0.95f, 1.3f, 0f), new Vector3(0.1f, 2.7f, 0.12f), K.Mats.Brass, false);
            K.Box(t, "MirrorFrame_R", new Vector3(0.95f, 1.3f, 0f), new Vector3(0.1f, 2.7f, 0.12f), K.Mats.Brass, false);
            K.Box(t, "MirrorFrame_T", new Vector3(0f, 2.65f, 0f), new Vector3(2f, 0.1f, 0.12f), K.Mats.Brass, false);
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

        private static ReasonPuzzle BuildReasonRoom(out List<Light> lights, out GameObject principle)
        {
            float dx = DoorX[1];
            var grp = root.Find("REASONRoom");
            var puzzleGo = K.Group(grp, "ReasonPuzzle");
            var puzzle = puzzleGo.AddComponent<ReasonPuzzle>();
            puzzle.SetPuzzleId("reason");

            // Balance scale
            var scaleRoot = K.Group(grp, "BalanceScale", new Vector3(dx, 0f, 17f));
            K.Prim(scaleRoot.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(1.3f, 0.08f, 1.3f), K.Mats.BlackMetal);
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
            K.Box(boxRoot.transform, "Pedestal", new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.9f), K.Mats.BlackMetal);
            var body = K.Box(boxRoot.transform, "Body", new Vector3(0f, 1.0f, 0f), new Vector3(0.6f, 0.4f, 0.6f), K.Mats.BlackMetal);
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
            K.Box(term.transform, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1.0f, 0.5f), K.Mats.BlackMetal);
            var pad = K.Box(term.transform, "Keypad", new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.2f, 0.4f), K.Mats.Brass);
            K.Text3D(term.transform, "Label", "ENTER VALUE", new Vector3(0f, 1.205f, 0f), Vector3.up, 0.05f, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 48, Vector3.forward);
            pad.AddComponent<KeypadController>().Configure(puzzle, "Enter Code", "REASON", "What is the value of the BLACK BOX?", 2, "AN ASSUMPTION COSTS YOU.");

            lights = new List<Light>
            {
                K.PointLight(grp, "ReasonLight_A", new Vector3(dx - 2.5f, 4f, 15f), new Color(0.6f, 0.75f, 1f), 1.3f, 10f),
                K.PointLight(grp, "ReasonLight_B", new Vector3(dx + 2.5f, 4f, 20f), new Color(0.6f, 0.75f, 1f), 1.3f, 10f),
                K.PointLight(grp, "CorridorLight", new Vector3(dx, 3f, 9f), new Color(0.55f, 0.7f, 1f), 0.7f, 6f),
                K.PointLight(grp, "ScaleLight", new Vector3(dx, 3.2f, 17f), new Color(1f, 0.85f, 0.6f), 0.9f, 5f),
            };
            var feedback = K.PointLight(grp, "FeedbackLight", new Vector3(dx, 3.6f, 17f), Color.red, 0f, 14f);
            feedback.enabled = false;

            puzzle.Configure(scaleCtrl, boxCtrl, feedback);
            principle = Principle(1);
            return puzzle;
        }

        // ============================================================== memory

        private static TerminalController BuildMemoryRoom(out List<Light> lights, out GameObject principle, out MemoryPuzzle puzzle)
        {
            float dx = DoorX[2];
            var grp = root.Find("MEMORYRoom");
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
            principle = Principle(2);
            return terminal;
        }

        // ========================================================== silhouette

        private static void BuildSilhouetteEvent()
        {
            var a = root.Find("SilhouetteCorridor");
            var fig = K.Group(a, "VossSilhouette", new Vector3(17f, 0f, -4.5f));
            K.Prim(fig.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.9f, 0.45f), K.Mats.Silhouette, false);
            K.Prim(fig.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.12f, 0.36f), K.Mats.Silhouette, false);
            K.Prim(fig.transform, "Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.5f, 0.02f, 0.5f), K.Mats.Silhouette, false);
            var start = K.Group(a, "WalkStart", new Vector3(16.6f, 0f, -4.5f));
            var end = K.Group(a, "WalkEnd", new Vector3(20.6f, 0f, -1.4f));
            var writing = K.Text3D(a, "WallWriting", "DID YOU SEE ME?\nOR DID YOU EXPECT TO?", new Vector3(20.5f, 1.9f, 2.8f), Vector3.back, 0.28f, new Color(0.6f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var zone = K.TriggerBox(a, "SilhouetteTrigger", new Vector3(DoorX[0], 1.2f, 5f), new Vector3(4f, 2.4f, 3.2f)).gameObject.AddComponent<PlayerTriggerZone>();
            var ev = a.gameObject.AddComponent<VossSilhouetteEvent>();
            ev.Configure(zone, fig.transform, start.transform, end.transform, writing.gameObject);
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
            K.FloorAndCeiling(v, "Vestibule", new Vector2(-19.5f, -4f), new Vector2(-16f, -2f), 3.2f, K.Mats.Floor, K.Mats.Ceiling);
            K.Wall(v, "Vestibule_N", true, -2f, -19.5f, -16f, 3.2f, K.Mats.Wall);
            K.Wall(v, "Vestibule_S", true, -4f, -19.5f, -16f, 3.2f, K.Mats.Wall);
            K.Wall(v, "Vestibule_W", false, -19.5f, -4f, -2f, 3.2f, K.Mats.Wall);

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
