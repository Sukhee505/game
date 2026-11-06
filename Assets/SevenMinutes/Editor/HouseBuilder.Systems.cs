using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SevenMinutes.Audio;
using SevenMinutes.Core;
using SevenMinutes.Game;
using SevenMinutes.Intro;
using SevenMinutes.NPC;
using SevenMinutes.Testing;
using SevenMinutes.Player;
using SevenMinutes.Robot;
using SevenMinutes.UI;

namespace SevenMinutes.EditorTools
{
    public static partial class HouseBuilder
    {
        // =====================================================================
        // Lighting, sky, fog, post-processing
        // =====================================================================

        private static void BuildLighting(Ctx ctx)
        {
            var moonGo = new GameObject("Sun");
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(1f, 0.95f, 0.85f);
            moon.intensity = 1.3f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 1f;
            moonGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            ctx.Moon = moon;

            if (M.Sky != null) RenderSettings.skybox = M.Sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.34f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.18f, 0.19f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.72f);
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.sun = moon;

            // global post-processing volume
            var volumeGo = new GameObject("PostProcessing");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, GenFolder + "/HorrorPostProcessing.asset");

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.7f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.45f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.2f);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.65f);
            color.contrast.Override(14f);
            color.saturation.Override(-18f);
            color.colorFilter.Override(new Color(0.92f, 0.95f, 1f));
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(0.12f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            volume.sharedProfile = profile;
        }

        // =====================================================================
        // Player, robot, camera, managers, UI
        // =====================================================================

        private static void BuildCharactersAndSystems(Ctx ctx)
        {
            // ---------------- camera ----------------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var playerCam = camGo.AddComponent<PlayerCamera>();
            camGo.transform.SetPositionAndRotation(ctx.CamMenu.position, ctx.CamMenu.rotation);

            // ---------------- player ----------------
            var player = BuildPlayer(ctx.StandingPose.position, ctx.StandingPose.rotation, out var rig);
            var controller = player.GetComponent<PlayerController>();
            controller.Configure(playerCam, rig);
            playerCam.Configure(controller);
            var interaction = player.AddComponent<PlayerInteraction>();
            interaction.Configure(controller);

            // ---------------- robot ----------------
            var robot = BuildRobot(ctx.RobotStart.position, ctx.RobotStart.rotation, out var robotRig);
            var robotController = robot.GetComponent<RobotController>();
            robotController.Configure(controller, robotRig);

            // ---------------- systems ----------------
            var sys = new GameObject("GameSystems");
            sys.AddComponent<RuntimeNavMeshBaker>();
            var audio = sys.AddComponent<AudioManager>();
            _ = audio;
            var gm = sys.AddComponent<GameManager>();
            var timer = sys.AddComponent<GameTimer>();
            var checkpoints = sys.AddComponent<CheckpointManager>();
            checkpoints.Configure(ctx.Checkpoints);
            var victory = sys.AddComponent<VictoryManager>();
            victory.Configure(ctx.RobotGardenPoint, ctx.Moon);
            var defeat = sys.AddComponent<DefeatManager>();
            var intro = sys.AddComponent<IntroSequence>();
            intro.Configure(controller, robotController, ctx.Tv, ctx.LyingPose, ctx.SittingPose, ctx.StandingPose, ctx.CamFace, ctx.CamRoom, ctx.CamMenu);
            gm.Configure(controller, robotController, timer, checkpoints, intro, victory, defeat);
            sys.AddComponent<TensionDirector>().Configure(timer);
            sys.AddComponent<LevelDirector>().Configure(robotController, controller, ctx.LavaRobotWait, ctx.FinalRobotWait);
            sys.AddComponent<RobotDialogue>();
            var hints = sys.AddComponent<RobotHintSystem>();
            hints.Configure(robotController, ctx.Math, ctx.Tiles, ctx.Lava, ctx.FinalDoor);
            robot.GetComponent<RobotInteraction>().Configure(robotController, hints, ctx.FinalDoor);

            // ---------------- UI ----------------
            var ui = new GameObject("UI");
            ui.AddComponent<ScreenEffects>();
            ui.AddComponent<GameHUD>().Configure(interaction);
            ui.AddComponent<MainMenuUI>();
            ui.AddComponent<PauseMenuUI>();
            ui.AddComponent<EndScreensUI>().Configure(victory, defeat);
            ui.AddComponent<NoteUI>();
            ui.AddComponent<KeypadUI>();
            ui.AddComponent<ComputerTerminalUI>();
            var dialogueUI = ui.AddComponent<DialogueUI>();

            // ---------------- UNIT-6: local LLM guard NPC ----------------
            var handler = ctx.Guard.AddComponent<GuardActionHandler>();
            handler.Configure(ctx.FinalDoor, ctx.Checkpoints[4]);
            var guardNpc = ctx.Guard.AddComponent<GuardNpc>();
            guardNpc.Configure(dialogueUI, handler, ctx.FinalDoor, ctx.GuardEyes, ctx.GuardLight);

            // ---------------- automated tests (F5 robot, F6 LLM) ----------------
            var testRoot = new GameObject("TestStartPoints").transform;
            var starts = new[]
            {
                new Vector3(3f, 4f, 3f), new Vector3(14f, 4f, 2f), new Vector3(11f, 4f, 7f), new Vector3(26f, 4f, 2f), new Vector3(20f, 0f, 16f),
                new Vector3(12f, 0f, 17f), new Vector3(24f, 0f, 19.5f), new Vector3(27f, 0f, 38f), new Vector3(21f, 0f, 42f), new Vector3(30f, 0f, 44f)
            };
            var startNames = new[] { "Yatak_odasi", "Ust_koridor", "Gizli_oda_A", "Merdiven_ustu", "Alt_koridor", "Gizli_oda_B", "Karo_odasi_giris", "Karo_odasi_cikis", "Lav_platformu_bati", "Lav_platformu_dogu" };
            var startPoints = new Transform[starts.Length];
            for (int i = 0; i < starts.Length; i++)
            {
                var t = new GameObject($"Start_{i + 1:00}_{startNames[i]}").transform;
                t.SetParent(testRoot, false);
                t.position = starts[i];
                startPoints[i] = t;
            }
            sys.AddComponent<CompanionTestRunner>().Configure(robotController, ctx.LavaButtonStand, startPoints, testDoors.ToArray());
            sys.AddComponent<NpcDialogueTestRunner>().Configure(guardNpc);

            // UnityWebRequest must be allowed to call http://localhost:11434 (Ollama)
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        }

        // ---------------- human character ----------------

        private static GameObject BuildPlayer(Vector3 pos, Quaternion rot, out HumanoidRig rig)
        {
            var root = new GameObject("Player");
            root.layer = 2; // Ignore Raycast: camera & interaction rays pass through the player
            root.transform.SetPositionAndRotation(pos, rot);
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.04f;
            var pc = root.AddComponent<PlayerController>();
            _ = pc;

            var model = Node("Model", root.transform, Vector3.zero);
            var pelvis = Node("Pelvis", model, new Vector3(0, 0.95f, 0));
            BoxL("Hips", pelvis, new Vector3(0, 0.02f, 0), new Vector3(0.34f, 0.2f, 0.21f), M.Pants);
            BoxL("Belt", pelvis, new Vector3(0, 0.11f, 0), new Vector3(0.35f, 0.04f, 0.22f), M.Shoes);

            var spine = Node("Spine", pelvis, new Vector3(0, 0.1f, 0));
            Prim(PrimitiveType.Capsule, "Torso", spine, new Vector3(0, 0.25f, 0), new Vector3(0.38f, 0.29f, 0.24f), M.Shirt);
            BoxL("Collar", spine, new Vector3(0, 0.47f, 0.02f), new Vector3(0.22f, 0.05f, 0.16f), M.Shirt);
            Prim(PrimitiveType.Cylinder, "Neck", spine, new Vector3(0, 0.52f, 0), new Vector3(0.09f, 0.05f, 0.09f), M.Skin);

            var head = Node("Head", spine, new Vector3(0, 0.56f, 0));
            Prim(PrimitiveType.Sphere, "Skull", head, new Vector3(0, 0.12f, 0), new Vector3(0.21f, 0.25f, 0.23f), M.Skin);
            Prim(PrimitiveType.Sphere, "Hair", head, new Vector3(0, 0.19f, -0.015f), new Vector3(0.225f, 0.17f, 0.24f), M.Hair);
            BoxL("Nose", head, new Vector3(0, 0.1f, 0.115f), new Vector3(0.03f, 0.05f, 0.03f), M.Skin);
            Prim(PrimitiveType.Sphere, "EarL", head, new Vector3(-0.105f, 0.11f, 0), new Vector3(0.03f, 0.06f, 0.04f), M.Skin);
            Prim(PrimitiveType.Sphere, "EarR", head, new Vector3(0.105f, 0.11f, 0), new Vector3(0.03f, 0.06f, 0.04f), M.Skin);
            Transform lidL = null, lidR = null;
            foreach (float side in new[] { -1f, 1f })
            {
                var eyePos = new Vector3(side * 0.045f, 0.135f, 0.095f);
                Prim(PrimitiveType.Sphere, "Eye", head, eyePos, Vector3.one * 0.036f, M.EyeWhite);
                Prim(PrimitiveType.Sphere, "Pupil", head, eyePos + new Vector3(0, 0, 0.015f), Vector3.one * 0.016f, M.Pupil);
                Prim(PrimitiveType.Sphere, "Brow", head, eyePos + new Vector3(0, 0.035f, 0.008f), new Vector3(0.045f, 0.012f, 0.02f), M.Hair);
                var lid = Node(side < 0 ? "EyelidL" : "EyelidR", head, eyePos + new Vector3(0, 0.025f, 0.008f));
                Prim(PrimitiveType.Sphere, "Lid", lid, new Vector3(0, -0.5f, 0), Vector3.one, M.Skin);
                lid.localScale = new Vector3(0.042f, 0.008f, 0.03f);
                if (side < 0) lidL = lid; else lidR = lid;
            }

            Transform Arm(float side, out Transform elbow)
            {
                var shoulder = Node(side < 0 ? "ShoulderL" : "ShoulderR", spine, new Vector3(side * 0.235f, 0.43f, 0));
                Prim(PrimitiveType.Sphere, "Deltoid", shoulder, Vector3.zero, Vector3.one * 0.11f, M.Shirt);
                Prim(PrimitiveType.Capsule, "UpperArm", shoulder, new Vector3(0, -0.15f, 0), new Vector3(0.1f, 0.15f, 0.1f), M.Shirt);
                elbow = Node("Elbow", shoulder, new Vector3(0, -0.3f, 0));
                Prim(PrimitiveType.Capsule, "Forearm", elbow, new Vector3(0, -0.13f, 0), new Vector3(0.085f, 0.13f, 0.085f), M.Skin);
                Prim(PrimitiveType.Sphere, "Hand", elbow, new Vector3(0, -0.29f, 0.01f), new Vector3(0.08f, 0.1f, 0.05f), M.Skin);
                return shoulder;
            }
            var shL = Arm(-1f, out var elL);
            var shR = Arm(1f, out var elR);

            Transform Leg(float side, out Transform knee)
            {
                var hip = Node(side < 0 ? "HipL" : "HipR", pelvis, new Vector3(side * 0.1f, -0.03f, 0));
                Prim(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0, -0.22f, 0), new Vector3(0.15f, 0.23f, 0.15f), M.Pants);
                knee = Node("Knee", hip, new Vector3(0, -0.45f, 0));
                Prim(PrimitiveType.Capsule, "Shin", knee, new Vector3(0, -0.2f, 0), new Vector3(0.12f, 0.21f, 0.12f), M.Pants);
                BoxL("Shoe", knee, new Vector3(0, -0.44f, 0.05f), new Vector3(0.11f, 0.08f, 0.26f), M.Shoes);
                return hip;
            }
            var hL = Leg(-1f, out var kL);
            var hR = Leg(1f, out var kR);

            rig = root.AddComponent<HumanoidRig>();
            rig.Configure(root.GetComponent<PlayerController>(), model, pelvis, spine, head, shL, elL, shR, elR, hL, kL, hR, kR, lidL, lidR,
                new Vector3(0.042f, 0.05f, 0.03f), new Vector3(0.042f, 0.008f, 0.03f));
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
            return root;
        }

        // ---------------- robot companion ----------------

        private static GameObject BuildRobot(Vector3 pos, Quaternion rot, out RobotRig rig)
        {
            var root = new GameObject("UNIT-7 Robot");
            root.transform.SetPositionAndRotation(pos, rot);
            root.AddComponent<NavMeshIgnore>();
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 0.9f;
            agent.speed = 3.8f;
            agent.acceleration = 14f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.15f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.enabled = false; // enabled after the runtime NavMesh bake
            var trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 0.5f, 0);
            trigger.height = 1.05f;
            trigger.radius = 0.38f;

            var body = Node("Body", root.transform, new Vector3(0, 0.4f, 0));
            BoxL("Pelvis", body, new Vector3(0, 0, 0), new Vector3(0.26f, 0.1f, 0.18f), M.RobotJoint);
            BoxL("Torso", body, new Vector3(0, 0.2f, 0), new Vector3(0.34f, 0.3f, 0.24f), M.RobotBody);
            BoxL("ChestPlate", body, new Vector3(0, 0.21f, 0.123f), new Vector3(0.26f, 0.2f, 0.02f), M.RobotJoint);
            var chestLight = BoxL("ChestLight", body, new Vector3(0, 0.27f, 0.135f), new Vector3(0.1f, 0.035f, 0.01f), M.RobotGlow);
            BoxL("Vent1", body, new Vector3(0, 0.17f, 0.135f), new Vector3(0.18f, 0.012f, 0.01f), M.RobotBody);
            BoxL("Vent2", body, new Vector3(0, 0.14f, 0.135f), new Vector3(0.18f, 0.012f, 0.01f), M.RobotBody);
            BoxL("Battery", body, new Vector3(0, 0.2f, -0.15f), new Vector3(0.22f, 0.22f, 0.08f), M.RobotJoint);
            var batteryLight = BoxL("BatteryLight", body, new Vector3(0.07f, 0.28f, -0.192f), new Vector3(0.03f, 0.03f, 0.01f), M.RobotGlow);
            Prim(PrimitiveType.Cylinder, "Neck", body, new Vector3(0, 0.37f, 0), new Vector3(0.07f, 0.03f, 0.07f), M.RobotJoint);

            var head = Node("Head", body, new Vector3(0, 0.39f, 0));
            BoxL("Skull", head, new Vector3(0, 0.1f, 0), new Vector3(0.3f, 0.19f, 0.24f), M.RobotBody);
            BoxL("Brow", head, new Vector3(0, 0.185f, 0.02f), new Vector3(0.31f, 0.03f, 0.22f), M.RobotJoint);
            BoxL("Face", head, new Vector3(0, 0.1f, 0.123f), new Vector3(0.26f, 0.13f, 0.02f), M.Pupil);
            var eyeL = Prim(PrimitiveType.Cylinder, "EyeL", head, new Vector3(-0.065f, 0.11f, 0.135f), new Vector3(0.065f, 0.008f, 0.065f), M.RobotGlow,
                false, new Vector3(90, 0, 0));
            var eyeR = Prim(PrimitiveType.Cylinder, "EyeR", head, new Vector3(0.065f, 0.11f, 0.135f), new Vector3(0.065f, 0.008f, 0.065f), M.RobotGlow,
                false, new Vector3(90, 0, 0));
            Prim(PrimitiveType.Cylinder, "EarL", head, new Vector3(-0.16f, 0.1f, 0), new Vector3(0.08f, 0.015f, 0.08f), M.RobotJoint, false, new Vector3(0, 0, 90));
            Prim(PrimitiveType.Cylinder, "EarR", head, new Vector3(0.16f, 0.1f, 0), new Vector3(0.08f, 0.015f, 0.08f), M.RobotJoint, false, new Vector3(0, 0, 90));
            Prim(PrimitiveType.Cylinder, "Antenna", head, new Vector3(0.1f, 0.22f, -0.05f), new Vector3(0.012f, 0.03f, 0.012f), M.RobotJoint);
            var tip = Prim(PrimitiveType.Sphere, "AntennaTip", head, new Vector3(0.1f, 0.26f, -0.05f), Vector3.one * 0.03f, M.RobotGlow);
            var eyeLight = PointLight("EyeLight", head, Vector3.zero, new Color(0.3f, 0.65f, 1f), 0f, 3.5f);
            eyeLight.transform.localPosition = new Vector3(0, 0.11f, 0.3f);

            Transform Arm(float side, out Transform elbow)
            {
                var shoulder = Node(side < 0 ? "ShoulderL" : "ShoulderR", body, new Vector3(side * 0.215f, 0.31f, 0));
                Prim(PrimitiveType.Sphere, "Joint", shoulder, Vector3.zero, Vector3.one * 0.085f, M.RobotJoint);
                BoxL("UpperArm", shoulder, new Vector3(0, -0.1f, 0), new Vector3(0.065f, 0.16f, 0.07f), M.RobotBody);
                elbow = Node("Elbow", shoulder, new Vector3(0, -0.19f, 0));
                Prim(PrimitiveType.Sphere, "Joint", elbow, Vector3.zero, Vector3.one * 0.065f, M.RobotJoint);
                BoxL("Forearm", elbow, new Vector3(0, -0.085f, 0), new Vector3(0.06f, 0.14f, 0.065f), M.RobotBody);
                BoxL("Hand", elbow, new Vector3(0, -0.18f, 0.01f), new Vector3(0.075f, 0.05f, 0.06f), M.RobotJoint);
                BoxL("Finger1", elbow, new Vector3(-0.022f, -0.225f, 0.02f), new Vector3(0.018f, 0.045f, 0.018f), M.RobotBody);
                BoxL("Finger2", elbow, new Vector3(0.022f, -0.225f, 0.02f), new Vector3(0.018f, 0.045f, 0.018f), M.RobotBody);
                return shoulder;
            }
            var sL = Arm(-1f, out var eL);
            var sR = Arm(1f, out var eR);

            Transform Leg(float side, out Transform knee)
            {
                var hip = Node(side < 0 ? "HipL" : "HipR", root.transform, new Vector3(side * 0.085f, 0.36f, 0));
                Prim(PrimitiveType.Sphere, "Joint", hip, Vector3.zero, Vector3.one * 0.08f, M.RobotJoint);
                BoxL("Thigh", hip, new Vector3(0, -0.09f, 0), new Vector3(0.075f, 0.16f, 0.085f), M.RobotBody);
                knee = Node("Knee", hip, new Vector3(0, -0.18f, 0));
                Prim(PrimitiveType.Sphere, "Joint", knee, Vector3.zero, Vector3.one * 0.07f, M.RobotJoint);
                BoxL("Shin", knee, new Vector3(0, -0.08f, 0), new Vector3(0.07f, 0.14f, 0.08f), M.RobotBody);
                BoxL("Foot", knee, new Vector3(0, -0.16f, 0.03f), new Vector3(0.11f, 0.04f, 0.17f), M.RobotJoint);
                return hip;
            }
            var hL = Leg(-1f, out var kL);
            var hR = Leg(1f, out var kR);

            rig = root.AddComponent<RobotRig>();
            rig.Configure(agent, body, head, hL, kL, hR, kR, sL, eL, sR, eR,
                new[] { eyeL.GetComponent<Renderer>(), eyeR.GetComponent<Renderer>(), chestLight.GetComponent<Renderer>(),
                    batteryLight.GetComponent<Renderer>(), tip.GetComponent<Renderer>() }, eyeLight);
            root.AddComponent<RobotController>();
            root.AddComponent<RobotInteraction>();
            return root;
        }
    }
}
