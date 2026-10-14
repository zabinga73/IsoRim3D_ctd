using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace IsoRim
{
    // How a colour being drawn is mixed with what is already on screen. The names are the usual
    // blend factors: One/Zero is solid, SrcAlpha/OneMinusSrcAlpha is see-through.
    public enum Blend
    {
        Zero,
        One,
        SrcAlpha,
        OneMinusSrcAlpha,
        SrcColor,
        DstColor,
        OneMinusSrcColor,
        OneMinusDstColor
    }

    // Which faces are left out.
    public enum Cull
    {
        None,
        Front,   // the faces turned toward the camera: the inside-out shells used as outlines
        Back
    }

    // What a material does near the pointer when the see-through is worked out by the graphics
    // card (RimWorld 1.7, see Cutaway.Smooth): nothing, sink toward the ground like a wall that is
    // lowered, or dissolve like the leaves of a tree.
    public enum CutKind
    {
        None,
        Sink,
        Dissolve
    }

    // Everything about how one kind of the mod's drawing is put on screen, apart from its colours.
    public struct DrawState
    {
        public Blend src;
        public Blend dst;
        public bool depthWrite;   // hides whatever is drawn later behind it
        public bool depthTest;    // false: drawn over everything, whatever stands in front
        public Cull cull;
        public bool pushBack;     // set a touch further back in depth than it is (outlines)
        public int queue;         // drawing order: lower numbers first
        public bool cut;          // drawn by the shader that does the see-through itself (1.7)

        // Solid and depth-tested, with nothing culled: the mod's walls, rock and models.
        public static DrawState Solid(int queue)
        {
            return new DrawState { src = Blend.One, dst = Blend.Zero, depthWrite = true, depthTest = true, cull = Cull.None, queue = queue };
        }

        // See-through by its alpha. Hidden behind solid things, but does not hide anything itself.
        public static DrawState SeeThrough(int queue)
        {
            return new DrawState { src = Blend.SrcAlpha, dst = Blend.OneMinusSrcAlpha, depthWrite = false, depthTest = true, cull = Cull.None, queue = queue };
        }

        public bool Opaque
        {
            get { return src == Blend.One && dst == Blend.Zero; }
        }

        // A short name that differs whenever anything in the state does.
        public string Key
        {
            get
            {
                return src + "-" + dst + "-" + (depthTest ? "T" : "t") + (depthWrite ? "W" : "w") + "-" + cull
                    + (pushBack ? "-Back" : "") + "-" + queue + (cut ? "-Cut" : "");
            }
        }
    }

    // Makes the mod's own materials.
    //
    // Unity (RimWorld 1.5 and 1.6) has a built-in shader, Hidden/Internal-Colored, that reads its
    // blending, depth and culling from material properties, so every kind of drawing is that one
    // shader set up differently. RimWorld 1.7 runs on Ludeon's own engine, Waylith, which keeps
    // Unity's classes as a compatibility layer but fixes those settings in the shader itself:
    // setting them on a material does nothing there. On 1.7 the mod therefore builds a shader of
    // its own for each kind of drawing (WaylithShaders), and only falls back to the game's shaders
    // if that fails.
    public static class R3DShaders
    {
        // Unity's own numbers for UnityEngine.Rendering.BlendMode (indexed by Blend), CullMode and
        // CompareFunction, as Hidden/Internal-Colored reads them. Written out here because the Unity
        // layer of RimWorld 1.7 no longer has the first two.
        private static readonly int[] UnityBlend = { 0, 1, 5, 10, 3, 2, 6, 4 };
        private const int UnityCullOff = 0;
        private const int UnityCullFront = 1;
        private const int UnityCullBack = 2;
        private const int UnityLessEqual = 4;
        private const int UnityAlways = 8;

        // Textured faces are drawn after the plain faces they sit on (see ModelBaker.FaceLift).
        public const int TexturedQueue = 2450;

        private static bool vertexColourTried;
        private static Shader vertexColour;
        private static bool vertexColourIsUnity;   // Hidden/Internal-Colored, which honours every DrawState
        private static readonly Dictionary<string, Material> textured = new Dictionary<string, Material>();

        // Whether the mod's own Waylith shaders are in use (RimWorld 1.7).
        public static bool OwnShaders
        {
            get
            {
#if RW17
                return WaylithShaders.Ready;
#else
                return false;
#endif
            }
        }

        // A material drawing a mesh's vertex colours times the material's colour, in the given way.
        // Null if the game has no shader for it. With exact set, also null rather than a stand-in
        // that would ignore the blending and depth asked for (for drawing that is wrong without them).
        // kind says what it does near the pointer while the see-through is worked out by the
        // graphics card (Cutaway.Smooth); otherwise it is ignored.
        public static Material Coloured(string name, DrawState state, bool exact = false, CutKind kind = CutKind.None)
        {
#if RW17
            Shader own = null;
            if (kind != CutKind.None && Cutaway.Smooth)
            {
                DrawState cutState = state;
                cutState.cut = true;
                own = WaylithShaders.Coloured(cutState);
            }
            bool cut = own != null;
            if (own == null)
            {
                own = WaylithShaders.Coloured(state);
            }
            if (own != null)
            {
                Material w = new Material(own);
                w.name = name;
                w.color = Color.white;
                w.renderQueue = state.queue;
                if (cut)
                {
                    KeepCut(w, kind);
                }
                return w;
            }
            if (exact)
            {
                return null;
            }
#endif
            Shader shader = VertexColour;
            if (shader == null || (exact && !vertexColourIsUnity))
            {
                return null;
            }
            Material m = new Material(shader);
            m.name = name;
            m.SetInt("_SrcBlend", UnityBlend[(int)state.src]);
            m.SetInt("_DstBlend", UnityBlend[(int)state.dst]);
            m.SetInt("_ZWrite", state.depthWrite ? 1 : 0);
            m.SetInt("_ZTest", state.depthTest ? UnityLessEqual : UnityAlways);
            m.SetInt("_Cull", state.cull == Cull.Front ? UnityCullFront : (state.cull == Cull.Back ? UnityCullBack : UnityCullOff));
            if (state.pushBack)
            {
                m.SetFloat("_ZBias", 1f);
            }
            m.renderQueue = state.queue;
            return m;
        }

        // A new material showing a tiling texture times a colour, solid like the models. On Unity it
        // is the game's Cutout shader. queue 0 keeps the usual order for textured faces (the Cutout
        // shader's own on Unity, TexturedQueue with the mod's Waylith shader). kind as for Coloured.
        public static Material Textured(string name, Texture2D tex, Color color, int queue = 0, CutKind kind = CutKind.None)
        {
            Material m = null;
#if RW17
            DrawState state = DrawState.Solid(queue > 0 ? queue : TexturedQueue);
            Shader own = null;
            if (kind != CutKind.None && Cutaway.Smooth)
            {
                DrawState cutState = state;
                cutState.cut = true;
                own = WaylithShaders.Textured(cutState);
            }
            bool cut = own != null;
            if (own == null)
            {
                own = WaylithShaders.Textured(state);
            }
            if (own != null)
            {
                m = new Material(own);
                m.renderQueue = state.queue;
                if (cut)
                {
                    KeepCut(m, kind);
                }
            }
#endif
            if (m == null)
            {
                m = new Material(ShaderDatabase.Cutout);
                if (queue > 0)
                {
                    m.renderQueue = queue;
                }
            }
            m.name = name;
            m.mainTexture = tex;
            m.color = color;
            return m;
        }

        // The shared material for a texture and colour, made once and kept, like MaterialPool.
        public static Material TexturedShared(string path, Texture2D tex, Color color, CutKind kind = CutKind.None)
        {
            if (!OwnShaders)
            {
                return MaterialPool.MatFrom(path, ShaderDatabase.Cutout, color);
            }
            if (!Cutaway.Smooth)
            {
                kind = CutKind.None;
            }
            Color32 c = color;
            string key = path + "|" + c.r + "," + c.g + "," + c.b + "," + c.a + "|" + (int)kind;
            Material m;
            if (!textured.TryGetValue(key, out m) || m == null)
            {
                m = Textured("IsoRim_" + path.Substring(path.LastIndexOf('/') + 1), tex, color, 0, kind);
                textured[key] = m;
            }
            return m;
        }

        // ---- the see-through on the graphics card (RimWorld 1.7)
        //
        // Every material that sinks or dissolves near the pointer is kept here, so that Cutaway can
        // move the see-through each frame by changing three values on each of them. What the
        // values mean is written at the top of Shaders/Waylith/IsoRimColouredCut.vert.

        private static readonly int CutId = Shader.PropertyToID("_IsoRimCut");
        private static readonly int CutShapeId = Shader.PropertyToID("_IsoRimCutShape");
        private static readonly int CutDirId = Shader.PropertyToID("_IsoRimCutDir");
        private static readonly List<Material> sinking = new List<Material>();
        private static readonly List<Material> dissolving = new List<Material>();
        private static readonly Vector4[] cutValues = new Vector4[9];   // cut, shape and direction, for each CutKind

        // Starts moving the see-through on a material made for the cut shader, from what it is now.
        public static void KeepCut(Material m, CutKind kind)
        {
            if (m == null || kind == CutKind.None)
            {
                return;
            }
            (kind == CutKind.Sink ? sinking : dissolving).Add(m);
            ApplyCut(m, (int)kind * 3, true, true, true);
        }

        // The see-through for every material of one kind. Only what changed is passed on: while
        // the pointer moves, that is just its position.
        public static void SetCut(CutKind kind, Vector4 cut, Vector4 shape, Vector4 dir)
        {
            int k = (int)kind * 3;
            bool newCut = cutValues[k] != cut;
            bool newShape = cutValues[k + 1] != shape;
            bool newDir = cutValues[k + 2] != dir;
            if (kind == CutKind.None || !(newCut || newShape || newDir))
            {
                return;
            }
            cutValues[k] = cut;
            cutValues[k + 1] = shape;
            cutValues[k + 2] = dir;
            List<Material> list = kind == CutKind.Sink ? sinking : dissolving;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
                ApplyCut(list[i], k, newCut, newShape, newDir);
            }
        }

        private static void ApplyCut(Material m, int k, bool cut, bool shape, bool dir)
        {
            if (cut)
            {
                m.SetVector(CutId, cutValues[k]);
            }
            if (shape)
            {
                m.SetVector(CutShapeId, cutValues[k + 1]);
            }
            if (dir)
            {
                m.SetVector(CutDirId, cutValues[k + 2]);
            }
        }

        // The game's shader for plain vertex colours: Unity's built-in one, or failing that the
        // game's own VertexColor shader.
        private static Shader VertexColour
        {
            get
            {
                if (!vertexColourTried)
                {
                    vertexColourTried = true;
                    // Built into every Unity player; draws plain vertex colours.
                    vertexColour = Shader.Find("Hidden/Internal-Colored");
                    if (vertexColour != null && !vertexColour.isSupported)
                    {
                        vertexColour = null;
                    }
                    vertexColourIsUnity = vertexColour != null;
                    if (vertexColour == null)
                    {
                        FieldInfo f = AccessTools.Field(typeof(ShaderDatabase), "VertexColor");
                        vertexColour = f != null ? f.GetValue(null) as Shader : null;
                        ModLog.Warn("[IsoRim] Hidden/Internal-Colored is not available; using the game's VertexColor shader.");
                    }
                    if (vertexColour == null)
                    {
                        Log.Error("[IsoRim] No vertex-colour shader is available, so walls and models cannot be drawn in 3D.");
                    }
                }
                return vertexColour;
            }
        }
    }
}
