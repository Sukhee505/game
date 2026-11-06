using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Environment;
using SevenMinutes.Puzzles;

namespace SevenMinutes.EditorTools
{
    public static partial class HouseBuilder
    {
        // =====================================================================
        // Ground floor hallway (x 16..32, z 14..18)
        // =====================================================================

        private static void BuildGroundHall(Ctx ctx)
        {
            var root = new GameObject("Floor1_Hallway").transform;
            root.SetParent(ctx.House, false);

            Floor("Floor", root, 16, 32, 14, 18, 0, M.WoodFloor);
            Ceiling("Ceiling", root, 16, 32, 14, 18, 4f, M.Plaster);

            WallX("Wall_South_A", root, 16, 24, 14, 0, 4, M.WallpaperHall, new Gap(20f, 1.4f, 2.3f));
            WallX("Wall_South_B", root, 28, 32.15f, 14, 0, 4, M.WallpaperHall);
            WallZ("Wall_West", root, 14, 20, 16, 0, 4, M.WallpaperHall, new Gap(16f, 1.4f, 2.3f));
            WallX("Wall_North", root, 15.85f, 32.15f, 18, 0, 4, M.WallpaperHall, new Gap(19f, 1.4f, 2.3f), new Gap(25f, 1.6f, 2.4f));

            // east wall with a barred window: the garden (and freedom) is visible but unreachable
            BoxMinMax("Wall_East_A", root, new Vector3(31.85f, 0, 14f), new Vector3(32.15f, 4, 14.6f), M.WallpaperHall);
            BoxMinMax("Wall_East_B", root, new Vector3(31.85f, 0, 17.4f), new Vector3(32.15f, 4, 18f), M.WallpaperHall);
            BoxMinMax("Wall_East_Sill", root, new Vector3(31.85f, 0, 14.6f), new Vector3(32.15f, 1.0f, 17.4f), M.WallpaperHall);
            BoxMinMax("Wall_East_Top", root, new Vector3(31.85f, 3.0f, 14.6f), new Vector3(32.15f, 4, 17.4f), M.WallpaperHall);
            BoxMinMax("Window_Glass", root, new Vector3(31.97f, 1.0f, 14.6f), new Vector3(32.03f, 3.0f, 17.4f), M.Glass);
            for (int i = 0; i < 7; i++)
                BoxMinMax("Window_Bar" + i, root, new Vector3(31.8f, 1.0f, 14.75f + i * 0.42f), new Vector3(31.86f, 3.0f, 14.81f + i * 0.42f), M.DarkMetal, false);
            var window = BoxMinMax("Window_Look", root, new Vector3(31.75f, 1.0f, 14.6f), new Vector3(31.8f, 3.0f, 17.4f), M.Glass);
            Add<ReadableObject>(window).Configure("Dışarı Bak", "Bahçe",
                "Parmaklıkların arasından bir bahçe, bir fener ve bir kapı görüyorsun.\nÖzgürlük hemen orada.\n\nCam kurşun geçirmez. Parmaklıklar kaynaklanmış.\nBaşka bir çıkış yolu olmalı.", false);

            SwingDoor("Door_LivingRoom", root, new Vector3(20f, 0, 14f), 1.4f, true, "kapı", "Kilitli. Diğer taraftan ağır bir şey kapıyı engelliyor.");
            SwingDoor("Door_Basement", root, new Vector3(19f, 0, 18f), 1.4f, true, "kapı", "Bodrum kapısı zincirle kapatılmış.");
            var tileDoor = SwingDoor("Door_TileRoom", root, new Vector3(25f, 0, 18f), 1.6f, false, "kapı", "", false, 2.4f);
            testDoors.Add(tileDoor);
            Text("Sign_Basement", root, new Vector3(19f, 2.65f, 17.83f), new Vector3(0, 0, 0), "BODRUM", 2f, new Color(0.6f, 0.55f, 0.45f), 1.5f, 0.35f);

            Lamp("Lamp_G1", root, new Vector3(20f, 3.2f, 16f), new Color(0.75f, 0.8f, 1f), 0.8f, 7f, true, 0.15f);
            Lamp("Lamp_G2", root, new Vector3(29f, 3.2f, 16f), new Color(0.75f, 0.8f, 1f), 0.7f, 7f, true, 0.3f);

            // decoration
            BoxMinMax("Clock_Body", root, new Vector3(16.2f, 0, 17.2f), new Vector3(16.7f, 2.1f, 17.75f), M.WoodDark);
            Box("Clock_Face", root, new Vector3(16.72f, 1.7f, 17.48f), new Vector3(0.02f, 0.3f, 0.3f), M.Paper, false);
            Text("Clock_Time", root, new Vector3(16.74f, 1.7f, 17.48f), new Vector3(0, -90, 0), "5:05", 1.5f, Color.black, 0.3f, 0.2f);
            Blood(root, new Vector3(19.2f, 0.012f, 17.3f), 0.8f, 0f);
            Blood(root, new Vector3(20.4f, 0.012f, 16.6f), 0.5f, 45f);
            BoxMinMax("Runner", root, new Vector3(17f, 0, 15.3f), new Vector3(31f, 0.01f, 16.7f), M.Carpet, false);
        }

        // =====================================================================
        // Easter egg room B — abandoned workstation (x 8..16, z 14..20)
        // =====================================================================

        private static void BuildEasterRoomB(Ctx ctx)
        {
            var root = new GameObject("EasterEgg_RoomB_Workshop").transform;
            root.SetParent(ctx.House, false);
            Floor("Floor", root, 8, 16, 14, 20, 0, M.Concrete, SurfaceType.Concrete);
            Ceiling("Ceiling", root, 8, 16, 14, 20, 4f, M.Plaster);
            WallZ("Wall_West", root, 14, 20, 8, 0, 4, M.ConcreteDark);
            WallX("Wall_North", root, 7.85f, 16.15f, 20, 0, 4, M.ConcreteDark);

            // workstation
            BoxMinMax("Desk", root, new Vector3(9.8f, 0.74f, 19.0f), new Vector3(13.2f, 0.8f, 19.8f), M.DarkMetal);
            BoxMinMax("DeskLegL", root, new Vector3(9.9f, 0, 19.1f), new Vector3(10.0f, 0.74f, 19.7f), M.DarkMetal);
            BoxMinMax("DeskLegR", root, new Vector3(13.0f, 0, 19.1f), new Vector3(13.1f, 0.74f, 19.7f), M.DarkMetal);
            var monitor = Box("Workstation_Monitor", root, new Vector3(11.5f, 1.3f, 19.6f), new Vector3(1.0f, 0.62f, 0.08f), M.TvBody);
            ImageQuad("Workstation_Screen", root, new Vector3(11.5f, 1.3f, 19.555f), Vector3.zero, new Vector2(0.92f, 0.54f), M.Workstation);
            Box("Keyboard", root, new Vector3(11.5f, 0.82f, 19.25f), new Vector3(0.6f, 0.03f, 0.2f), M.DarkMetal, false);
            var clue = Add<ReadableObject>(monitor);
            clue.Configure("İş İstasyonunu Kullan", "UNIT-7 BAKIM TERMİNALİ",
                "PROJE UNIT-7 — DURUM: KONTROLDEN ÇIKTI\n\n" +
                "UNIT-6 ÇIKIŞ PROTOKOLÜ:\n" +
                "  • UNIT-6 bir yapay zekâdır. Ziyaretçiye GÜVEN TESTİ uygular.\n" +
                "  • Güven %60'a ulaşırsa iki sayılık kapı kodunu verir.\n" +
                "  • Kod, kapının yanındaki konsola girilir.\n" +
                "  • Not: UNIT-6 yalanı ve kabalığı sezer. Dürüst ol.\n\n" +
                "UYARI: kapı bekçisi UNIT-6 yalnızca çıkışın yanındaki ağır plakaya basılı tutulurken güç alır.\n" +
                "Bunu sadece UNIT-7 kadar ağır bir ünite yapabilir.", true);
            ctx.WorkstationClue = clue;
            PointLight("ScreenGlow", root, new Vector3(11.5f, 1.4f, 19.0f), new Color(0.3f, 1f, 0.45f), 0.8f, 4f);

            // prototype robot head — the mysterious object
            BoxMinMax("SideTable", root, new Vector3(14.2f, 0, 18.8f), new Vector3(15.6f, 0.8f, 19.7f), M.Rust);
            var head = Box("Prototype_Head", root, new Vector3(14.9f, 0.95f, 19.25f), new Vector3(0.32f, 0.24f, 0.28f), M.RobotBody);
            head.transform.rotation = Quaternion.Euler(0, -25, 12);
            BoxL("Face", head.transform, new Vector3(0, 0, 0.145f), new Vector3(0.27f, 0.15f, 0.01f), M.RobotJoint);
            BoxL("EyeL", head.transform, new Vector3(-0.07f, 0.01f, 0.152f), new Vector3(0.06f, 0.03f, 0.01f), M.Pupil);
            BoxL("EyeR", head.transform, new Vector3(0.07f, 0.01f, 0.152f), new Vector3(0.06f, 0.03f, 0.01f), M.Pupil);
            Add<ReadableObject>(head).Configure("Garip Nesneyi İncele", "Prototip bir kafa",
                "Eski bir robot kafası — tıpkı UNIT-7'ninki gibi.\nAltına kazınmış:\n\n\"UNIT-6. Kapıyı korumak için yapıldı.\nAçmak için değil.\"\n\nYanında el yazısıyla bir etiket: \"UNIT-7 hissetmeyi öğreniyor. Onlara yardım etmeden önce kapatın.\"", false);

            for (int i = 0; i < 4; i++)
                Box("Cable" + i, root, new Vector3(10f + i * 1.3f, 0.03f, 17f - i * 0.4f), new Vector3(1.6f, 0.05f, 0.05f), M.RobotJoint, false)
                    .transform.rotation = Quaternion.Euler(0, i * 37f, 0);
            Box("Crate", root, new Vector3(9f, 0.4f, 15f), new Vector3(0.8f, 0.8f, 0.8f), M.Wood);
            Box("Crate2", root, new Vector3(9.1f, 1.1f, 15.1f), new Vector3(0.6f, 0.6f, 0.6f), M.Wood);
            Text("Wall_Message", root, new Vector3(8.17f, 2.2f, 17f), new Vector3(0, -90, 0),
                "ONLARI SEÇTİ.\nSENİ DE SEÇECEK.", 3f, new Color(0.5f, 0.02f, 0.02f), 3.5f, 1.2f);
            Lamp("Lamp_Workshop", root, new Vector3(12f, 3.2f, 16.5f), new Color(0.6f, 0.85f, 1f), 0.6f, 7f, true, 0.4f);

            Add<RobotLineZone>(TriggerBox("Zone_RoomB", root, new Vector3(8.3f, 0, 14.3f), new Vector3(15.7f, 2.5f, 19.7f)))
                .Configure("Burası... burası benim yapıldığım yer. O iş istasyonuna bak.", true, 1);
        }

        // =====================================================================
        // LEVEL 2 — Memory floor (x 22..31, z 18..40)
        // =====================================================================

        private static void BuildTileRoom(Ctx ctx)
        {
            var root = new GameObject("Level2_MemoryFloor").transform;
            root.SetParent(ctx.House, false);
            const float tileSize = 1.5f, x0 = 22.15f, z0 = 21f;

            Floor("Start_Platform", root, 22, 31, 18, 21, 0, M.Concrete, SurfaceType.Concrete);
            Floor("End_Platform", root, 22, 31, 36, 40, 0, M.Concrete, SurfaceType.Concrete);
            var catwalk = Floor("Robot_Catwalk", root, 28.45f, 31, 21, 36, 0, M.MetalFloor, SurfaceType.Metal);
            _ = catwalk;
            Ceiling("Ceiling", root, 22, 31, 18, 40, 5f, M.ConcreteDark);
            WallZ("Wall_West", root, 18, 40, 22, -4, 9, M.ConcreteDark);
            WallZ("Wall_East", root, 18, 40, 31, 0, 5, M.ConcreteDark);
            BoxMinMax("Divider_Glass", root, new Vector3(28.2f, -4, 21f), new Vector3(28.4f, 5f, 36f), M.Glass);
            BoxMinMax("Divider_Rail", root, new Vector3(28.15f, 1.0f, 21f), new Vector3(28.45f, 1.08f, 36f), M.DarkMetal, false);

            // pit under the tiles
            var pit = new GameObject("Pit").transform;
            pit.SetParent(root, false);
            pit.gameObject.AddComponent<NavMeshIgnore>();
            BoxMinMax("Pit_Floor", pit, new Vector3(22f, -4.3f, 21f), new Vector3(28.3f, -4f, 36f), M.Stone);
            BoxMinMax("Pit_Wall_S", pit, new Vector3(22f, -4f, 20.8f), new Vector3(28.3f, -0.3f, 21f), M.Stone);
            BoxMinMax("Pit_Wall_N", pit, new Vector3(22f, -4f, 36f), new Vector3(28.3f, -0.3f, 36.2f), M.Stone);
            var kill = TriggerBox("Pit_Kill", pit, new Vector3(22f, -2.8f, 21f), new Vector3(28.3f, -1.3f, 36f));
            Add<HazardVolume>(kill).Configure("Zemin çöktü — düştün!", FailKind.Fall);
            PointLight("Pit_Glow", pit, new Vector3(25f, -3f, 28.5f), new Color(0.6f, 0.05f, 0.05f), 1.2f, 9f);

            // 4 x 10 tiles
            var tilesRoot = new GameObject("Tiles").transform;
            tilesRoot.SetParent(root, false);
            tilesRoot.gameObject.AddComponent<NavMeshIgnore>();
            var tiles = new MemoryTile[TileMemoryPuzzle.Columns * TileMemoryPuzzle.Rows];
            for (int r = 0; r < TileMemoryPuzzle.Rows; r++)
            for (int c = 0; c < TileMemoryPuzzle.Columns; c++)
            {
                var center = new Vector3(x0 + c * tileSize + tileSize / 2, -0.1f, z0 + r * tileSize + tileSize / 2);
                var t = Box($"Tile_{c}_{r}", tilesRoot, center, new Vector3(tileSize - 0.06f, 0.2f, tileSize - 0.06f), M.Tile);
                t.AddComponent<FootstepSurface>().Surface = SurfaceType.Tile;
                var mt = t.AddComponent<MemoryTile>();
                mt.Configure(t.GetComponent<Renderer>());
                tiles[r * TileMemoryPuzzle.Columns + c] = mt;
            }

            // start button
            var pedestal = Box("Button_Pedestal", root, new Vector3(23.2f, 0.5f, 19.4f), new Vector3(0.5f, 1.0f, 0.5f), M.DarkMetal);
            var cap = Prim(PrimitiveType.Cylinder, "Button_Cap", pedestal.transform, new Vector3(0, 0.53f, 0), new Vector3(0.3f, 0.04f, 0.3f), M.GlowGreen);
            var startButton = Add<PushButton>(pedestal);
            startButton.Configure("Düğmeye Bas — yolu göster", cap.transform, true, "");
            Highlight(pedestal);
            Text("Sign_Tiles", root, new Vector3(26.5f, 3.2f, 18.17f), Vector3.zero,
                "<b>BAS. HATIRLA. HAYATTA KAL.</b>", 3f, new Color(0.8f, 0.75f, 0.6f), 5f, 0.6f).transform.rotation = Quaternion.Euler(0, 180, 0);

            // gates for the robot catwalk
            var gates = new List<DoorController>();
            foreach (float gz in new[] { 21.1f, 35.9f })
            {
                var g = Box("Robot_Gate", root, new Vector3(29.725f, 1.25f, gz), new Vector3(2.5f, 2.5f, 0.12f), M.Hazard);
                g.AddComponent<NavMeshIgnore>();
                var gate = g.AddComponent<DoorController>();
                gate.Configure("kapı", true, false, "", g.transform, DoorMotion.Slide, 0f, new Vector3(0, 2.6f, 0), 1.2f,
                    Obstacle(g, new Vector3(2.5f, 2.5f, 0.12f)), Audio.SoundId.Grind);
                gates.Add(gate);
                testDoors.Add(gate);
            }

            // end: lever + exit door to the lava room
            var exitDoor = SlideDoor("Door_ToLava", root, new Vector3(25f, 0, 40f), 1.6f, 2.4f, "Kilitli. Kolu çek.");
            testDoors.Add(exitDoor);
            var lever = BuildLever("Lever_Tiles", root, new Vector3(23.4f, 0, 39.3f), 180f, "Kol kilitli. Önce zeminden geç.");
            var endZone = Add<PlayerZone>(TriggerBox("Zone_TilesEnd", root, new Vector3(22.2f, 0, 36.3f), new Vector3(28.1f, 2f, 39.9f)));

            var levelZone = Add<LevelZone>(TriggerBox("Zone_Level2", root, new Vector3(22.2f, 0, 18.3f), new Vector3(30.8f, 2.5f, 20.8f)));
            levelZone.Configure(2);
            ctx.Checkpoints[1] = Point(ctx, "Checkpoint_L2", new Vector3(25.5f, 0, 19.5f), 0f);

            Lamp("Lamp_Start", root, new Vector3(26.5f, 4.2f, 19.5f), new Color(0.8f, 0.85f, 1f), 1.0f, 8f, true, 0.1f, true);
            Lamp("Lamp_End", root, new Vector3(26.5f, 4.2f, 38f), new Color(0.8f, 0.85f, 1f), 1.0f, 8f, true, 0.1f);
            PointLight("Tiles_Fill", root, new Vector3(25f, 4.5f, 28.5f), new Color(0.5f, 0.55f, 0.7f), 0.6f, 12f);
            PointLight("Catwalk_Light", root, new Vector3(29.7f, 3f, 28.5f), new Color(1f, 0.5f, 0.2f), 0.5f, 8f);

            ctx.Tiles = Add<TileMemoryPuzzle>(root.gameObject);
            ctx.Tiles.Configure(tiles, startButton, endZone, lever, exitDoor, gates.ToArray(),
                Point(ctx, "Robot_TileMeet", new Vector3(27.3f, 0, 38.6f), 0f));
        }

        private static DoorController SlideDoor(string name, Transform parent, Vector3 bottomCenter, float width, float height, string lockedText)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = bottomCenter;
            root.gameObject.AddComponent<NavMeshIgnore>();
            BoxL("Frame_L", root, new Vector3(-width / 2 - 0.08f, height / 2, 0), new Vector3(0.16f, height, T + 0.1f), M.Hazard);
            BoxL("Frame_R", root, new Vector3(width / 2 + 0.08f, height / 2, 0), new Vector3(0.16f, height, T + 0.1f), M.Hazard);
            var panel = BoxL("Panel", root, new Vector3(0, height / 2, 0), new Vector3(width, height, 0.12f), M.DoorDark, true);
            var obstacle = Obstacle(panel, new Vector3(width, height, 0.12f));
            var door = root.gameObject.AddComponent<DoorController>();
            door.Configure("Kapı", true, true, lockedText, panel.transform, DoorMotion.Slide, 0f, new Vector3(0, height + 0.05f, 0), 1.6f,
                obstacle, Audio.SoundId.Grind);
            Highlight(root.gameObject);
            return door;
        }

        private static LeverController BuildLever(string name, Transform parent, Vector3 pos, float yaw, string lockedText, bool usable = false)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            BoxL("Base", root, new Vector3(0, 0.55f, 0), new Vector3(0.5f, 1.1f, 0.35f), M.DarkMetal, true);
            BoxL("Plate", root, new Vector3(0, 0.95f, 0.18f), new Vector3(0.4f, 0.4f, 0.02f), M.Hazard);
            var pivot = Node("Pivot", root, new Vector3(0, 0.95f, 0.22f));
            BoxL("Stick", pivot, new Vector3(0, 0.3f, 0), new Vector3(0.06f, 0.6f, 0.06f), M.Metal, true);
            Prim(PrimitiveType.Sphere, "Knob", pivot, new Vector3(0, 0.62f, 0), Vector3.one * 0.13f, M.GlowRed);
            var light = BoxL("Indicator", root, new Vector3(0, 1.18f, 0.18f), new Vector3(0.1f, 0.06f, 0.02f), M.GlowRed);
            var lever = root.gameObject.AddComponent<LeverController>();
            lever.Configure(pivot, light.GetComponent<Renderer>(), usable, lockedText);
            Highlight(root.gameObject);
            return lever;
        }
    }
}
