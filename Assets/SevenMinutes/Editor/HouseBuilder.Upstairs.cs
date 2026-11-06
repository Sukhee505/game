using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Environment;
using SevenMinutes.Puzzles;

namespace SevenMinutes.EditorTools
{
    public static partial class HouseBuilder
    {
        // Upper floor: y = 4. Footprint x 0..28, z 0..14. Stairwell x 24..28, z 4..14.
        private const float U = 4f;

        private static void BuildUpperFloor(Ctx ctx)
        {
            var root = new GameObject("Floor2_Upstairs").transform;
            root.SetParent(ctx.House, false);

            Floor("Floor_Main", root, 0, 24, 0, 14, U, M.WoodFloor);
            Floor("Floor_Landing", root, 24, 28, 0, 4, U, M.WoodFloor);
            Ceiling("Ceiling", root, 0, 28, 0, 14, 8f, M.Plaster);

            // exterior (full height for both floors)
            WallX("Ext_South", root, -0.15f, 28.15f, 0, 0, 8, M.Brick);
            WallZ("Ext_West", root, 0, 14, 0, 0, 8, M.Brick);
            WallZ("Ext_East", root, 0, 14, 28, 0, 8, M.Brick);
            WallX("Ext_North_Upper", root, 0, 28, 14, U, 4, M.WallpaperHall);
            WallX("Ext_North_Lower", root, -0.15f, 16, 14, 0, U, M.Brick);

            // interior
            WallZ("Wall_BedroomEast", root, 0, 14, 8, U, 4, M.Wallpaper, new Gap(2f, 1.6f, 2.4f));
            WallX("Wall_BedroomNorth", root, 0, 8, 8, U, 4, M.Wallpaper);
            WallX("Wall_HallNorth", root, 8, 20, 4, U, 4, M.WallpaperHall, new Gap(11f, 1.4f, 2.3f), new Gap(17f, 1.4f, 2.3f));
            WallX("Wall_RoomA_North", root, 8, 14, 10, U, 4, M.WallpaperRoomA);
            WallZ("Wall_RoomA_East", root, 4, 10, 14, U, 4, M.WallpaperRoomA);
            WallX("Wall_Closet_North", root, 14, 20, 8, U, 4, M.WallpaperHall);
            WallZ("Wall_Branch_West", root, 4, 12, 20, U, 4, M.WallpaperHall);
            WallZ("Wall_Stairwell", root, 4, 14, 24, 0, 8, M.WallpaperHall);
            WallX("Wall_Branch_End", root, 20, 24, 12, U, 4, M.WallpaperHall, new Gap(22f, 1.4f, 2.3f));

            // doors
            var roomADoor = SwingDoor("Door_RoomA", root, new Vector3(11f, U, 4f), 1.4f, false, "kapı", "");
            var closetDoor = SwingDoor("Door_Closet", root, new Vector3(17f, U, 4f), 1.4f, false, "kapı", "");
            SwingDoor("Door_NoEntry", root, new Vector3(22f, U, 12f), 1.4f, true, "kapı",
                "Diğer taraftan kilitli. Ahşabı derin çizikler kaplıyor.");
            testDoors.Add(roomADoor); _ = closetDoor;
            Text("Sign_NoEntry", root, new Vector3(22f, U + 2.65f, 11.83f), new Vector3(0, 0, 0), "<b>GİRİLMEZ</b>", 2.4f, new Color(0.7f, 0.05f, 0.05f), 1.6f, 0.4f);

            // hallway lamps and decoration
            Lamp("Lamp_Hall1", root, new Vector3(12f, U + 3.1f, 2f), new Color(0.75f, 0.8f, 1f), 0.9f, 7f, true, 0.12f);
            Lamp("Lamp_Hall2", root, new Vector3(21f, U + 3.1f, 2f), new Color(0.75f, 0.8f, 1f), 0.8f, 7f, true, 0.2f);
            Lamp("Lamp_Branch", root, new Vector3(22f, U + 3.1f, 9f), new Color(1f, 0.3f, 0.2f), 0.6f, 5f, true, 0.3f);
            Lamp("Lamp_Closet", root, new Vector3(17f, U + 3.1f, 6f), new Color(1f, 0.8f, 0.55f), 0.35f, 4f, true, 0.4f);
            for (int i = 0; i < 3; i++)
                PictureFrame(root, new Vector3(10.5f + i * 4.3f, U + 1.9f, 0.17f), new Vector3(0, 0, 0), i == 1 ? M.Photo : M.WoodDark);
            BoxMinMax("Hall_Runner", root, new Vector3(8.5f, U, 1.2f), new Vector3(23.5f, U + 0.01f, 2.8f), M.Carpet, false);
            Box("Hall_SideTable", root, new Vector3(19f, U + 0.4f, 0.45f), new Vector3(1.2f, 0.8f, 0.5f), M.WoodDark);
            Blood(root, new Vector3(18.4f, U + 0.012f, 2.6f), 1.1f, 20f);
            Blood(root, new Vector3(22.2f, U + 0.012f, 10.8f), 0.8f, 75f);

            // closet: junk and a fake note on the shelf
            Box("Closet_Shelf", root, new Vector3(17f, U + 1.2f, 7.65f), new Vector3(5.4f, 0.06f, 0.5f), M.WoodDark);
            Box("Closet_Box1", root, new Vector3(15.2f, U + 0.3f, 7.4f), new Vector3(0.6f, 0.6f, 0.6f), M.Wood);
            Box("Closet_Box2", root, new Vector3(18.6f, U + 0.25f, 7.3f), new Vector3(0.5f, 0.5f, 0.7f), M.Wood);
            var closetNote = Box("Closet_Note", root, new Vector3(17f, U + 1.25f, 7.6f), new Vector3(0.3f, 0.01f, 0.22f), M.Paper);
            Add<ReadableObject>(closetNote).Configure("Notu Oku", "Rafa iğnelenmiş bir not",
                "Koridorun sonundaki kapıdan geçme.\nHiçbir yere çıkmıyor.\nBu evde hiçbir şey bir yere çıkmıyor.", false);

            Add<RobotLineZone>(TriggerBox("Zone_Branch", root, new Vector3(20.2f, U, 8f), new Vector3(23.8f, U + 2.5f, 11.8f)))
                .Configure("Bu koridor çıkmaz. Merdivenleri bulmalıyız.", true, 1);
        }

        private static DoorController SwingDoor(string name, Transform parent, Vector3 bottomCenter, float width, bool locked,
            string doorName, string lockedText, bool alongZ = false, float height = 2.3f, bool playerCanOpen = true)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = bottomCenter;
            if (alongZ) root.rotation = Quaternion.Euler(0, 90, 0);
            root.gameObject.AddComponent<NavMeshIgnore>(); // doors carve the NavMesh at runtime instead

            // frame
            BoxL("Frame_L", root, new Vector3(-width / 2 - 0.05f, height / 2, 0), new Vector3(0.1f, height, T + 0.06f), M.WoodDark);
            BoxL("Frame_R", root, new Vector3(width / 2 + 0.05f, height / 2, 0), new Vector3(0.1f, height, T + 0.06f), M.WoodDark);
            BoxL("Frame_T", root, new Vector3(0, height + 0.05f, 0), new Vector3(width + 0.2f, 0.1f, T + 0.06f), M.WoodDark);

            var hinge = Node("Hinge", root, new Vector3(-width / 2, 0, 0));
            var panel = BoxL("Panel", hinge, new Vector3(width / 2, height / 2, 0), new Vector3(width - 0.04f, height - 0.02f, 0.06f), M.WoodDark, true);
            BoxL("Knob", panel.transform, new Vector3(width / 2 - 0.15f, 0, 0.06f), new Vector3(0.06f, 0.06f, 0.06f), M.Gold);
            BoxL("Knob2", panel.transform, new Vector3(width / 2 - 0.15f, 0, -0.06f), new Vector3(0.06f, 0.06f, 0.06f), M.Gold);
            var obstacle = Obstacle(panel, new Vector3(width - 0.04f, height - 0.02f, 0.06f));

            var door = root.gameObject.AddComponent<DoorController>();
            door.Configure(char.ToUpper(doorName[0]) + doorName.Substring(1), locked, playerCanOpen,
                lockedText, hinge, DoorMotion.Swing, 100f, Vector3.zero, 1.3f, obstacle, Audio.SoundId.Creak);
            Highlight(root.gameObject);
            return door;
        }

        private static void PictureFrame(Transform p, Vector3 pos, Vector3 euler, Material picture)
        {
            var f = Box("Picture", p, pos, new Vector3(0.9f, 0.7f, 0.04f), M.WoodDark, false);
            f.transform.rotation = Quaternion.Euler(euler);
            var img = ImageQuad("Image", f.transform, Vector3.zero, Vector3.zero, new Vector2(0.75f, 0.55f), picture);
            img.transform.localPosition = new Vector3(0, 0, 0.0215f);
            img.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        private static void Blood(Transform p, Vector3 pos, float size, float yaw)
        {
            var b = Box("BloodStain", p, pos, new Vector3(size, 0.005f, size), M.Blood, false);
            b.transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        // =====================================================================
        // Bedroom (x 0..8, z 0..8, y 4) — LEVEL 1
        // =====================================================================

        private static void BuildBedroom(Ctx ctx)
        {
            var root = new GameObject("Level1_Bedroom").transform;
            root.SetParent(ctx.House, false);
            var containers = new List<SearchableContainer>();

            BoxMinMax("Rug", root, new Vector3(1.5f, U, 2.5f), new Vector3(5.5f, U + 0.012f, 6.5f), M.Carpet, false);

            // ---- bed (head against west wall) ----
            var bed = Node("Bed", root, Vector3.zero);
            BoxMinMax("Bed_Frame", bed, new Vector3(0.2f, U, 4f), new Vector3(2.4f, U + 0.35f, 5.6f), M.WoodDark);
            BoxMinMax("Bed_Mattress", bed, new Vector3(0.25f, U + 0.35f, 4.05f), new Vector3(2.35f, U + 0.55f, 5.55f), M.Fabric);
            BoxMinMax("Bed_Headboard", bed, new Vector3(0.15f, U, 3.95f), new Vector3(0.3f, U + 1.2f, 5.65f), M.WoodDark);
            BoxMinMax("Bed_Footboard", bed, new Vector3(2.35f, U, 3.95f), new Vector3(2.45f, U + 0.7f, 5.65f), M.WoodDark);
            Box("Bed_Pillow", bed, new Vector3(0.55f, U + 0.62f, 4.8f), new Vector3(0.4f, 0.14f, 0.9f), M.Pillow, false);
            BoxMinMax("Bed_Blanket", bed, new Vector3(1.0f, U + 0.55f, 4.0f), new Vector3(2.38f, U + 0.6f, 5.6f), M.Blanket, false);

            ctx.LyingPose = Point(ctx, "Pose_Lying", new Vector3(2.2f, U + 0.66f, 4.8f), 90f);
            ctx.SittingPose = Point(ctx, "Pose_Sitting", new Vector3(1.45f, U, 5.78f), 0f);
            ctx.StandingPose = Point(ctx, "Pose_Standing", new Vector3(1.6f, U, 6.5f), 60f);
            ctx.Checkpoints[0] = ctx.StandingPose;
            ctx.CamFace = CamPoint(ctx, "Cam_Face", new Vector3(1.25f, U + 1.45f, 4.45f), new Vector3(0.45f, U + 0.75f, 4.8f));
            ctx.CamRoom = CamPoint(ctx, "Cam_Room", new Vector3(4.6f, U + 2.1f, 2.2f), new Vector3(1.5f, U + 0.9f, 5.6f));
            ctx.CamMenu = CamPoint(ctx, "Cam_Menu", new Vector3(6.6f, U + 2.6f, 1.0f), new Vector3(2.5f, U + 0.8f, 5.2f));

            // ---- nightstand (drawer) + lamp ----
            containers.Add(Drawer("Nightstand", root, new Vector3(0.55f, U, 6.1f), new Vector3(0.6f, 0.6f, 0.55f), 90f, "Çekmece", "komodin çekmecesi"));
            var nightLamp = Lamp("Lamp_Nightstand", root, new Vector3(0.5f, U + 0.95f, 6.15f), new Color(1f, 0.6f, 0.35f), 0.7f, 3.5f, true, 0.05f, true, false);
            Prim(PrimitiveType.Cylinder, "LampBase", root, new Vector3(0.5f, U + 0.7f, 6.15f), new Vector3(0.12f, 0.1f, 0.12f), M.DarkMetal);

            // ---- desk with drawer + chair + lamp (west wall, south part) ----
            BoxMinMax("Desk_Top", root, new Vector3(0.2f, U + 0.72f, 1.0f), new Vector3(1.0f, U + 0.77f, 2.8f), M.Wood);
            BoxMinMax("Desk_Leg1", root, new Vector3(0.22f, U, 1.02f), new Vector3(0.3f, U + 0.72f, 1.1f), M.Wood);
            BoxMinMax("Desk_Leg2", root, new Vector3(0.9f, U, 1.02f), new Vector3(0.98f, U + 0.72f, 1.1f), M.Wood);
            BoxMinMax("Desk_Leg3", root, new Vector3(0.22f, U, 2.7f), new Vector3(0.3f, U + 0.72f, 2.78f), M.Wood);
            BoxMinMax("Desk_Leg4", root, new Vector3(0.9f, U, 2.7f), new Vector3(0.98f, U + 0.72f, 2.78f), M.Wood);
            containers.Add(Drawer("Desk", root, new Vector3(0.6f, U + 0.52f, 1.9f), new Vector3(0.75f, 0.18f, 0.7f), 90f, "Masa Çekmecesi", "masa çekmecesi", 0.4f));
            var chair = Node("Chair_Fallen", root, new Vector3(1.7f, U + 0.25f, 2.1f), new Vector3(0, 30, 85));
            BoxL("Seat", chair, Vector3.zero, new Vector3(0.45f, 0.05f, 0.45f), M.Wood, true);
            BoxL("Back", chair, new Vector3(0, 0.3f, -0.2f), new Vector3(0.45f, 0.6f, 0.05f), M.Wood);
            var deskLamp = Lamp("Lamp_Desk", root, new Vector3(0.45f, U + 1.15f, 2.4f), new Color(1f, 0.75f, 0.45f), 0.8f, 4f, true, 0.06f, false, false);
            Prim(PrimitiveType.Cylinder, "DeskLampPole", root, new Vector3(0.45f, U + 0.95f, 2.4f), new Vector3(0.03f, 0.18f, 0.03f), M.DarkMetal);
            Box("Desk_Papers", root, new Vector3(0.65f, U + 0.775f, 1.5f), new Vector3(0.35f, 0.01f, 0.28f), M.Paper, false);

            // ---- wardrobe (south wall, hinged doors) ----
            containers.Add(Wardrobe(root, new Vector3(6.4f, U, 0.5f)));

            // ---- dresser with three drawers (north wall) ----
            BoxMinMax("Dresser_Body", root, new Vector3(5.9f, U, 7.3f), new Vector3(7.7f, U + 1.05f, 7.85f), M.WoodDark);
            for (int i = 0; i < 3; i++)
            {
                containers.Add(Drawer("Dresser" + i, root, new Vector3(6.8f, U + 0.2f + i * 0.32f, 7.5f), new Vector3(1.6f, 0.26f, 0.5f), 180f,
                    "Çekmece", i == 0 ? "şifonyerin alt çekmecesi" : i == 1 ? "şifonyerin orta çekmecesi" : "şifonyerin üst çekmecesi", 0.35f, true));
            }
            Box("Dresser_Clock", root, new Vector3(6.4f, U + 1.2f, 7.55f), new Vector3(0.25f, 0.3f, 0.12f), M.DarkMetal, false);

            // ---- small cabinet (south wall) ----
            containers.Add(Cabinet(root, new Vector3(3.0f, U, 0.45f)));

            // ---- TV on a low cabinet (north wall) facing the bed ----
            BoxMinMax("TvCabinet", root, new Vector3(3.0f, U, 7.3f), new Vector3(5.0f, U + 0.55f, 7.85f), M.WoodDark);
            containers.Add(Drawer("TvDrawer", root, new Vector3(4.0f, U + 0.3f, 7.5f), new Vector3(1.6f, 0.22f, 0.5f), 180f, "TV Dolabı", "TV dolabı çekmecesi", 0.3f, true));

            // ---- boarded window on the west wall ----
            BoxMinMax("Window_Frame", root, new Vector3(0.15f, U + 1.1f, 2.9f), new Vector3(0.22f, U + 2.6f, 3.9f), M.WoodDark, false);
            for (int i = 0; i < 4; i++)
            {
                var plank = Box("Window_Board" + i, root, new Vector3(0.25f, U + 1.3f + i * 0.38f, 3.4f), new Vector3(0.04f, 0.18f, 1.3f), M.Wood, false);
                plank.transform.rotation = Quaternion.Euler(i * 7f - 10f, 0, 0);
            }

            // ---- exit door + keypad ----
            var door = SwingDoor("Door_Bedroom", root, new Vector3(8f, U, 2f), 1.6f, true, "kapı", "Kilitli. Tuş takımı 4 haneli bir şifre istiyor.", true, 2.4f);
            testDoors.Add(door);
            var keypadGo = Box("Keypad", root, new Vector3(7.82f, U + 1.35f, 3.2f), new Vector3(0.05f, 0.32f, 0.22f), M.DarkMetal);
            for (int r = 0; r < 4; r++)
            for (int c = 0; c < 3; c++)
                BoxL("Key", keypadGo.transform, new Vector3(-0.03f, -0.1f + r * 0.055f, -0.06f + c * 0.06f), new Vector3(0.01f, 0.04f, 0.045f), M.Metal);
            var status = BoxL("StatusLight", keypadGo.transform, new Vector3(-0.03f, 0.13f, 0f), new Vector3(0.01f, 0.03f, 0.12f), M.GlowRed);
            var keypad = Add<KeypadController>(keypadGo);
            keypad.Configure(door, status.GetComponent<Renderer>());
            Highlight(keypadGo);
            ctx.Keypad = keypad;

            // ---- decoration: blood, clothes, scratches ----
            Blood(root, new Vector3(7.2f, U + 0.013f, 2.3f), 0.9f, 10f);
            Blood(root, new Vector3(3.4f, U + 0.013f, 3.2f), 0.5f, 140f);
            Text("Wall_Writing", root, new Vector3(7.83f, U + 2.6f, 5.6f), new Vector3(0, 90, 0),
                "SAATE\nGÜVENME", 3.2f, new Color(0.45f, 0.02f, 0.02f), 2.4f, 1f);
            PictureFrame(root, new Vector3(4f, U + 2.4f, 0.17f), Vector3.zero, M.Photo);

            // ---- lights ----
            var ceiling = Lamp("Lamp_Ceiling", root, new Vector3(4f, U + 3.2f, 4f), new Color(0.75f, 0.8f, 1f), 0.45f, 8f, true, 0.06f, true);
            ctx.BedroomLights = new[] { ceiling, nightLamp, deskLamp };
            ctx.Tv = BuildTelevision(ctx, root, new Vector3(4f, U + 1.25f, 7.72f));

            // ---- the secret note can be in one of these ----
            var candidates = new List<SearchableContainer> { containers[0], containers[1], containers[2], containers[3] };
            var others = new List<SearchableContainer>();
            foreach (var c in containers) if (!candidates.Contains(c)) others.Add(c);
            ctx.SecretCandidates = candidates.ToArray();
            ctx.OtherContainers = others.ToArray();

            ctx.Math = Add<MathPuzzle>(root.gameObject);
            ctx.Math.Configure(ctx.SecretCandidates, ctx.OtherContainers, keypad);

            ctx.RobotStart = Point(ctx, "Robot_Start", new Vector3(6.6f, U, 4.6f), -100f);
        }

        private static Transform CamPoint(Ctx ctx, string name, Vector3 pos, Vector3 lookAt)
        {
            var t = Point(ctx, name, pos, 0f);
            t.rotation = Quaternion.LookRotation(lookAt - pos);
            return t;
        }

        /// <summary>Drawer unit: body + sliding drawer. yaw = direction the drawer faces (0 = +z, 90 = +x, 180 = -z).</summary>
        private static SearchableContainer Drawer(string name, Transform parent, Vector3 basePos, Vector3 size, float yaw,
            string display, string location, float slide = 0.35f, bool drawerOnly = false)
        {
            var root = Node(name, parent, Vector3.zero);
            root.position = basePos;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            // local axes: drawer front faces local +z
            float bodyHeight = size.y;
            if (!drawerOnly)
            {
                BoxL("Body", root, new Vector3(0, bodyHeight / 2, 0), new Vector3(size.x, bodyHeight, size.z), M.WoodDark, true);
            }
            var drawer = Node("Drawer", root, new Vector3(0, drawerOnly ? 0 : bodyHeight * 0.65f, drawerOnly ? 0.02f : 0.03f));
            float dh = drawerOnly ? size.y : bodyHeight * 0.3f;
            float dw = drawerOnly ? size.x * 0.96f : size.x * 0.85f;
            float dd = drawerOnly ? size.z * 0.9f : size.z * 0.9f;
            BoxL("Front", drawer, new Vector3(0, 0, dd / 2), new Vector3(dw, dh, 0.03f), M.Wood, true);
            BoxL("Bottom", drawer, new Vector3(0, -dh / 2 + 0.01f, 0), new Vector3(dw * 0.92f, 0.02f, dd), M.WoodDark);
            BoxL("Handle", drawer, new Vector3(0, 0, dd / 2 + 0.03f), new Vector3(0.14f, 0.025f, 0.025f), M.Gold);
            var note = BoxL("Note", drawer, new Vector3(0.02f, -dh / 2 + 0.03f, 0), new Vector3(0.2f, 0.005f, 0.14f), M.Paper);
            note.transform.localRotation = Quaternion.Euler(0, 17, 0);

            var c = drawer.gameObject.AddComponent<SearchableContainer>();
            c.Configure(display, location, ContainerKind.Drawer, drawer, new Vector3(0, 0, slide), 0f, note);
            drawer.gameObject.AddComponent<Highlightable>();
            return c;
        }

        private static SearchableContainer Wardrobe(Transform parent, Vector3 basePos)
        {
            var root = Node("Wardrobe", parent, Vector3.zero);
            root.position = basePos;
            // back against the south wall, opening toward +z
            BoxL("Back", root, new Vector3(0, 1.1f, -0.33f), new Vector3(1.8f, 2.2f, 0.04f), M.WoodDark, true);
            BoxL("Side_L", root, new Vector3(-0.88f, 1.1f, 0), new Vector3(0.04f, 2.2f, 0.7f), M.WoodDark, true);
            BoxL("Side_R", root, new Vector3(0.88f, 1.1f, 0), new Vector3(0.04f, 2.2f, 0.7f), M.WoodDark, true);
            BoxL("Top", root, new Vector3(0, 2.2f, 0), new Vector3(1.8f, 0.05f, 0.7f), M.WoodDark, true);
            BoxL("Floor", root, new Vector3(0, 0.05f, 0), new Vector3(1.8f, 0.1f, 0.7f), M.WoodDark, true);
            BoxL("Rail", root, new Vector3(0, 1.9f, 0), new Vector3(1.7f, 0.03f, 0.03f), M.DarkMetal);
            for (int i = 0; i < 3; i++) BoxL("Coat" + i, root, new Vector3(-0.5f + i * 0.45f, 1.45f, 0), new Vector3(0.4f, 0.85f, 0.12f), M.Shirt);
            var note = BoxL("Note", root, new Vector3(0.2f, 0.11f, 0.1f), new Vector3(0.21f, 0.005f, 0.15f), M.Paper);

            var hinge = Node("DoorHinge", root, new Vector3(-0.86f, 0, 0.36f));
            BoxL("Door", hinge, new Vector3(0.86f, 1.1f, 0), new Vector3(1.7f, 2.15f, 0.04f), M.Wood, true);
            BoxL("Handle", hinge, new Vector3(1.55f, 1.1f, 0.04f), new Vector3(0.03f, 0.2f, 0.03f), M.Gold);

            var c = hinge.gameObject.AddComponent<SearchableContainer>();
            c.Configure("Gardırop", "gardırop", ContainerKind.HingedDoor, hinge, Vector3.zero, -105f, note);
            hinge.gameObject.AddComponent<Highlightable>();
            return c;
        }

        private static SearchableContainer Cabinet(Transform parent, Vector3 basePos)
        {
            var root = Node("Cabinet", parent, Vector3.zero);
            root.position = basePos;
            BoxL("Body", root, new Vector3(0, 0.45f, -0.02f), new Vector3(0.9f, 0.9f, 0.45f), M.WoodDark, true);
            var hinge = Node("DoorHinge", root, new Vector3(-0.44f, 0, 0.22f));
            BoxL("Door", hinge, new Vector3(0.44f, 0.45f, 0), new Vector3(0.86f, 0.84f, 0.03f), M.Wood, true);
            BoxL("Knob", hinge, new Vector3(0.78f, 0.45f, 0.03f), new Vector3(0.04f, 0.04f, 0.04f), M.Gold);
            var note = BoxL("Note", root, new Vector3(0, 0.12f, 0), new Vector3(0.2f, 0.005f, 0.15f), M.Paper);
            var c = hinge.gameObject.AddComponent<SearchableContainer>();
            c.Configure("Dolap", "küçük dolap", ContainerKind.HingedDoor, hinge, Vector3.zero, -100f, note);
            hinge.gameObject.AddComponent<Highlightable>();
            return c;
        }

        private static Intro.TelevisionController BuildTelevision(Ctx ctx, Transform parent, Vector3 center)
        {
            var tv = new GameObject("Television").transform;
            tv.SetParent(parent, false);
            tv.position = center;
            BoxL("Body", tv, Vector3.zero, new Vector3(1.66f, 0.98f, 0.1f), M.TvBody, true);
            BoxL("Stand", tv, new Vector3(0, -0.56f, 0), new Vector3(0.4f, 0.14f, 0.25f), M.TvBody);
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            screen.transform.SetParent(tv, false);
            screen.transform.localPosition = new Vector3(0, 0.01f, -0.052f);
            screen.transform.localScale = new Vector3(1.56f, 0.88f, 1f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            screen.GetComponent<Renderer>().sharedMaterial = M.Screen;

            var light = PointLight("ScreenLight", tv, center + new Vector3(0, 0, -0.6f), new Color(0.7f, 0.75f, 0.9f), 1.2f, 7f, true);
            light.enabled = false;

            var placeholder = Text("Placeholder", tv, center + new Vector3(0, 0.02f, -0.06f), Vector3.zero,
                "<color=#ff3333><b>VİDEO BULUNAMADI</b></color>\n“oyuna hoş geldin. Bu evden kaçmaya on dakikan var. başarılar dilerim”\n<size=55%>IntroductionVideo.mp4 dosyasını Assets/Media içine koy ve 5 MINUTES > Build Game Scene çalıştır</size>",
                0.9f, Color.white, 1.45f, 0.8f).gameObject;

            var vp = tv.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.playOnAwake = false;
            var audio = tv.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0.4f;
            audio.volume = 1f;

            var controller = tv.gameObject.AddComponent<Intro.TelevisionController>();
            controller.Configure(introClip, vp, audio, screen.GetComponent<Renderer>(), light, placeholder, ctx.BedroomLights);
            return controller;
        }

        // =====================================================================
        // Easter egg room A (upstairs, x 8..14, z 4..10)
        // =====================================================================

        private static void BuildEasterRoomA(Ctx ctx)
        {
            var root = new GameObject("EasterEgg_RoomA_Nursery").transform;
            root.SetParent(ctx.House, false);
            Lamp("Candle", root, new Vector3(11f, U + 0.95f, 8.9f), new Color(1f, 0.55f, 0.25f), 0.8f, 4.5f, true, 0.5f, false, false);
            BoxMinMax("Table", root, new Vector3(9.8f, U + 0.72f, 8.4f), new Vector3(12.2f, U + 0.78f, 9.6f), M.WoodDark);
            BoxMinMax("Table_Leg", root, new Vector3(10.9f, U, 8.9f), new Vector3(11.1f, U + 0.72f, 9.1f), M.WoodDark);

            var photo = Box("Photograph", root, new Vector3(10.4f, U + 0.79f, 9.0f), new Vector3(0.3f, 0.01f, 0.24f), M.WoodDark);
            ImageQuad("Image", photo.transform, photo.transform.position + Vector3.up * 0.006f, new Vector3(90, 0, 0), new Vector2(0.28f, 0.22f), M.Photo);
            photo.transform.rotation = Quaternion.Euler(0, -12, 0);
            Add<ReadableObject>(photo).Configure("Fotoğrafa Bak", "Eski bir fotoğraf",
                "Dört kişilik bir aile bu evin önünde duruyor. Anne ile babanın yüzleri kazınmış.\n\n" +
                "Arkasında, solmuş mürekkeple:\n\"Yaz 2009 — O'nun yan eve taşındığı yıl.\"", false);

            var paper = Box("Newspaper", root, new Vector3(11.7f, U + 0.79f, 9.0f), new Vector3(0.5f, 0.01f, 0.36f), M.Paper);
            ImageQuad("Image", paper.transform, paper.transform.position + Vector3.up * 0.006f, new Vector3(90, 0, 0), new Vector2(0.48f, 0.34f), M.Newspaper);
            paper.transform.rotation = Quaternion.Euler(0, 8, 0);
            Add<ReadableObject>(paper).Configure("Gazeteyi Oku", "GÜNLÜK HABER — 14 MART 2011",
                "DÖRT KİŞİLİK AİLE İZ BIRAKMADAN KAYBOLDU\n\nPolis evi içeriden kilitli buldu. " +
                "Evdeki bütün saatler tam olarak saati beş geçe durmuştu.\n\n" +
                "Komşular, maskeli bir adamın geceleri eve 'garip makineler' taşıdığını gördüklerini söylüyor.", false);

            var diary = Box("Diary", root, new Vector3(9.0f, U + 0.42f, 5.0f), new Vector3(0.22f, 0.04f, 0.3f), M.Blanket);
            Box("CribSide", root, new Vector3(9.2f, U + 0.35f, 5.5f), new Vector3(1.0f, 0.7f, 0.05f), M.Wood, true);
            Box("Crib", root, new Vector3(9.2f, U + 0.2f, 5.0f), new Vector3(1.0f, 0.4f, 0.9f), M.Wood, true);
            Add<ReadableObject>(diary).Configure("Günlüğü Oku", "Bir çocuğun günlüğü",
                "Aşağıdaki zemini bir oyuna çevirdi.\n\"Ortanın BİR karo solundan başla,\" diye güldü,\n\"sonra yılanı sağa doğru takip et.\"\n\n" +
                "Küçük robot bana karşı çok iyi. Ondan YARDIM istediğinde seni dinliyor.", true);

            PictureFrame(root, new Vector3(13.83f, U + 1.9f, 7f), new Vector3(0, -90, 0), M.Photo);
            Blood(root, new Vector3(12.5f, U + 0.012f, 5.6f), 0.7f, 30f);
            Text("Wall_Count", root, new Vector3(11f, U + 2.3f, 9.83f), new Vector3(0, 0, 0),
                "1 2 3 4 5\n5 5 5 5 5", 2.6f, new Color(0.35f, 0.02f, 0.02f), 3f, 1.2f);

            Add<RobotLineZone>(TriggerBox("Zone_RoomA", root, new Vector3(8.3f, U, 4.3f), new Vector3(13.7f, U + 2.5f, 9.7f)))
                .Configure("Bu insanlar... bu evden hiç çıkmadılar. Her şeyi oku — bize yardımı dokunabilir.", true, 1);
        }

        // =====================================================================
        // Stairs (x 24..28, from z 4 at y 4 down to z 14 at y 0)
        // =====================================================================

        private static void BuildStairs(Ctx ctx)
        {
            var root = new GameObject("Stairs").transform;
            root.SetParent(ctx.House, false);
            const int steps = 20;
            for (int i = 0; i < steps; i++)
            {
                float z0 = 4f + i * 0.5f;
                float top = U - i * 0.2f - 0.1f;
                BoxMinMax("Step" + i, root, new Vector3(24.15f, top - 0.2f, z0), new Vector3(27.85f, top, z0 + 0.5f), M.WoodDark, false);
                BoxMinMax("StepBody" + i, root, new Vector3(24.15f, Mathf.Max(0, top - 0.6f), z0), new Vector3(27.85f, top - 0.2f, z0 + 0.5f), M.Wood, false);
            }
            // walkable ramp collider (invisible)
            float len = Mathf.Sqrt(10f * 10f + 4f * 4f);
            var ramp = new GameObject("Stairs_Ramp");
            ramp.transform.SetParent(root, false);
            float angle = Mathf.Atan2(4f, 10f) * Mathf.Rad2Deg;
            ramp.transform.rotation = Quaternion.Euler(angle, 0, 0);
            Vector3 normal = ramp.transform.up;
            ramp.transform.position = new Vector3(26f, 2f, 9f) - normal * 0.1f;
            ramp.AddComponent<BoxCollider>().size = new Vector3(3.7f, 0.2f, len + 0.2f);
            ramp.AddComponent<FootstepSurface>().Surface = SurfaceType.Wood;

            // railing
            for (int i = 0; i <= 5; i++)
            {
                float z = 4.2f + i * 1.9f;
                float y = U - (z - 4f) * 0.4f;
                BoxMinMax("Rail_Post" + i, root, new Vector3(27.6f, y, z), new Vector3(27.7f, y + 0.9f, z + 0.1f), M.WoodDark, false);
            }
            var rail = Box("Rail", root, new Vector3(27.65f, 2.9f, 9f), new Vector3(0.08f, 0.08f, len), M.WoodDark, false);
            rail.transform.rotation = Quaternion.Euler(angle, 0, 0);
            Lamp("Lamp_Stairs", root, new Vector3(26f, 6.4f, 9f), new Color(0.8f, 0.85f, 1f), 0.6f, 7f, true, 0.15f);
            Add<RobotLineZone>(TriggerBox("Zone_Stairs", root, new Vector3(24.2f, U, 0.3f), new Vector3(27.8f, U + 2.5f, 3.7f)))
                .Configure("Merdivenler! Hadi aşağı inelim.", true, 1);
        }
    }
}
