#if RW17
using System;
using System.Collections.Generic;
using System.IO;
using Verse;
using Waylith.Rendering;

namespace IsoRim
{
    // RimWorld 1.7 only. The mod's shaders for Waylith, the engine 1.7 runs on.
    //
    // Two GLSL programs live in Shaders/Waylith: one draws vertex colours (walls, rock, models,
    // shadows, overlays) and one draws a tiling texture (the textured faces of walls, models and
    // roofs). Waylith fixes blending, depth and culling per shader, so each program is made into
    // one engine shader for every DrawState the mod uses, the first time it is asked for. The
    // engine compiles the GLSL itself. Whatever goes wrong is logged once and that kind of drawing
    // falls back to the game's own shaders (see R3DShaders).
    public static class WaylithShaders
    {
        private const string ColouredProgram = "IsoRimColoured";
        private const string TexturedProgram = "IsoRimTextured";

        private static bool tried;
        private static bool ready;
        private static readonly Dictionary<string, UnityEngine.Shader> made = new Dictionary<string, UnityEngine.Shader>();

        // Whether the mod's shader files were found and handed to the engine.
        public static bool Ready
        {
            get
            {
                if (!tried)
                {
                    tried = true;
                    ready = Init();
                }
                return ready;
            }
        }

        public static UnityEngine.Shader Coloured(DrawState state)
        {
            return Get(ColouredProgram, state, false);
        }

        public static UnityEngine.Shader Textured(DrawState state)
        {
            return Get(TexturedProgram, state, true);
        }

        private static bool Init()
        {
            string dir = IsoRimMod.RootDir != null ? Path.Combine(IsoRimMod.RootDir, "Shaders", "Waylith") : null;
            if (dir == null || !File.Exists(Path.Combine(dir, ColouredProgram + ".vert")))
            {
                Log.Error("[IsoRim] The mod's shaders for RimWorld 1.7 are missing (expected in " + dir
                    + "). The 3D view falls back to the game's shaders and will not look right.");
                return false;
            }
            try
            {
                ShaderSourceLoader.RegisterShaderSourceDirectory(dir);
                string found;
                if (!ShaderSourceLoader.TryResolveShaderPath(ColouredProgram + ".vert", out found))
                {
                    Log.Error("[IsoRim] The engine did not pick up the shader folder " + dir + ". The 3D view falls back to the game's shaders.");
                    return false;
                }
                ModLog.Debug("[IsoRim] Waylith shader folder registered: " + dir);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[IsoRim] Registering the shader folder " + dir + " failed, so the 3D view falls back to the game's shaders: " + e);
                return false;
            }
        }

        private static UnityEngine.Shader Get(string program, DrawState state, bool textured)
        {
            if (!Ready)
            {
                return null;
            }
            string key = "IsoRim/" + program + "/" + state.Key;
            UnityEngine.Shader shader;
            if (made.TryGetValue(key, out shader))
            {
                return shader;
            }

            shader = null;
            try
            {
                ShaderProgramDefinition def = Definition(program, state, textured);
                List<string> errors = new List<string>(def.ConfigErrors());
                if (errors.Count > 0)
                {
                    Log.Error("[IsoRim] Shader " + key + " is set up wrongly: " + string.Join("; ", errors));
                }
                else
                {
                    Shader engine = new Shader(key, key, def);
                    if (engine.HasErrors)
                    {
                        Log.Error("[IsoRim] Shader " + key + " did not compile; see the engine's messages above.");
                    }
                    else
                    {
                        engine = ShaderRegistry.Register(engine);
                        shader = UnityEngine.Shader.FromEngineShader(key, engine);
                        ModLog.Debug("[IsoRim] Built shader " + key + ".");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[IsoRim] Building shader " + key + " failed: " + e);
                shader = null;
            }
            made[key] = shader;
            return shader;
        }

        private static ShaderProgramDefinition Definition(string program, DrawState state, bool textured)
        {
            ShaderProgramDefinition def = new ShaderProgramDefinition();
            def.vertexShaderFileName = program + ".vert";
            def.fragmentShaderFileName = program + ".frag";
            def.useTexture = textured;
            def.flipTextureST = false;
            def.transparent = !state.Opaque;
            def.depthTest = state.depthTest;
            def.depthWrite = state.depthWrite;
            def.cullMode = state.cull == Cull.Front ? FaceCull.Front : (state.cull == Cull.Back ? FaceCull.Back : FaceCull.None);
            def.colorWrite = true;
            def.stencilEnabled = false;
            if (state.Opaque)
            {
                def.blendMode = ShaderBlendMode.Opaque;
            }
            else if (state.src == Blend.SrcAlpha && state.dst == Blend.OneMinusSrcAlpha)
            {
                def.blendMode = ShaderBlendMode.Alpha;
            }
            else
            {
                def.blendMode = ShaderBlendMode.Custom;
            }
            def.blendSrcFactor = Factor(state.src);
            def.blendDstFactor = Factor(state.dst);
            // Unity's "Offset 1, 1", which Hidden/Internal-Colored applies for _ZBias 1.
            def.depthBiasConstantFactor = state.pushBack ? 1f : 0f;
            def.depthBiasSlopeFactor = state.pushBack ? 1f : 0f;
            def.renderQueue = new RenderQueueValue(state.queue);
            if (textured)
            {
                // The mod's textures are made to tile.
                def.textureWrapOverrides = new Dictionary<string, TextureWrapMode> { { "_MainTex", TextureWrapMode.Repeat } };
            }
            return def;
        }

        private static BlendFactor Factor(Blend b)
        {
            switch (b)
            {
                case Blend.Zero: return BlendFactor.Zero;
                case Blend.One: return BlendFactor.One;
                case Blend.SrcAlpha: return BlendFactor.SrcAlpha;
                case Blend.OneMinusSrcAlpha: return BlendFactor.OneMinusSrcAlpha;
                case Blend.SrcColor: return BlendFactor.SrcColor;
                case Blend.DstColor: return BlendFactor.DstColor;
                case Blend.OneMinusSrcColor: return BlendFactor.OneMinusSrcColor;
                default: return BlendFactor.OneMinusDstColor;
            }
        }
    }
}
#endif
