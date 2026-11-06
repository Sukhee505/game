using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

namespace SevenMinutes.EditorTools
{
    /// <summary>All generated materials (filled by BuildAssets).</summary>
    internal static class M
    {
        public static Material Wallpaper, WallpaperHall, WallpaperRoomA, Plaster, WoodFloor, Carpet, Concrete, ConcreteDark, Stone,
            Metal, MetalFloor, DarkMetal, Rust, Hazard, Wood, WoodDark, Fabric, Pillow, Blanket, TvBody, Screen, Bulb, Paper,
            Photo, Newspaper, Blood, Glass, Tile, Lava, Grass, Path, Bark, Leaves, Gold, DoorDark, RobotBody, RobotJoint,
            RobotGlow, Skin, Hair, Shirt, Pants, Shoes, EyeWhite, Pupil, GlowRed, GlowGreen, GlowBlue, Laser, Spike,
            SymbolCircle, SymbolTriangle, SymbolSquare, SymbolStar, Workstation, Particle, Sky, Brick;
    }

    public static partial class HouseBuilder
    {
        private static VideoClip introClip;

        private static void BuildAssets()
        {
            introClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Media/IntroductionVideo.mp4");
            if (introClip == null)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Media" }))
                {
                    introClip = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guid));
                    if (introClip != null) break;
                }
            }
            Ctx_VideoFound = introClip != null;

            // ---- textures ----
            var wallpaper = Tex("Wallpaper", 256, (x, y) =>
            {
                float stripe = Mathf.Repeat(x / 32f, 1f) < 0.5f ? 1f : 0.88f;
                float n = Noise(x, y, 0.05f) * 0.25f + Noise(x, y, 0.3f) * 0.08f;
                float stain = Mathf.Clamp01(Noise(x + 100, y, 0.012f) - 0.55f) * 1.5f;
                float v = stripe * (0.75f + n) - stain * 0.35f;
                return new Color(0.32f * v, 0.36f * v, 0.3f * v);
            });
            var wood = Tex("WoodFloor", 256, (x, y) =>
            {
                int plank = y / 32;
                float tone = 0.75f + Hash(plank) * 0.3f;
                float grain = Mathf.Sin((x + Hash(plank * 7) * 200f) * 0.08f + Noise(x, y, 0.05f) * 6f) * 0.08f;
                float edge = (y % 32) < 2 || ((x + plank * 61) % 128) < 2 ? 0.45f : 1f;
                float v = (tone + grain) * edge;
                return new Color(0.33f * v, 0.22f * v, 0.14f * v);
            });
            var carpet = Tex("Carpet", 128, (x, y) =>
            {
                float n = Noise(x, y, 0.4f) * 0.25f + 0.75f;
                bool border = x < 8 || y < 8 || x > 119 || y > 119;
                return border ? new Color(0.25f * n, 0.17f * n, 0.08f * n) : new Color(0.32f * n, 0.05f * n, 0.06f * n);
            });
            var concrete = Tex("Concrete", 256, (x, y) =>
            {
                float v = 0.55f + Noise(x, y, 0.04f) * 0.25f + Noise(x, y, 0.4f) * 0.1f;
                float crack = Mathf.Abs(Noise(x, y, 0.02f) - 0.5f) < 0.006f ? 0.5f : 1f;
                return new Color(0.42f * v * crack, 0.42f * v * crack, 0.43f * v * crack);
            });
            var metal = Tex("MetalPlate", 256, (x, y) =>
            {
                float v = 0.7f + Noise(x, y, 0.08f) * 0.15f + Noise(x, y, 0.6f) * 0.05f;
                bool seam = (x % 128) < 2 || (y % 128) < 2;
                bool rivet = ((x % 128) - 10) * ((x % 128) - 10) + ((y % 128) - 10) * ((y % 128) - 10) < 12;
                float rust = Mathf.Clamp01(Noise(x + 40, y + 70, 0.03f) - 0.62f) * 2f;
                if (seam) v *= 0.45f;
                if (rivet) v *= 1.3f;
                return Color.Lerp(new Color(0.45f * v, 0.46f * v, 0.48f * v), new Color(0.35f, 0.18f, 0.08f), rust);
            });
            var grate = Tex("Grate", 128, (x, y) =>
            {
                bool bar = (x % 16) < 4 || (y % 16) < 4;
                return bar ? new Color(0.35f, 0.35f, 0.37f) : new Color(0.04f, 0.04f, 0.05f);
            });
            var hazard = Tex("Hazard", 128, (x, y) => Mathf.Repeat((x + y) / 32f, 1f) < 0.5f ? new Color(0.9f, 0.7f, 0.05f) : new Color(0.06f, 0.06f, 0.06f));
            var tile = Tex("MemoryTile", 128, (x, y) =>
            {
                int b = Mathf.Min(Mathf.Min(x, 127 - x), Mathf.Min(y, 127 - y));
                float n = 0.85f + Noise(x, y, 0.1f) * 0.2f;
                if (b < 5) return new Color(0.75f, 0.7f, 0.55f);       // visible metallic border
                if (b < 8) return new Color(0.05f, 0.05f, 0.06f);
                return new Color(0.16f * n, 0.17f * n, 0.2f * n);
            });
            var lava = Tex("Lava", 256, (x, y) =>
            {
                float n = Noise(x, y, 0.025f) * 0.6f + Noise(x, y, 0.08f) * 0.3f + Noise(x, y, 0.25f) * 0.1f;
                float crust = Mathf.Clamp01((n - 0.55f) * 4f);
                Color hot = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.3f, 0.02f), Mathf.Clamp01(n * 1.6f));
                return Color.Lerp(hot, new Color(0.08f, 0.03f, 0.02f), crust);
            });
            var grass = Tex("Grass", 256, (x, y) =>
            {
                float n = Noise(x, y, 0.06f) * 0.3f + Noise(x, y, 0.7f) * 0.25f;
                return new Color(0.12f + n * 0.1f, 0.3f + n * 0.25f, 0.08f + n * 0.05f);
            });
            var stone = Tex("Stone", 256, (x, y) =>
            {
                float n = Noise(x, y, 0.03f) * 0.5f + Noise(x, y, 0.15f) * 0.3f + Noise(x, y, 0.6f) * 0.2f;
                return new Color(0.16f + n * 0.12f, 0.12f + n * 0.09f, 0.1f + n * 0.08f);
            });
            var brick = Tex("Brick", 256, (x, y) =>
            {
                int row = y / 32;
                int bx = (x + (row % 2) * 32) % 64;
                bool mortar = (y % 32) < 3 || bx < 3;
                float n = 0.8f + Hash(row * 13 + (x + (row % 2) * 32) / 64) * 0.3f + Noise(x, y, 0.3f) * 0.1f;
                return mortar ? new Color(0.25f, 0.24f, 0.22f) : new Color(0.38f * n, 0.17f * n, 0.12f * n);
            });
            var bark = Tex("Bark", 128, (x, y) =>
            {
                float n = Mathf.Abs(Mathf.Sin(x * 0.3f + Noise(x, y, 0.08f) * 5f)) * 0.4f + 0.6f;
                return new Color(0.25f * n, 0.17f * n, 0.1f * n);
            });
            var leaves = Tex("Leaves", 128, (x, y) =>
            {
                float n = Noise(x, y, 0.2f);
                return new Color(0.08f + n * 0.1f, 0.25f + n * 0.25f, 0.07f + n * 0.05f);
            });
            var path = Tex("PathStone", 128, (x, y) =>
            {
                float n = Noise(x, y, 0.1f) * 0.3f + 0.6f;
                bool joint = Mathf.Abs(Noise(x, y, 0.05f) - 0.5f) < 0.02f;
                return joint ? new Color(0.15f, 0.13f, 0.1f) : new Color(0.5f * n, 0.48f * n, 0.44f * n);
            });
            var paper = Tex("Paper", 128, (x, y) =>
            {
                float n = 0.9f + Noise(x, y, 0.2f) * 0.1f;
                bool line = y % 12 == 0 && y > 12 && y < 116 && x > 10 && x < 118;
                return line ? new Color(0.45f, 0.45f, 0.5f) : new Color(0.85f * n, 0.82f * n, 0.7f * n);
            });
            var news = Tex("Newspaper", 128, (x, y) =>
            {
                float n = 0.85f + Noise(x, y, 0.3f) * 0.1f;
                bool headline = y > 100 && y < 116 && x > 8 && x < 120 && (x / 6) % 5 != 4;
                bool text = y < 92 && y > 6 && (y % 6) < 2 && ((x / 4) % 7) != 6 && (x < 60 || x > 66) && x > 6 && x < 122;
                bool photo = x > 70 && x < 118 && y > 50 && y < 90;
                if (headline) return new Color(0.1f, 0.1f, 0.1f);
                if (photo) return new Color(0.25f, 0.25f, 0.25f) * (0.6f + Noise(x, y, 0.2f));
                return text ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.78f * n, 0.75f * n, 0.65f * n);
            });
            var photo = Tex("Photograph", 128, (x, y) =>
            {
                bool frame = x < 6 || y < 6 || x > 121 || y > 121;
                if (frame) return new Color(0.9f, 0.88f, 0.8f);
                float v = 0.25f + y / 300f + Noise(x, y, 0.15f) * 0.15f;
                Color c = new Color(0.5f * v, 0.42f * v, 0.32f * v);
                // four silhouettes
                for (int i = 0; i < 4; i++)
                {
                    float cx = 26 + i * 25, h = i < 2 ? 70 : 52;
                    float dx = Mathf.Abs(x - cx);
                    bool body = dx < 8 && y < h && y > 10;
                    bool head = (x - cx) * (x - cx) + (y - h - 7) * (y - h - 7) < 64;
                    if (body || head) c = new Color(0.08f, 0.06f, 0.05f);
                    if (head && i < 2 && Noise(x, y, 0.9f) > 0.4f) c = new Color(0.85f, 0.8f, 0.7f); // scratched faces
                }
                return c;
            });
            var blood = Tex("Blood", 128, (x, y) =>
            {
                float dx = (x - 64) / 64f, dy = (y - 64) / 64f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) + (Noise(x, y, 0.08f) - 0.5f) * 0.6f;
                float a = d < 0.6f ? 1f : 0f;
                return new Color(0.25f, 0.01f, 0.01f, a);
            }, true);
            var particle = Tex("SoftParticle", 64, (x, y) =>
            {
                float dx = (x - 32) / 32f, dy = (y - 32) / 32f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                return new Color(1f, 1f, 1f, a * a);
            }, true);
            var symCircle = SymbolTex("SymCircle", 0);
            var symTriangle = SymbolTex("SymTriangle", 1);
            var symSquare = SymbolTex("SymSquare", 2);
            var symStar = SymbolTex("SymStar", 3);
            var workstation = Tex("WorkstationScreen", 256, (x, y) =>
            {
                Color bg = new Color(0.01f, 0.05f, 0.02f) * (0.9f + (y % 4 == 0 ? 0.3f : 0f));
                // sequence: star, triangle, circle, square
                int[] seq = { 3, 1, 0, 2 };
                for (int i = 0; i < 4; i++)
                {
                    float cx = 32 + i * 64, cy = 120;
                    if (SymbolMask(seq[i], (x - cx) / 26f, (y - cy) / 26f)) return new Color(0.3f, 1f, 0.45f);
                    // numbers as dots under symbols
                    for (int d = 0; d <= i; d++)
                        if ((x - (cx - i * 5 + d * 10)) * (x - (cx - i * 5 + d * 10)) + (y - 70) * (y - 70) < 10) return new Color(0.3f, 1f, 0.45f);
                }
                bool header = y > 200 && y < 214 && x > 20 && x < 236 && (x / 8) % 4 != 3;
                return header ? new Color(0.2f, 0.7f, 0.3f) : bg;
            });

            // ---- materials ----
            M.Wallpaper = Lit("Wallpaper", Color.white, 0.15f, 0f, wallpaper);
            M.WallpaperHall = Lit("WallpaperHall", new Color(0.85f, 0.75f, 0.7f), 0.15f, 0f, wallpaper);
            M.WallpaperRoomA = Lit("WallpaperRoomA", new Color(0.8f, 0.85f, 1f), 0.1f, 0f, wallpaper);
            M.Plaster = Lit("Plaster", new Color(0.25f, 0.24f, 0.23f), 0.05f, 0f, concrete);
            M.WoodFloor = Lit("WoodFloor", Color.white, 0.35f, 0f, wood);
            M.Carpet = Lit("Carpet", Color.white, 0.05f, 0f, carpet);
            M.Concrete = Lit("Concrete", Color.white, 0.2f, 0f, concrete);
            M.ConcreteDark = Lit("ConcreteDark", new Color(0.5f, 0.5f, 0.52f), 0.2f, 0f, concrete);
            M.Stone = Lit("Stone", Color.white, 0.15f, 0f, stone);
            M.Brick = Lit("Brick", Color.white, 0.1f, 0f, brick);
            M.Metal = Lit("Metal", Color.white, 0.55f, 0.7f, metal);
            M.MetalFloor = Lit("MetalGrate", Color.white, 0.5f, 0.8f, grate);
            M.DarkMetal = Lit("DarkMetal", new Color(0.12f, 0.12f, 0.13f), 0.6f, 0.85f);
            M.Rust = Lit("Rust", new Color(0.35f, 0.17f, 0.08f), 0.2f, 0.4f, metal);
            M.Hazard = Lit("Hazard", Color.white, 0.4f, 0.2f, hazard);
            M.Wood = Lit("Wood", new Color(0.8f, 0.7f, 0.6f), 0.35f, 0f, wood);
            M.WoodDark = Lit("WoodDark", new Color(0.45f, 0.35f, 0.3f), 0.4f, 0f, wood);
            M.Fabric = Lit("BedSheet", new Color(0.55f, 0.55f, 0.6f), 0.05f, 0f, carpet);
            M.Pillow = Lit("Pillow", new Color(0.75f, 0.73f, 0.7f), 0.05f);
            M.Blanket = Lit("Blanket", new Color(0.2f, 0.25f, 0.35f), 0.05f, 0f, carpet);
            M.TvBody = Lit("TvBody", new Color(0.03f, 0.03f, 0.03f), 0.7f, 0.2f);
            M.Screen = Unlit("TvScreen", new Color(0.01f, 0.01f, 0.012f));
            M.Bulb = Lit("Bulb", new Color(1f, 0.85f, 0.6f), 0.5f, 0f, null, new Color(1f, 0.8f, 0.5f) * 2f);
            M.Paper = Lit("Paper", Color.white, 0.1f, 0f, paper);
            M.Photo = Lit("Photo", Color.white, 0.4f, 0f, photo);
            M.Newspaper = Lit("Newspaper", Color.white, 0.1f, 0f, news);
            M.Blood = Lit("Blood", Color.white, 0.7f, 0f, blood, null, true);
            M.Glass = Transparent("Glass", new Color(0.55f, 0.65f, 0.7f, 0.18f));
            M.Tile = Lit("MemoryTile", Color.white, 0.75f, 0.3f, tile, Color.black);
            M.Lava = Lit("Lava", Color.white, 0.6f, 0f, lava, new Color(1f, 0.35f, 0.05f) * 2.2f, false, lava);
            M.Grass = Lit("Grass", Color.white, 0.1f, 0f, grass);
            M.Path = Lit("Path", Color.white, 0.2f, 0f, path);
            M.Bark = Lit("Bark", Color.white, 0.1f, 0f, bark);
            M.Leaves = Lit("Leaves", Color.white, 0.15f, 0f, leaves);
            M.Gold = Lit("Gold", new Color(0.75f, 0.55f, 0.2f), 0.75f, 1f);
            M.DoorDark = Lit("DoorDark", new Color(0.35f, 0.36f, 0.4f), 0.6f, 0.9f, metal);
            M.RobotBody = Lit("RobotBody", new Color(0.32f, 0.34f, 0.37f), 0.7f, 0.85f);
            M.RobotJoint = Lit("RobotJoint", new Color(0.06f, 0.06f, 0.07f), 0.5f, 0.9f);
            M.RobotGlow = Lit("RobotGlow", new Color(0.05f, 0.05f, 0.06f), 0.9f, 0f, null, Color.black);
            M.Skin = Lit("Skin", new Color(0.78f, 0.6f, 0.48f), 0.3f);
            M.Hair = Lit("Hair", new Color(0.08f, 0.06f, 0.05f), 0.3f);
            M.Shirt = Lit("Shirt", new Color(0.35f, 0.38f, 0.42f), 0.1f, 0f, carpet);
            M.Pants = Lit("Pants", new Color(0.12f, 0.14f, 0.2f), 0.1f);
            M.Shoes = Lit("Shoes", new Color(0.06f, 0.05f, 0.05f), 0.4f);
            M.EyeWhite = Lit("EyeWhite", new Color(0.9f, 0.9f, 0.88f), 0.8f);
            M.Pupil = Lit("Pupil", new Color(0.03f, 0.03f, 0.03f), 0.9f);
            M.GlowRed = Lit("GlowRed", new Color(1f, 0.1f, 0.05f), 0.5f, 0f, null, new Color(1f, 0.08f, 0.04f) * 3f);
            M.GlowGreen = Lit("GlowGreen", new Color(0.1f, 1f, 0.3f), 0.5f, 0f, null, new Color(0.1f, 1f, 0.3f) * 2f);
            M.GlowBlue = Lit("GlowBlue", new Color(0.2f, 0.6f, 1f), 0.5f, 0f, null, new Color(0.2f, 0.6f, 1f) * 2f);
            M.Laser = Lit("Laser", new Color(1f, 0.05f, 0.05f), 0.5f, 0f, null, new Color(1f, 0.02f, 0.02f) * 6f);
            M.Spike = Lit("Spike", new Color(0.5f, 0.5f, 0.52f), 0.8f, 1f);
            M.SymbolCircle = Lit("SymbolCircle", new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f, symCircle, new Color(0.6f, 0.8f, 1f) * 2.5f, false, symCircle);
            M.SymbolTriangle = Lit("SymbolTriangle", new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f, symTriangle, new Color(0.6f, 0.8f, 1f) * 2.5f, false, symTriangle);
            M.SymbolSquare = Lit("SymbolSquare", new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f, symSquare, new Color(0.6f, 0.8f, 1f) * 2.5f, false, symSquare);
            M.SymbolStar = Lit("SymbolStar", new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f, symStar, new Color(0.6f, 0.8f, 1f) * 2.5f, false, symStar);
            M.Workstation = Lit("Workstation", Color.white, 0.8f, 0f, workstation, Color.white * 1.2f, false, workstation);
            M.Particle = ParticleMat("Particle", particle);
            M.Sky = SkyMat();
        }

        // ---------------- texture painting ----------------

        private static float Hash(int n)
        {
            n = (n << 13) ^ n;
            return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
        }

        private static float Noise(float x, float y, float freq) => Mathf.PerlinNoise(x * freq + 13.7f, y * freq + 71.3f);

        private delegate Color Painter(int x, int y);

        private static Texture2D Tex(string name, int size, Painter painter, bool alpha = false)
        {
            var tex = new Texture2D(size, size, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = painter(x, y);
            tex.SetPixels(px);
            tex.Apply(true);
            AssetDatabase.CreateAsset(tex, $"{GenFolder}/Textures/{name}.asset");
            return tex;
        }

        private static bool SymbolMask(int symbol, float u, float v)
        {
            // u, v in [-1, 1]
            switch (symbol)
            {
                case 0: { float r = Mathf.Sqrt(u * u + v * v); return r < 0.85f && r > 0.55f; }
                case 1: { float y = v + 0.2f; return y > -0.6f && y < 0.9f && Mathf.Abs(u) < (0.9f - y) * 0.62f && !(y > -0.35f && y < 0.55f && Mathf.Abs(u) < (0.55f - y) * 0.45f); }
                case 2: { float a = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)); return a < 0.75f && a > 0.48f; }
                default:
                {
                    float ang = Mathf.Atan2(v, u) + Mathf.PI / 2f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float k = Mathf.Repeat(ang / (Mathf.PI * 2f / 5f), 1f);
                    float edge = Mathf.Lerp(0.38f, 0.9f, Mathf.Abs(k - 0.5f) * 2f);
                    return r < edge;
                }
            }
        }

        private static Texture2D SymbolTex(string name, int symbol)
        {
            return Tex(name, 128, (x, y) =>
            {
                float u = (x - 64) / 52f, v = (y - 64) / 52f;
                return SymbolMask(symbol, u, v) ? Color.white : new Color(0.02f, 0.02f, 0.025f);
            });
        }

        // ---------------- material factories ----------------

        private static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        private static Material Lit(string name, Color color, float smooth, float metal = 0f, Texture2D tex = null,
            Color? emission = null, bool alphaClip = false, Texture2D emissionMap = null)
        {
            var m = new Material(LitShader) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTexture("_MainTex", tex);
            }
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Metallic", metal);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                if (emissionMap != null) m.SetTexture("_EmissionMap", emissionMap);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (alphaClip)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
            }
            AssetDatabase.CreateAsset(m, $"{GenFolder}/Materials/{name}.mat");
            return m;
        }

        private static Material Unlit(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            var m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, $"{GenFolder}/Materials/{name}.mat");
            return m;
        }

        private static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Material Transparent(string name, Color color)
        {
            var m = new Material(LitShader) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.95f);
            m.SetFloat("_Metallic", 0.1f);
            MakeTransparent(m);
            AssetDatabase.CreateAsset(m, $"{GenFolder}/Materials/{name}.mat");
            return m;
        }

        private static Material ParticleMat(string name, Texture2D tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            var m = new Material(shader) { name = name };
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            MakeTransparent(m);
            AssetDatabase.CreateAsset(m, $"{GenFolder}/Materials/{name}.mat");
            return m;
        }

        private static Material SkyMat()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return null;
            var m = new Material(shader) { name = "DaySky" };
            m.SetFloat("_SunSize", 0.05f);
            m.SetFloat("_AtmosphereThickness", 1f);
            m.SetColor("_SkyTint", new Color(0.5f, 0.6f, 0.85f));
            m.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.06f));
            m.SetFloat("_Exposure", 1.3f);
            AssetDatabase.CreateAsset(m, $"{GenFolder}/Materials/DaySky.mat");
            return m;
        }
    }
}
