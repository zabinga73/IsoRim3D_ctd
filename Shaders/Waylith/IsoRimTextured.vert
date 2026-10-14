#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): a tiling texture, for the textured faces of walls,
// models and roofs. The transform is the same as in IsoRimColoured.vert. The texture coordinate
// goes out at location 1, where the engine's own shaders put it.

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec2 inUv; // @vertex TexCoord0 optional
layout(location = 2) in mat4 inModelMatrix; // @instance ModelMatrix required

layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;

layout(location = 1) out vec2 vUv;

void main()
{
    // An all-zero matrix would mean the engine gave none: drawn untransformed rather than lost.
    mat4 model = inModelMatrix[3][3] != 0.0 ? inModelMatrix : mat4(1.0);
    vUv = inUv;
    gl_Position = uVertex.ViewProj * (model * vec4(inPosition, 1.0));
}
