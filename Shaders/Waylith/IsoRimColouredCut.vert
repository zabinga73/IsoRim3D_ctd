#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): IsoRimColoured, plus the see-through round the
// pointer worked out here, on the graphics card, so that it follows the pointer every frame
// without the map having to be rebuilt (Source/Dynamic.cs, Cutaway).
//
// _IsoRimCutShape.x says what a material does near the pointer: 1 sinks it toward the ground
// (walls, rock, doors' surroundings, fittings on walls), 2 dissolves it (leaves, in the fragment
// shader). The other values are the same for every material of a kind and are set each frame:
//   _IsoRimCut       x, y: the centre on the ground (map x, z); z: the reach; w: how far on (0 to 1)
//   _IsoRimCutShape  y: the share of the height left at the centre; z: the width of the soft edge
//   _IsoRimCutDir    x, y: the way the camera looks along the ground; z: 1 to spare what is behind
// This block has to be the same, member for member, as the one in IsoRimColouredCut.frag.

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec4 inColor; // @vertex Color optional
layout(location = 2) in mat4 inModelMatrix; // @instance ModelMatrix required

layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;

layout(set = 2, binding = 0) uniform IsoRimColouredCutMaterial
{
    vec4 _Color; // @default vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _IsoRimCut; // @default vec4(0.0, 0.0, 0.0, 0.0)
    vec4 _IsoRimCutShape; // @default vec4(0.0, 1.0, 1.0, 0.0)
    vec4 _IsoRimCutDir; // @default vec4(0.0, 0.0, 0.0, 0.0)
} uMaterial;

layout(location = 0) out vec4 vColor;
layout(location = 3) out vec3 vWorld;

// How much of the cut applies at this point on the ground: 1 in the middle, 0 outside the reach.
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
        // Squeezed toward the ground, by an amount that changes smoothly with the distance, so
        // pieces that meet at a point always move together and nothing opens up between them.
        world.y *= mix(1.0, uMaterial._IsoRimCutShape.y, CutAmount(world.xz));
    }
    vWorld = world.xyz;
    vColor = inColor;
    gl_Position = uVertex.ViewProj * world;
}
