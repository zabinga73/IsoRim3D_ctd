#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): IsoRimTextured, plus the see-through round the
// pointer; see IsoRimColouredCut.vert, which this follows. The block below has to be the same,
// member for member, as the one in IsoRimTexturedCut.frag.

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec2 inUv; // @vertex TexCoord0 optional
layout(location = 2) in mat4 inModelMatrix; // @instance ModelMatrix required

layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;

layout(set = 2, binding = 0) uniform IsoRimTexturedCutMaterial
{
    vec4 _Color; // @default vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _IsoRimCut; // @default vec4(0.0, 0.0, 0.0, 0.0)
    vec4 _IsoRimCutShape; // @default vec4(0.0, 1.0, 1.0, 0.0)
    vec4 _IsoRimCutDir; // @default vec4(0.0, 0.0, 0.0, 0.0)
} uMaterial;

layout(location = 1) out vec2 vUv;
layout(location = 3) out vec3 vWorld;

float CutAmount(vec2 xz)
{
    vec4 cut = uMaterial._IsoRimCut;
    if (cut.w <= 0.0)
    {
        return 0.0;
    }
    vec2 off = xz - cut.xy;
    float soft = max(uMaterial._IsoRimCutShape.z, 0.001);
    float t = 1.0 - smoothstep(cut.z - soft, cut.z, length(off));
    if (uMaterial._IsoRimCutDir.z > 0.5)
    {
        t *= 1.0 - smoothstep(0.1, 1.1, dot(off, uMaterial._IsoRimCutDir.xy));
    }
    return t * cut.w;
}

void main()
{
    // An all-zero matrix would mean the engine gave none: drawn untransformed rather than lost.
    mat4 model = inModelMatrix[3][3] != 0.0 ? inModelMatrix : mat4(1.0);
    vec4 world = model * vec4(inPosition, 1.0);
    if (abs(uMaterial._IsoRimCutShape.x - 1.0) < 0.5)
    {
        world.y *= mix(1.0, uMaterial._IsoRimCutShape.y, CutAmount(world.xz));
    }
    vWorld = world.xyz;
    vUv = inUv;
    gl_Position = uVertex.ViewProj * world;
}
