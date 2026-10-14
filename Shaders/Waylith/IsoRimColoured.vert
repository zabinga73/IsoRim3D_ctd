#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): plain vertex colours.
// Used for the mod's walls, rock, models, outlines, shadows and overlays. Blending, depth and
// culling are not set here: the mod makes one engine shader from this program for each way it
// draws (Source/WaylithShaders.cs).
//
// The transform is the engine's own, as in Waylith's built-in fallback shader: the camera's
// ViewProj (set 0, binding 0) times the model matrix of each draw (push constant).

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec4 inColor; // @vertex Color optional

layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;

layout(location = 0) out vec4 vColor;

void main()
{
    vColor = inColor;
    gl_Position = uVertex.ViewProj * (push.modelMatrix * vec4(inPosition, 1.0));
}
