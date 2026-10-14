#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): a tiling texture, for the textured faces of walls,
// models and roofs. The transform is the same as in IsoRimColoured.vert. The texture coordinate
// goes out at location 1, where the engine's own shaders put it.

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec2 inUv; // @vertex TexCoord0 optional

layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;

layout(location = 1) out vec2 vUv;

void main()
{
    vUv = inUv;
    gl_Position = uVertex.ViewProj * (push.modelMatrix * vec4(inPosition, 1.0));
}
