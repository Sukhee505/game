using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using SevenMinutes.Core;
using SevenMinutes.Environment;

namespace SevenMinutes.EditorTools
{
    /// <summary>
    /// Menu "7 MINUTES ▸ Build Game Scene": generates the complete game (house, puzzles, traps, characters, UI, audio,
    /// lighting) into Assets/SevenMinutes/Scenes/SevenMinutes_TheHouse.unity and makes it the first scene of the build.
    /// Everything is generated from code, so the scene can be rebuilt at any time.
    /// </summary>
    public static partial class HouseBuilder
    {
        public const string RootFolder = "Assets/SevenMinutes";
        private const string GenFolder = RootFolder + "/Generated";
        private const string ScenePath = RootFolder + "/Scenes/SevenMinutes_TheHouse.unity";
        private const float T = 0.3f;     // wall thickness
        private const float UvMeters = 2f; // texture repeat size

        private static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        private static int shadowBudget = 3;
        private static readonly List<DoorController> testDoors = new List<DoorController>();

        /// <summary>Shared references between the build steps.</summary>
        private class Ctx
        {
            public Transform House, Garden, Points;
            public Transform[] Checkpoints = new Transform[5];
            public Light Moon;

            // Level 1
            public Puzzles.SearchableContainer[] SecretCandidates;
            public Puzzles.SearchableContainer[] OtherContainers;
            public Puzzles.KeypadController Keypad;
            public Puzzles.MathPuzzle Math;
            public Transform LyingPose, SittingPose, StandingPose, CamFace, CamRoom, CamMenu, RobotStart;
            public Intro.TelevisionController Tv;
            public FlickerLight[] BedroomLights;

            // Level 2..5
            public Puzzles.TileMemoryPuzzle Tiles;
            public Puzzles.LavaBridgePuzzle Lava;
            public Puzzles.FinalDoorPuzzle FinalDoor;
            public Puzzles.ReadableObject WorkstationClue;
            public Transform LavaRobotWait, FinalRobotWait, RobotGardenPoint;
            public GameObject Guard;
            public Renderer[] GuardEyes;
            public Light GuardLight;
            public Transform LavaButtonStand;
            public Transform[] RobotTestStarts;
            public Environment.DoorController[] TestDoors;
        }

        [MenuItem("5 MINUTES/Build Game Scene", priority = 0)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("7 MINUTES", "Generating textures and materials...", 0.1f);
                PrepareFolders();
                MeshCache.Clear();
                shadowBudget = 3;
                testDoors.Clear();
                BuildAssets();

                EditorUtility.DisplayProgressBar("7 MINUTES", "Building the house...", 0.35f);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var ctx = new Ctx
                {
                    House = new GameObject("House").transform,
                    Garden = new GameObject("Garden").transform,
                    Points = new GameObject("Points").transform
                };

                BuildLighting(ctx);
                BuildUpperFloor(ctx);
                BuildBedroom(ctx);
                BuildEasterRoomA(ctx);
                BuildStairs(ctx);
                BuildGroundHall(ctx);
                BuildEasterRoomB(ctx);
                EditorUtility.DisplayProgressBar("7 MINUTES", "Building the challenges...", 0.55f);
                BuildTileRoom(ctx);
                BuildLavaRoom(ctx);
                BuildTrapCorridor(ctx);
                BuildFinalHall(ctx);
                BuildGarden(ctx);

                EditorUtility.DisplayProgressBar("7 MINUTES", "Creating characters and systems...", 0.8f);
                BuildCharactersAndSystems(ctx);

                if (!AssetDatabase.IsValidFolder(RootFolder + "/Scenes")) AssetDatabase.CreateFolder(RootFolder, "Scenes");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                SetBuildScenes();
                AssetDatabase.SaveAssets();

                Debug.Log("[7 MINUTES] Scene generated: " + ScenePath);
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("5 DAKİKA: EV",
                    "The game scene was generated:\n" + ScenePath + "\n\n" +
                    (Ctx_VideoFound ? "Intro video found and connected to the TV." :
                        "NOTE: Assets/Media/IntroductionVideo.mp4 was not found — the TV will show a placeholder.\nCopy your video there and run this menu again.") +
                    "\n\nPress Play to start.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool Ctx_VideoFound;

        private static void SetBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) list.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = list.ToArray();
            PlayerSettings.productName = "5 DAKIKA - EV";
        }

        private static void PrepareFolders()
        {
            if (AssetDatabase.IsValidFolder(GenFolder)) AssetDatabase.DeleteAsset(GenFolder);
            AssetDatabase.CreateFolder(RootFolder, "Generated");
            AssetDatabase.CreateFolder(GenFolder, "Textures");
            AssetDatabase.CreateFolder(GenFolder, "Materials");
        }

        // =====================================================================
        // Geometry helpers
        // =====================================================================

        private static Mesh BoxMesh(Vector3 size)
        {
            string key = $"{size.x:0.###}_{size.y:0.###}_{size.z:0.###}";
            if (MeshCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var h = size * 0.5f;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void Face(Vector3 n, Vector3 u, Vector3 v, float du, float dv)
            {
                int i = verts.Count;
                Vector3 c = Vector3.Scale(n, h);
                Vector3 hu = u * (Mathf.Abs(Vector3.Dot(u, h)));
                Vector3 hv = v * (Mathf.Abs(Vector3.Dot(v, h)));
                verts.Add(c - hu - hv); verts.Add(c + hu - hv); verts.Add(c + hu + hv); verts.Add(c - hu + hv);
                for (int k = 0; k < 4; k++) norms.Add(n);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(du / UvMeters, 0));
                uvs.Add(new Vector2(du / UvMeters, dv / UvMeters)); uvs.Add(new Vector2(0, dv / UvMeters));
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
            }

            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z);

            var mesh = new Mesh { name = "Box_" + key };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            MeshCache[key] = mesh;
            return mesh;
        }

        /// <summary>World-space box (centre + size) with a box collider and world-scaled UVs.</summary>
        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>Box in the parent's local space (props, characters).</summary>
        private static GameObject BoxL(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider = false,
            Vector3? euler = null)
        {
            var go = Box(name, parent, Vector3.zero, size, mat, collider);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go;
        }

        private static GameObject BoxMinMax(string name, Transform parent, Vector3 min, Vector3 max, Material mat, bool collider = true)
        {
            return Box(name, parent, (min + max) * 0.5f, max - min, mat, collider);
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
            bool keepCollider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Quad with 0..1 UVs for images (photos, screens, symbols). Visible from its -Z side (rotate as needed).</summary>
        private static GameObject ImageQuad(string name, Transform parent, Vector3 worldPos, Vector3 worldEuler, Vector2 size, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(parent, true);
            q.transform.position = worldPos;
            q.transform.rotation = Quaternion.Euler(worldEuler);
            var ps = parent != null ? parent.lossyScale : Vector3.one;
            q.transform.localScale = new Vector3(size.x / Mathf.Max(0.0001f, ps.x), size.y / Mathf.Max(0.0001f, ps.y), 1f);
            q.GetComponent<Renderer>().sharedMaterial = mat;
            return q;
        }

        private static Transform Node(string name, Transform parent, Vector3 localPos, Vector3? euler = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return t;
        }

        private static Transform Point(Ctx ctx, string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(ctx.Points, false);
            t.position = pos;
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        private struct Gap
        {
            public float Center, Width, Height;
            public Gap(float c, float w, float h) { Center = c; Width = w; Height = h; }
        }

        /// <summary>Wall running along X at the given Z, from y0 with height h, with optional openings.</summary>
        private static void WallX(string name, Transform p, float x0, float x1, float z, float y0, float h, Material m, params Gap[] gaps)
        {
            var list = new List<Gap>(gaps);
            list.Sort((a, b) => a.Center.CompareTo(b.Center));
            float cur = x0;
            int i = 0;
            foreach (var g in list)
            {
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cur + 0.01f) BoxMinMax($"{name}_{i++}", p, new Vector3(cur, y0, z - T / 2), new Vector3(g0, y0 + h, z + T / 2), m);
                if (g.Height < h - 0.01f)
                    BoxMinMax($"{name}_Lintel{i++}", p, new Vector3(g0, y0 + g.Height, z - T / 2), new Vector3(g1, y0 + h, z + T / 2), m);
                cur = g1;
            }
            if (x1 > cur + 0.01f) BoxMinMax($"{name}_{i}", p, new Vector3(cur, y0, z - T / 2), new Vector3(x1, y0 + h, z + T / 2), m);
        }

        /// <summary>Wall running along Z at the given X.</summary>
        private static void WallZ(string name, Transform p, float z0, float z1, float x, float y0, float h, Material m, params Gap[] gaps)
        {
            var list = new List<Gap>(gaps);
            list.Sort((a, b) => a.Center.CompareTo(b.Center));
            float cur = z0;
            int i = 0;
            foreach (var g in list)
            {
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cur + 0.01f) BoxMinMax($"{name}_{i++}", p, new Vector3(x - T / 2, y0, cur), new Vector3(x + T / 2, y0 + h, g0), m);
                if (g.Height < h - 0.01f)
                    BoxMinMax($"{name}_Lintel{i++}", p, new Vector3(x - T / 2, y0 + g.Height, g0), new Vector3(x + T / 2, y0 + h, g1), m);
                cur = g1;
            }
            if (z1 > cur + 0.01f) BoxMinMax($"{name}_{i}", p, new Vector3(x - T / 2, y0, cur), new Vector3(x + T / 2, y0 + h, z1), m);
        }

        private static GameObject Floor(string name, Transform p, float x0, float x1, float z0, float z1, float top, Material m,
            SurfaceType surface = SurfaceType.Wood)
        {
            var go = BoxMinMax(name, p, new Vector3(x0, top - 0.3f, z0), new Vector3(x1, top, z1), m);
            go.AddComponent<FootstepSurface>().Surface = surface;
            return go;
        }

        private static GameObject Ceiling(string name, Transform p, float x0, float x1, float z0, float z1, float bottom, Material m)
        {
            return BoxMinMax(name, p, new Vector3(x0 - 0.2f, bottom, z0 - 0.2f), new Vector3(x1 + 0.2f, bottom + 0.3f, z1 + 0.2f), m);
        }

        private static Light PointLight(string name, Transform p, Vector3 pos, Color c, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            bool useShadows = shadows && shadowBudget > 0;
            if (useShadows) shadowBudget--;
            l.shadows = useShadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        /// <summary>Hanging / wall lamp with an emissive bulb and an optional flicker.</summary>
        private static FlickerLight Lamp(string name, Transform p, Vector3 pos, Color c, float intensity, float range,
            bool flicker, float flickerChance = 0.08f, bool shadows = false, bool hanging = true)
        {
            var root = new GameObject(name).transform;
            root.SetParent(p, false);
            root.position = pos;
            if (hanging)
            {
                Prim(PrimitiveType.Cylinder, "Cord", root, new Vector3(0, 0.35f, 0), new Vector3(0.015f, 0.35f, 0.015f), M.DarkMetal);
                Prim(PrimitiveType.Cylinder, "Shade", root, new Vector3(0, 0.08f, 0), new Vector3(0.32f, 0.06f, 0.32f), M.DarkMetal);
            }
            var bulb = Prim(PrimitiveType.Sphere, "Bulb", root, Vector3.zero, Vector3.one * 0.1f, M.Bulb);
            var light = PointLight("Light", root, pos + Vector3.down * 0.05f, c, intensity * 2.4f, range * 1.4f, shadows);
            var fl = light.gameObject.AddComponent<FlickerLight>();
            fl.Configure(flicker, flickerChance, bulb.GetComponent<Renderer>());
            return fl;
        }

        private static TextMeshPro Text(string name, Transform p, Vector3 pos, Vector3 euler, string text, float fontSize, Color color,
            float width = 3f, float height = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(width, height);
            return tmp;
        }

        private static T Add<T>(GameObject go) where T : Component => go.AddComponent<T>();

        private static void Highlight(GameObject go) => go.AddComponent<Highlightable>();

        private static NavMeshObstacle Obstacle(GameObject go, Vector3 size)
        {
            var o = go.AddComponent<NavMeshObstacle>();
            o.shape = NavMeshObstacleShape.Box;
            o.size = size;
            o.center = Vector3.zero;
            o.carving = true;
            o.carveOnlyStationary = false;
            return o;
        }

        private static GameObject TriggerBox(string name, Transform p, Vector3 min, Vector3 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(p, false);
            go.transform.position = (min + max) * 0.5f;
            var c = go.AddComponent<BoxCollider>();
            c.isTrigger = true;
            c.size = max - min;
            return go;
        }

        private static void Serialize(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogWarning($"[7 MINUTES] Property '{property}' not found on {target.GetType().Name}");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
