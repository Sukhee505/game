using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Environment;
using SevenMinutes.Puzzles;
using SevenMinutes.Traps;

namespace SevenMinutes.EditorTools
{
    public static partial class HouseBuilder
    {
        private const float DuctClearance = 1.08f;

        // =====================================================================
        // LEVEL 3 — Lava pit and bridge (x 18..34, z 40..60)
        // =====================================================================

        private static void BuildLavaRoom(Ctx ctx)
        {
            var root = new GameObject("Level3_LavaPit").transform;
            root.SetParent(ctx.House, false);

            Floor("Player_Platform", root, 18, 34, 40, 46, 0, M.Concrete, SurfaceType.Concrete);
            Floor("Robot_Platform", root, 18, 34, 54, 60, 0, M.Concrete, SurfaceType.Concrete);
            Floor("Duct_Floor", root, 31.6f, 34, 46, 54, 0, M.Metal, SurfaceType.Metal);
            Ceiling("Ceiling", root, 18, 34, 40, 60, 6f, M.ConcreteDark);

            WallX("Wall_South", root, 17.85f, 34.15f, 40, 0, 6, M.ConcreteDark, new Gap(25f, 1.6f, 2.4f));
            WallZ("Wall_West", root, 40, 60, 18, -10, 16, M.Stone);
            WallZ("Wall_East", root, 40, 60, 34, 0, 6, M.ConcreteDark);
            WallX("Wall_North", root, 17.85f, 34.15f, 60, 0, 6, M.ConcreteDark,
                new Gap(25f, 1.6f, 2.4f), new Gap(29f, 1.4f, DuctClearance));

            // ---- the pit ----
            var pit = new GameObject("Pit").transform;
            pit.SetParent(root, false);
            pit.gameObject.AddComponent<NavMeshIgnore>();
            BoxMinMax("Pit_Wall_S", pit, new Vector3(18f, -10f, 45.8f), new Vector3(31.6f, -0.3f, 46f), M.Stone);
            BoxMinMax("Pit_Wall_N", pit, new Vector3(18f, -10f, 54f), new Vector3(31.6f, -0.3f, 54.2f), M.Stone);
            BoxMinMax("Pit_Wall_E", pit, new Vector3(31.6f, -10f, 46f), new Vector3(31.9f, 0f, 54f), M.Stone);
            var lava = BoxMinMax("Lava_Surface", pit, new Vector3(18f, -7.4f, 46f), new Vector3(31.6f, -7f, 54f), M.Lava);
            var kill = TriggerBox("Lava_Kill", pit, new Vector3(18f, -4.5f, 46f), new Vector3(31.6f, -2.2f, 54f));
            Add<HazardVolume>(kill).Configure("Lavaya düştün!", FailKind.Burn);
            var lavaLights = new[]
            {
                PointLight("Lava_Light1", pit, new Vector3(21f, -5.5f, 50f), new Color(1f, 0.35f, 0.05f), 5f, 14f),
                PointLight("Lava_Light2", pit, new Vector3(28f, -5.5f, 50f), new Color(1f, 0.3f, 0.05f), 5f, 14f),
                PointLight("Lava_Up", pit, new Vector3(25f, 0.8f, 50f), new Color(1f, 0.35f, 0.1f), 1.8f, 12f)
            };
            Add<LavaSurface>(lava).Configure(lava.GetComponent<Renderer>(), lavaLights);
            Particles("Lava_Embers", pit, new Vector3(24.8f, -6.8f, 50f), new Vector3(13f, 0.2f, 7.5f), new Color(1f, 0.5f, 0.1f), 0.08f, 25f, 1.2f, 4f);
            Particles("Lava_Smoke", pit, new Vector3(24.8f, -6.5f, 50f), new Vector3(13f, 0.5f, 7.5f), new Color(0.15f, 0.12f, 0.1f, 0.35f), 1.6f, 6f, 0.6f, 6f);
            var lavaAudio = new GameObject("Lava_Audio_Anchor");
            lavaAudio.transform.SetParent(pit, false);
            lavaAudio.transform.position = new Vector3(25f, -3f, 50f);
            lavaAudio.AddComponent<LoopingSound>().Configure(Audio.SoundId.Lava, 0.8f, 26f);

            // ---- maintenance duct (robot only: 1.08 m high) along the east side ----
            var duct = new GameObject("MaintenanceDuct").transform;
            duct.SetParent(root, false);
            BoxMinMax("Duct_Wall_W", duct, new Vector3(31.6f, 0f, 46f), new Vector3(31.9f, DuctClearance, 54f), M.Metal);
            BoxMinMax("Duct_Block", duct, new Vector3(31.6f, DuctClearance, 46f), new Vector3(33.85f, 6f, 54f), M.Metal);
            BoxMinMax("Duct_Hazard_S", duct, new Vector3(31.9f, DuctClearance, 45.95f), new Vector3(33.85f, DuctClearance + 0.25f, 46f), M.Hazard, false);
            BoxMinMax("Duct_Hazard_N", duct, new Vector3(31.9f, DuctClearance, 54f), new Vector3(33.85f, DuctClearance + 0.25f, 54.05f), M.Hazard, false);
            Text("Duct_Sign", duct, new Vector3(32.75f, 1.9f, 45.94f), Vector3.zero, "BAKIM KANALI\n<size=60%>SADECE ROBOT GİREBİLİR</size>", 2f,
                new Color(0.9f, 0.75f, 0.2f), 2.2f, 0.8f);
            PointLight("Duct_Light", duct, new Vector3(32.8f, 0.6f, 50f), new Color(0.3f, 0.6f, 1f), 0.5f, 4f);

            // ---- bridge segments (extended positions; the puzzle retracts them at start) ----
            var bridge = new GameObject("Bridge").transform;
            bridge.SetParent(root, false);
            bridge.gameObject.AddComponent<NavMeshIgnore>();
            var segments = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var seg = new GameObject("Segment" + i).transform;
                seg.SetParent(bridge, false);
                seg.position = new Vector3(25.5f, 0f, 47f + i * 2f);
                BoxL("Deck", seg, new Vector3(0, -0.125f, 0), new Vector3(3f, 0.25f, 1.98f), M.MetalFloor, true).AddComponent<FootstepSurface>().Surface = SurfaceType.Metal;
                BoxL("Rail_L", seg, new Vector3(-1.45f, 0.5f, 0), new Vector3(0.06f, 0.06f, 1.98f), M.Hazard, true);
                BoxL("Rail_R", seg, new Vector3(1.45f, 0.5f, 0), new Vector3(0.06f, 0.06f, 1.98f), M.Hazard, true);
                BoxL("Post_L", seg, new Vector3(-1.45f, 0.25f, 0), new Vector3(0.06f, 0.5f, 0.06f), M.DarkMetal);
                BoxL("Post_R", seg, new Vector3(1.45f, 0.25f, 0), new Vector3(0.06f, 0.5f, 0.06f), M.DarkMetal);
                BoxL("Under", seg, new Vector3(0, -0.35f, 0), new Vector3(2.6f, 0.2f, 1.9f), M.DarkMetal);
                segments[i] = seg;
            }

            // ---- computer (to the right of the start position) ----
            BoxMinMax("Computer_Desk", root, new Vector3(28.1f, 0, 42.6f), new Vector3(29.3f, 0.78f, 43.4f), M.DarkMetal);
            var monitor = Box("Computer", root, new Vector3(28.7f, 1.12f, 43.25f), new Vector3(0.75f, 0.5f, 0.08f), M.TvBody);
            monitor.transform.rotation = Quaternion.Euler(0, 180, 0);
            ImageQuad("Computer_Screen", monitor.transform, monitor.transform.position + new Vector3(0, 0, -0.045f), Vector3.zero,
                new Vector2(0.68f, 0.42f), M.GlowGreen).GetComponent<Renderer>().sharedMaterial = M.RobotGlow;
            var screenText = Text("Computer_Text", monitor.transform, monitor.transform.position + new Vector3(0, 0, -0.05f), Vector3.zero,
                "", 0.55f, new Color(0.3f, 1f, 0.45f), 0.66f, 0.4f);
            screenText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            var terminal = Add<ComputerTerminal>(monitor);
            terminal.Configure(screenText);
            Highlight(monitor);
            Box("Computer_Keyboard", root, new Vector3(28.7f, 0.8f, 42.95f), new Vector3(0.55f, 0.03f, 0.18f), M.DarkMetal, false);
            PointLight("Computer_Glow", root, new Vector3(28.7f, 1.3f, 42.6f), new Color(0.3f, 1f, 0.45f), 0.6f, 3f);

            // ---- the big red button on the robot's platform ----
            var pedestal = Box("RedButton_Pedestal", root, new Vector3(21f, 0.35f, 57.1f), new Vector3(0.5f, 0.7f, 0.5f), M.DarkMetal);
            var cap = Prim(PrimitiveType.Cylinder, "RedButton_Cap", pedestal.transform, new Vector3(0, 0.38f, 0), new Vector3(0.36f, 0.05f, 0.36f), M.GlowRed);
            var redButton = Add<PushButton>(pedestal);
            redButton.Configure("Düğmeye Bas", cap.transform, true, "");
            Highlight(pedestal);
            var stand = Point(ctx, "Robot_ButtonStand", new Vector3(21f, 0, 56.35f), 0f);
            ctx.LavaButtonStand = stand;
            PointLight("RedButton_Glow", root, new Vector3(21f, 1.2f, 57f), new Color(1f, 0.1f, 0.05f), 0.8f, 3f);

            var exitDoor = SlideDoor("Door_ToTraps", root, new Vector3(25f, 0, 60f), 1.6f, 2.4f, "Kilitli. Kolu çek.");
            var lever = BuildLever("Lever_Lava", root, new Vector3(27.2f, 0, 59.3f), 180f, "", true);

            Add<LevelZone>(TriggerBox("Zone_Level3", root, new Vector3(18.2f, 0, 40.3f), new Vector3(31.4f, 2.5f, 44f))).Configure(3);
            ctx.Checkpoints[2] = Point(ctx, "Checkpoint_L3", new Vector3(25f, 0, 41.3f), 0f);
            ctx.LavaRobotWait = Point(ctx, "Robot_LavaWait", new Vector3(23.5f, 0, 55.4f), 180f);

            Text("Sign_Danger", root, new Vector3(25f, 4.4f, 59.83f), new Vector3(0, 180, 0), "<b>TEHLİKE — ERİMİŞ ÇEKİRDEK</b>", 3.5f,
                new Color(1f, 0.55f, 0.2f), 6f, 0.7f);
            Lamp("Lamp_PlayerSide", root, new Vector3(25f, 5.2f, 42f), new Color(0.8f, 0.85f, 1f), 0.6f, 7f, true, 0.15f);
            Lamp("Lamp_RobotSide", root, new Vector3(25f, 5.2f, 57.5f), new Color(0.8f, 0.85f, 1f), 0.6f, 7f, true, 0.15f);

            ctx.Lava = Add<LavaBridgePuzzle>(root.gameObject);
            ctx.Lava.Configure(terminal, stand, redButton, segments, lever, exitDoor);
        }

        private static ParticleSystem Particles(string name, Transform parent, Vector3 pos, Vector3 box, Color color, float size,
            float rate, float speed, float lifetime)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(box.x, box.z, box.y);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = M.Particle;
            return ps;
        }

        // =====================================================================
        // LEVEL 4 — Trap corridor (x 22..28, z 60..96) + robot duct (x 28..30)
        // =====================================================================

        private static void BuildTrapCorridor(Ctx ctx)
        {
            var root = new GameObject("Level4_TrapCorridor").transform;
            root.SetParent(ctx.House, false);

            Floor("Floor", root, 22, 28, 60, 96, 0, M.MetalFloor, SurfaceType.Metal);
            Ceiling("Ceiling", root, 22, 28, 60, 96, 4f, M.Metal);
            WallZ("Wall_West", root, 60, 96, 22, 0, 4, M.Metal);
            WallZ("Wall_East", root, 60, 96, 28, 0, 4, M.Metal);

            // robot duct
            Floor("Duct_Floor", root, 28, 30, 60, 96, 0, M.Metal, SurfaceType.Metal);
            WallZ("Duct_Wall_East", root, 60, 96, 30, 0, 4, M.Metal);
            BoxMinMax("Duct_Ceiling", root, new Vector3(28.15f, DuctClearance, 60f), new Vector3(29.85f, 4f, 96f), M.Metal);
            PointLight("Duct_Light1", root, new Vector3(29f, 0.7f, 70f), new Color(0.3f, 0.6f, 1f), 0.4f, 6f);
            PointLight("Duct_Light2", root, new Vector3(29f, 0.7f, 86f), new Color(0.3f, 0.6f, 1f), 0.4f, 6f);

            // hazard stripes on the floor before each trap
            foreach (float z in new[] { 62.5f, 71.5f, 82.5f })
                BoxMinMax("Stripe", root, new Vector3(22.15f, 0, z), new Vector3(27.85f, 0.006f, z + 0.4f), M.Hazard, false);

            // ---- obstacle 1: swinging spiked ball ----
            var ballRoot = new GameObject("Trap_SwingingBall").transform;
            ballRoot.SetParent(root, false);
            ballRoot.position = new Vector3(25f, 3.9f, 66.5f);
            ballRoot.gameObject.AddComponent<NavMeshIgnore>();
            BoxL("Mount", ballRoot, new Vector3(0, 0.03f, 0), new Vector3(0.6f, 0.15f, 0.6f), M.DarkMetal);
            var pivot = Node("Pivot", ballRoot, Vector3.zero);
            for (int i = 0; i < 8; i++)
                Prim(PrimitiveType.Capsule, "Chain" + i, pivot, new Vector3(0, -0.15f - i * 0.25f, 0), new Vector3(0.07f, 0.14f, 0.07f), M.Rust,
                    false, new Vector3(0, i * 90f, 0));
            var ball = Node("Ball", pivot, new Vector3(0, -2.45f, 0));
            Prim(PrimitiveType.Sphere, "Sphere", ball, Vector3.zero, Vector3.one * 1.1f, M.DarkMetal);
            var dirs = new List<Vector3>();
            for (int i = 0; i < 14; i++)
            {
                float y = 1f - (i + 0.5f) / 14f * 2f;
                float r = Mathf.Sqrt(1f - y * y);
                float a = i * 2.39996f;
                dirs.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
            }
            foreach (var d in dirs)
            {
                var spike = Prim(PrimitiveType.Cylinder, "Spike", ball, d * 0.68f, new Vector3(0.07f, 0.17f, 0.07f), M.Spike);
                spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d);
            }
            var ballCol = ball.gameObject.AddComponent<SphereCollider>();
            ballCol.isTrigger = true;
            ballCol.radius = 0.78f;
            Add<SwingingBallTrap>(ballRoot.gameObject).Configure(pivot, ball);

            // ---- obstacle 2: crushing walls (x 23.5..26.5 passage, z 73..79) ----
            BoxMinMax("Narrow_L1", root, new Vector3(22.15f, 0, 72f), new Vector3(23.5f, 4, 73f), M.Rust);
            BoxMinMax("Narrow_R1", root, new Vector3(26.5f, 0, 72f), new Vector3(27.85f, 4, 73f), M.Rust);
            BoxMinMax("Narrow_L2", root, new Vector3(22.15f, 0, 79f), new Vector3(23.5f, 4, 80f), M.Rust);
            BoxMinMax("Narrow_R2", root, new Vector3(26.5f, 0, 79f), new Vector3(27.85f, 4, 80f), M.Rust);
            var crushRoot = new GameObject("Trap_CrushingWalls").transform;
            crushRoot.SetParent(root, false);
            crushRoot.position = new Vector3(25f, 0f, 76f);
            crushRoot.gameObject.AddComponent<NavMeshIgnore>();
            var left = BoxL("Wall_Left", crushRoot, new Vector3(-2.3f, 1.8f, 0), new Vector3(1.6f, 3.6f, 6f), M.Rust, true);
            var right = BoxL("Wall_Right", crushRoot, new Vector3(2.3f, 1.8f, 0), new Vector3(1.6f, 3.6f, 6f), M.Rust, true);
            BoxL("Teeth_L", left.transform, new Vector3(0.82f, 0, 0), new Vector3(0.06f, 3.4f, 5.8f), M.Hazard);
            BoxL("Teeth_R", right.transform, new Vector3(-0.82f, 0, 0), new Vector3(0.06f, 3.4f, 5.8f), M.Hazard);
            var warn = new[]
            {
                PointLight("Warning1", crushRoot, new Vector3(25f, 3.7f, 72.6f), new Color(1f, 0.05f, 0.02f), 0f, 7f),
                PointLight("Warning2", crushRoot, new Vector3(25f, 3.7f, 79.4f), new Color(1f, 0.05f, 0.02f), 0f, 7f)
            };
            Prim(PrimitiveType.Sphere, "WarnBulb1", crushRoot, new Vector3(0, 3.85f, -3.4f), Vector3.one * 0.18f, M.GlowRed);
            Prim(PrimitiveType.Sphere, "WarnBulb2", crushRoot, new Vector3(0, 3.85f, 3.4f), Vector3.one * 0.18f, M.GlowRed);
            var dust = Particles("Dust", crushRoot, new Vector3(25f, 0.2f, 76f), new Vector3(0.6f, 0.2f, 5.5f), new Color(0.5f, 0.45f, 0.4f, 0.5f), 0.5f, 0f, 1.2f, 1.5f);
            var dmain = dust.main;
            dmain.loop = false;
            dmain.playOnAwake = false;
            var burst = dust.emission;
            burst.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            Add<CrushingWallsTrap>(crushRoot.gameObject).Configure(left.transform, right.transform, 1.4f, warn, dust);

            // ---- obstacle 3: lasers ----
            BuildLaser(root, "Laser_A_Jump", 84f, 0.3f, LaserMode.Static, "ATLA");
            BuildLaser(root, "Laser_B_Crouch", 88f, 1.6f, LaserMode.Static, "EĞİL");
            BuildLaser(root, "Laser_C_Moving", 92f, 0.25f, LaserMode.MovingVertical, "BEKLE");

            // lights
            for (int i = 0; i < 6; i++)
                Lamp("Lamp_Trap" + i, root, new Vector3(25f, 3.75f, 61.5f + i * 6.5f), i % 2 == 0 ? new Color(1f, 0.3f, 0.2f) : new Color(0.7f, 0.75f, 0.9f),
                    0.7f, 7f, true, 0.12f, false, false);

            // robot warnings + level start
            Add<LevelZone>(TriggerBox("Zone_Level4", root, new Vector3(22.2f, 0, 60.3f), new Vector3(27.8f, 2.5f, 62.2f))).Configure(4);
            Add<RobotLineZone>(TriggerBox("Warn_Ball", root, new Vector3(22.2f, 0, 62.3f), new Vector3(27.8f, 2.5f, 63.6f)))
                .Configure("O top... kenara savrulduğunda koşarak geç!", false, 4);
            Add<RobotLineZone>(TriggerBox("Warn_Walls", root, new Vector3(22.2f, 0, 70.5f), new Vector3(27.8f, 2.5f, 71.9f)))
                .Configure("Duvarlar! Açılmalarını bekle, sonra koş.", false, 4);
            Add<RobotLineZone>(TriggerBox("Warn_Lasers", root, new Vector3(22.2f, 0, 81.2f), new Vector3(27.8f, 2.5f, 82.5f)))
                .Configure("Lazerler. Alçak olanın üstünden atla, yüksek olanın altından eğil (Ctrl), hareketli olan yükselince geç.", false, 4);
            ctx.Checkpoints[3] = Point(ctx, "Checkpoint_L4", new Vector3(25f, 0, 61.3f), 0f);
        }

        private static void BuildLaser(Transform parent, string name, float z, float y, LaserMode mode, string label)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = new Vector3(25f, y, z);
            root.gameObject.AddComponent<NavMeshIgnore>();
            var beam = new GameObject("Beam");
            beam.transform.SetParent(root, false);
            beam.transform.localScale = new Vector3(5.7f, 0.05f, 0.05f);
            beam.AddComponent<MeshFilter>().sharedMesh = BoxMesh(Vector3.one);
            beam.AddComponent<MeshRenderer>().sharedMaterial = M.Laser;
            var col = beam.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1f, 2.2f, 2.2f);
            BoxL("Emitter_L", root, new Vector3(-2.88f, 0, 0), new Vector3(0.12f, 0.18f, 0.18f), M.DarkMetal);
            BoxL("Emitter_R", root, new Vector3(2.88f, 0, 0), new Vector3(0.12f, 0.18f, 0.18f), M.DarkMetal);
            var lights = new[]
            {
                PointLight("Glow", root, root.position + new Vector3(0, 0, 0.2f), new Color(1f, 0.05f, 0.05f), 1.4f, 3.5f)
            };
            Add<LaserTrap>(root.gameObject).Configure(beam.transform, mode, 0.25f, 2.35f, lights);
            Text("Label_" + label, parent, new Vector3(27.83f, 2.9f, z), new Vector3(0, 90, 0), label, 2.4f, new Color(1f, 0.25f, 0.2f), 1.5f, 0.4f);
        }

        // =====================================================================
        // Final hall + the great door (x 18..34, z 96..106)
        // =====================================================================

        private static void BuildFinalHall(Ctx ctx)
        {
            var root = new GameObject("Final_DoorHall").transform;
            root.SetParent(ctx.House, false);

            Floor("Floor", root, 18, 34, 96, 106, 0, M.ConcreteDark, SurfaceType.Concrete);
            Ceiling("Ceiling", root, 18, 34, 96, 106, 7f, M.ConcreteDark);
            WallZ("Wall_West", root, 96, 106, 18, 0, 7, M.ConcreteDark);
            WallZ("Wall_East", root, 96, 106, 34, 0, 7, M.ConcreteDark);
            WallX("Wall_South_W", root, 17.85f, 22, 96, 0, 7, M.ConcreteDark);
            WallX("Wall_South_E", root, 30, 34.15f, 96, 0, 7, M.ConcreteDark);
            BoxMinMax("Wall_South_Lintel", root, new Vector3(22f, 4f, 95.85f), new Vector3(30f, 7f, 96.15f), M.ConcreteDark);
            BoxMinMax("Wall_North_W", root, new Vector3(17.85f, 0, 105.85f), new Vector3(22f, 7f, 106.3f), M.Brick);
            BoxMinMax("Wall_North_E", root, new Vector3(28f, 0, 105.85f), new Vector3(34.15f, 7f, 106.3f), M.Brick);
            BoxMinMax("Wall_North_Top", root, new Vector3(22f, 5.5f, 105.85f), new Vector3(28f, 7f, 106.3f), M.Brick);

            // ---- the great door ----
            var doorRoot = new GameObject("GreatDoor").transform;
            doorRoot.SetParent(root, false);
            doorRoot.position = new Vector3(25f, 0f, 106f);
            doorRoot.gameObject.AddComponent<NavMeshIgnore>();
            BoxL("Pillar_L", doorRoot, new Vector3(-3.25f, 2.9f, -0.35f), new Vector3(0.5f, 5.8f, 0.5f), M.DoorDark);
            BoxL("Pillar_R", doorRoot, new Vector3(3.25f, 2.9f, -0.35f), new Vector3(0.5f, 5.8f, 0.5f), M.DoorDark);
            BoxL("Arch", doorRoot, new Vector3(0, 5.8f, -0.35f), new Vector3(7f, 0.5f, 0.5f), M.DoorDark);
            BoxL("Arch_Gold", doorRoot, new Vector3(0, 5.8f, -0.61f), new Vector3(6.6f, 0.12f, 0.02f), M.Gold);
            var halves = new Transform[2];
            for (int h = 0; h < 2; h++)
            {
                float side = h == 0 ? -1f : 1f;
                var half = BoxL(h == 0 ? "Door_Left" : "Door_Right", doorRoot, new Vector3(side * 1.5f, 2.75f, 0f), new Vector3(3f, 5.5f, 0.4f), M.DoorDark, true);
                BoxL("Trim_Top", half.transform, new Vector3(0, 2.5f, -0.21f), new Vector3(2.8f, 0.1f, 0.02f), M.Gold);
                BoxL("Trim_Bottom", half.transform, new Vector3(0, -2.5f, -0.21f), new Vector3(2.8f, 0.1f, 0.02f), M.Gold);
                BoxL("Trim_Mid", half.transform, new Vector3(0, 0f, -0.21f), new Vector3(2.8f, 0.06f, 0.02f), M.Gold);
                var diamond = BoxL("Diamond", half.transform, new Vector3(0, 1.2f, -0.22f), new Vector3(0.9f, 0.9f, 0.03f), M.Gold);
                diamond.transform.localRotation = Quaternion.Euler(0, 0, 45);
                BoxL("Diamond_In", diamond.transform, new Vector3(0, 0, -0.02f), new Vector3(0.6f, 0.6f, 0.02f), M.DoorDark);
                BoxL("Rivets", half.transform, new Vector3(-side * 1.35f, 0, -0.21f), new Vector3(0.08f, 5.2f, 0.03f), M.Metal);
                Obstacle(half, new Vector3(3f, 5.5f, 0.4f));
                halves[h] = half.transform;
            }
            var core = Prim(PrimitiveType.Cylinder, "Core_Panel", doorRoot, new Vector3(0, 6.35f, -0.16f), new Vector3(1.0f, 0.04f, 1.0f), M.GlowBlue,
                false, new Vector3(90, 0, 0));
            var bolts = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                // bolts ride with the left half, crossing the seam
                var bolt = Prim(PrimitiveType.Cylinder, "Bolt" + i, halves[0], new Vector3(1.5f, 0.9f + i * 1.25f - 2.75f, -0.3f), new Vector3(0.22f, 0.5f, 0.22f), M.Gold,
                    false, new Vector3(0, 0, 90));
                bolts[i] = bolt.transform;
            }

            // ---- UNIT-6: the old guard robot of the door (local LLM NPC) ----
            var guard = new GameObject("UNIT-6 Guard (LLM NPC)").transform;
            guard.SetParent(root, false);
            guard.position = new Vector3(21.4f, 0, 103.8f);
            guard.rotation = Quaternion.Euler(0, 160, 0);
            var gCol = guard.gameObject.AddComponent<BoxCollider>();
            gCol.center = new Vector3(0, 1.0f, 0);
            gCol.size = new Vector3(1.0f, 2.0f, 0.8f);
            foreach (float side in new[] { -1f, 1f })
            {
                BoxL("Leg", guard, new Vector3(side * 0.2f, 0.45f, 0), new Vector3(0.22f, 0.9f, 0.26f), M.Rust);
                BoxL("Foot", guard, new Vector3(side * 0.2f, 0.05f, 0.08f), new Vector3(0.28f, 0.1f, 0.42f), M.RobotJoint);
                BoxL("Arm", guard, new Vector3(side * 0.52f, 1.15f, 0.02f), new Vector3(0.18f, 0.75f, 0.2f), M.Rust);
                Prim(PrimitiveType.Sphere, "Shoulder", guard, new Vector3(side * 0.5f, 1.55f, 0), Vector3.one * 0.26f, M.RobotJoint);
                BoxL("Hand", guard, new Vector3(side * 0.52f, 0.72f, 0.04f), new Vector3(0.2f, 0.16f, 0.2f), M.RobotJoint);
            }
            BoxL("Hips", guard, new Vector3(0, 0.95f, 0), new Vector3(0.62f, 0.2f, 0.36f), M.RobotJoint);
            BoxL("Torso", guard, new Vector3(0, 1.35f, 0), new Vector3(0.82f, 0.62f, 0.46f), M.Rust);
            BoxL("ChestPlate", guard, new Vector3(0, 1.38f, 0.235f), new Vector3(0.56f, 0.4f, 0.02f), M.DoorDark);
            BoxL("Neck", guard, new Vector3(0, 1.72f, 0), new Vector3(0.18f, 0.12f, 0.18f), M.RobotJoint);
            BoxL("Head", guard, new Vector3(0, 1.92f, 0), new Vector3(0.46f, 0.32f, 0.38f), M.Rust);
            BoxL("Visor", guard, new Vector3(0, 1.94f, 0.195f), new Vector3(0.38f, 0.1f, 0.02f), M.Pupil);
            var gEyeL = BoxL("EyeL", guard, new Vector3(-0.09f, 1.94f, 0.207f), new Vector3(0.1f, 0.045f, 0.01f), M.RobotGlow);
            var gEyeR = BoxL("EyeR", guard, new Vector3(0.09f, 1.94f, 0.207f), new Vector3(0.1f, 0.045f, 0.01f), M.RobotGlow);
            var gCore = BoxL("ChestCore", guard, new Vector3(0, 1.42f, 0.25f), new Vector3(0.14f, 0.14f, 0.01f), M.RobotGlow);
            Text("Guard_Label", guard, guard.position + new Vector3(0, 2.45f, 0), new Vector3(0, 160, 0), "UNIT-6", 2.2f, new Color(1f, 0.4f, 0.3f), 1.2f, 0.3f);
            var gLight = PointLight("Guard_EyeLight", guard, guard.position + guard.forward * 0.6f + Vector3.up * 1.9f, new Color(1f, 0.15f, 0.08f), 0f, 4f);
            Highlight(guard.gameObject);
            ctx.Guard = guard.gameObject;
            ctx.GuardEyes = new[] { gEyeL.GetComponent<Renderer>(), gEyeR.GetComponent<Renderer>(), gCore.GetComponent<Renderer>() };
            ctx.GuardLight = gLight;
            var slotLights = new Renderer[0];
            var panelLight = PointLight("Guard_PowerLight", root, new Vector3(21.4f, 2.6f, 102.6f), new Color(0.4f, 0.7f, 1f), 0f, 4f);

            // ---- pressure plate (robot only) ----
            var plate = new GameObject("PressurePlate").transform;
            plate.SetParent(root, false);
            plate.position = new Vector3(31.2f, 0, 99.6f);
            BoxL("Base", plate, new Vector3(0, 0.04f, 0), new Vector3(1.7f, 0.08f, 1.7f), M.Hazard);
            var plateTop = BoxL("Top", plate, new Vector3(0, 0.1f, 0), new Vector3(1.3f, 0.06f, 1.3f), M.DarkMetal);
            var plateGlow = BoxL("Glow", plateTop.transform, new Vector3(0, 0.035f, 0), new Vector3(1.1f, 0.01f, 1.1f), M.RobotGlow);
            var plateTrigger = plate.gameObject.AddComponent<BoxCollider>();
            plateTrigger.isTrigger = true;
            plateTrigger.center = new Vector3(0, 0.75f, 0);
            plateTrigger.size = new Vector3(1.6f, 1.5f, 1.6f);
            var pp = plate.gameObject.AddComponent<PressurePlate>();
            pp.Configure(plateTop.transform, plateGlow.GetComponent<Renderer>());
            Text("Plate_Label", root, new Vector3(31.2f, 0.02f, 98.5f), new Vector3(90, 0, 0), "AĞIR YÜK", 2f, new Color(0.9f, 0.75f, 0.2f), 1.6f, 0.4f);

            // power cables (light up when powered)
            var cables = new List<Renderer>
            {
                BoxMinMax("Cable_1", root, new Vector3(21.2f, 0, 99.55f), new Vector3(31.2f, 0.03f, 99.65f), M.RobotGlow, false).GetComponent<Renderer>(),
                BoxMinMax("Cable_2", root, new Vector3(21.15f, 0, 99.55f), new Vector3(21.25f, 0.03f, 103.3f), M.RobotGlow, false).GetComponent<Renderer>(),
                BoxMinMax("Cable_3", root, new Vector3(24.95f, 0, 99.6f), new Vector3(25.05f, 0.03f, 105.6f), M.RobotGlow, false).GetComponent<Renderer>(),
                core.GetComponent<Renderer>(),
                plateGlow.GetComponent<Renderer>()
            };

            // light that "comes from outside" when the door opens
            var doorway = new[]
            {
                PointLight("Doorway_Light1", root, new Vector3(25f, 3f, 108.5f), new Color(1f, 0.85f, 0.65f), 0f, 14f, true),
                PointLight("Doorway_Light2", root, new Vector3(25f, 2f, 103f), new Color(1f, 0.85f, 0.65f), 0f, 10f)
            };

            Lamp("Lamp_Final1", root, new Vector3(21f, 6.4f, 101f), new Color(0.6f, 0.75f, 1f), 1.2f, 10f, true, 0.08f);
            Lamp("Lamp_Final2", root, new Vector3(29f, 6.4f, 101f), new Color(0.6f, 0.75f, 1f), 1.0f, 10f, true, 0.08f, true);

            // ---- exit door console (code given by UNIT-6 when it trusts the player) ----
            var console = new GameObject("ExitConsole").transform;
            console.SetParent(root, false);
            console.position = new Vector3(29.7f, 0, 105.35f);
            BoxL("Stand", console, new Vector3(0, 0.55f, 0), new Vector3(0.5f, 1.1f, 0.4f), M.DarkMetal, true);
            BoxL("Stand_Stripe", console, new Vector3(0, 0.15f, -0.205f), new Vector3(0.5f, 0.08f, 0.01f), M.Hazard);
            var pad = BoxL("Keypad", console, new Vector3(0, 1.32f, -0.05f), new Vector3(0.36f, 0.44f, 0.08f), M.DarkMetal, true);
            pad.transform.localRotation = Quaternion.Euler(-20f, 0, 0);
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++)
                    BoxL("Key", pad.transform, new Vector3(-0.09f + c * 0.09f, -0.15f + r * 0.07f, -0.045f), new Vector3(0.07f, 0.05f, 0.01f), M.Metal);
            var consoleLight = BoxL("StatusLight", pad.transform, new Vector3(0, 0.18f, -0.045f), new Vector3(0.2f, 0.035f, 0.01f), M.GlowRed);
            var exitConsole = Add<KeypadController>(pad);
            exitConsole.Configure(null, consoleLight.GetComponent<Renderer>());
            exitConsole.SetPrompt("Kapı Konsolunu Kullan");
            Highlight(pad);
            Text("Console_Label", root, new Vector3(29.7f, 1.85f, 105.15f), new Vector3(0, 180, 0), "KAPI KONSOLU", 1.6f, new Color(0.5f, 0.8f, 1f), 1.2f, 0.3f);
            PointLight("Console_Glow", root, new Vector3(29.7f, 1.6f, 104.8f), new Color(0.4f, 0.7f, 1f), 0.6f, 3f);
            PointLight("Door_Red", root, new Vector3(25f, 5.2f, 104.5f), new Color(1f, 0.1f, 0.05f), 1.2f, 7f);

            Add<LevelZone>(TriggerBox("Zone_Level5", root, new Vector3(18.2f, 0, 96.3f), new Vector3(33.8f, 2.5f, 98.6f))).Configure(5);
            ctx.Checkpoints[4] = Point(ctx, "Checkpoint_Final", new Vector3(25f, 0, 97.4f), 0f);
            ctx.FinalRobotWait = Point(ctx, "Robot_FinalWait", new Vector3(29.4f, 0, 98.2f), 180f);

            var victory = TriggerBox("Zone_Victory", ctx.Garden, new Vector3(18f, 0, 109.5f), new Vector3(32f, 3f, 112f));
            victory.AddComponent<VictoryZone>();
            ctx.RobotGardenPoint = Point(ctx, "Robot_Garden", new Vector3(26.6f, 0, 112.6f), 0f);

            ctx.FinalDoor = Add<FinalDoorPuzzle>(root.gameObject);
            ctx.FinalDoor.Configure(pp, plate, halves, new[] { new Vector3(-3.2f, 0, 0), new Vector3(3.2f, 0, 0) }, bolts, slotLights,
                cables.ToArray(), panelLight, ctx.Moon, doorway, ctx.WorkstationClue);
            ctx.FinalDoor.SetExitConsole(exitConsole);
        }

        // =====================================================================
        // Garden (outside)
        // =====================================================================

        private static void BuildGarden(Ctx ctx)
        {
            var g = ctx.Garden;
            Floor("Garden_Ground", g, -20, 70, 106.3f, 160, 0, M.Grass, SurfaceType.Grass);
            Floor("Garden_Strip_A", g, 32.2f, 70, -10, 40, 0, M.Grass, SurfaceType.Grass);
            Floor("Garden_Strip_B", g, 34.2f, 70, 40, 106.3f, 0, M.Grass, SurfaceType.Grass);

            for (int i = 0; i < 14; i++)
            {
                var s = Box("PathStone" + i, g, new Vector3(25f + Mathf.Sin(i * 0.5f) * 0.6f, 0.02f, 108f + i * 2f), new Vector3(1.6f, 0.06f, 1.4f), M.Path, false);
                s.transform.rotation = Quaternion.Euler(0, i * 13f, 0);
            }

            var rnd = new System.Random(77);
            var treeSpots = new List<Vector3>();
            for (int i = 0; i < 26; i++)
            {
                float x = (float)(rnd.NextDouble() * 80 - 15);
                float z = (float)(rnd.NextDouble() * 45 + 112);
                if (Mathf.Abs(x - 25f) < 5f) x += x < 25f ? -6f : 6f;
                treeSpots.Add(new Vector3(x, 0, z));
            }
            for (int i = 0; i < 10; i++) treeSpots.Add(new Vector3(40f + (float)rnd.NextDouble() * 25f, 0, (float)rnd.NextDouble() * 90f));
            foreach (var p in treeSpots) Tree(g, p, 0.8f + (float)rnd.NextDouble() * 0.6f);

            for (int i = 0; i < 18; i++)
            {
                var b = Prim(PrimitiveType.Sphere, "Bush" + i, g,
                    new Vector3((float)(rnd.NextDouble() * 30 + 10), 0.3f, (float)(rnd.NextDouble() * 30 + 110)),
                    new Vector3(1.4f, 0.8f, 1.2f), M.Leaves);
                if (Mathf.Abs(b.transform.position.x - 25f) < 2.5f) Object.DestroyImmediate(b);
            }

            for (int i = 0; i < 4; i++)
            {
                float z = 112f + i * 7f;
                float x = i % 2 == 0 ? 22.6f : 27.4f;
                Prim(PrimitiveType.Cylinder, "LampPost" + i, g, new Vector3(x, 1.4f, z), new Vector3(0.12f, 1.4f, 0.12f), M.DarkMetal, true);
                Prim(PrimitiveType.Sphere, "LampGlobe" + i, g, new Vector3(x, 2.9f, z), Vector3.one * 0.35f, M.Bulb);
                PointLight("GardenLight" + i, g, new Vector3(x, 2.9f, z), new Color(1f, 0.8f, 0.5f), 1.6f, 10f, i == 0);
            }
            // the lantern visible through the barred window
            Prim(PrimitiveType.Cylinder, "WindowLampPost", g, new Vector3(37f, 1.4f, 16f), new Vector3(0.12f, 1.4f, 0.12f), M.DarkMetal, true);
            Prim(PrimitiveType.Sphere, "WindowLampGlobe", g, new Vector3(37f, 2.9f, 16f), Vector3.one * 0.35f, M.Bulb);
            PointLight("WindowLampLight", g, new Vector3(37f, 2.9f, 16f), new Color(1f, 0.8f, 0.5f), 1.6f, 10f);
            BoxMinMax("Gate_Post_L", g, new Vector3(41f, 0, 13.6f), new Vector3(41.3f, 2.2f, 13.9f), M.DarkMetal);
            BoxMinMax("Gate_Post_R", g, new Vector3(41f, 0, 18.1f), new Vector3(41.3f, 2.2f, 18.4f), M.DarkMetal);
            for (int i = 0; i < 9; i++)
                BoxMinMax("Gate_Bar" + i, g, new Vector3(41.1f, 0, 14.1f + i * 0.45f), new Vector3(41.16f, 1.9f, 14.16f + i * 0.45f), M.DarkMetal, false);

            // fence around the garden
            for (int i = 0; i < 30; i++)
            {
                BoxMinMax("Fence_N" + i, g, new Vector3(-10f + i * 2.5f, 0, 155f), new Vector3(-9.9f + i * 2.5f, 1.3f, 155.1f), M.WoodDark, false);
            }
            BoxMinMax("Fence_Rail", g, new Vector3(-10f, 1.0f, 155f), new Vector3(65f, 1.08f, 155.1f), M.WoodDark, false);
            Box("Bench", g, new Vector3(30.5f, 0.45f, 120f), new Vector3(1.8f, 0.1f, 0.5f), M.Wood);
            Box("Bench_Back", g, new Vector3(30.5f, 0.8f, 120.25f), new Vector3(1.8f, 0.6f, 0.08f), M.Wood, false);
        }

        private static void Tree(Transform p, Vector3 pos, float scale)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(p, false);
            t.position = pos;
            t.localScale = Vector3.one * scale;
            Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 1.6f, 0), new Vector3(0.35f, 1.6f, 0.35f), M.Bark, true);
            Prim(PrimitiveType.Sphere, "Crown1", t, new Vector3(0, 3.8f, 0), new Vector3(2.8f, 2.4f, 2.8f), M.Leaves);
            Prim(PrimitiveType.Sphere, "Crown2", t, new Vector3(0.6f, 4.6f, 0.3f), new Vector3(2f, 1.8f, 2f), M.Leaves);
            Prim(PrimitiveType.Sphere, "Crown3", t, new Vector3(-0.5f, 4.4f, -0.4f), new Vector3(1.8f, 1.6f, 1.8f), M.Leaves);
        }
    }
}
