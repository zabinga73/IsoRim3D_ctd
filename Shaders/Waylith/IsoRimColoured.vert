#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): plain vertex colours.
// Used for the mod's walls, rock, models, outlines, shadows and overlays. Blending, depth and
// culling are not set here: the mod makes one engine shader from this program for each way it
// draws (Source/WaylithShaders.cs).
//
// The camera's ViewProj comes from set 0, binding 0. Each draw's model matrix comes per instance,
// the way the engine hands it to its own shaders: Waylith draws meshes in instanced batches and
// has no other place for it.

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec4 inColor; // @vertex Color optional
layout(location = 2) in mat4 inModelMatrix; // @instance ModelMatrix required

layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;

layout(location = 0) out vec4 vColor;

void main()
{
    // An all-zero matrix would mean the engine gave none: drawn untransformed rather than lost.
    mat4 model = inModelMatrix[3][3] != 0.0 ? inModelMatrix : mat4(1.0);
    vColor = inColor;
    gl_Position = uVertex.ViewProj * (model * vec4(inPosition, 1.0));
}
