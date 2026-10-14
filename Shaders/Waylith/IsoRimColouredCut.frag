#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): IsoRimColoured.frag, plus leaves that dissolve near
// the pointer (_IsoRimCutShape.x 2). See IsoRimColouredCut.vert for what the values mean; the
// block below has to be the same, member for member, as the one there.

layout(location = 0) in vec4 vColor;
layout(location = 3) in vec3 vWorld;

layout(set = 2, binding = 0) uniform IsoRimColouredCutMaterial
{
    vec4 _Color; // @default vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _IsoRimCut; // @default vec4(0.0, 0.0, 0.0, 0.0)
    vec4 _IsoRimCutShape; // @default vec4(0.0, 1.0, 1.0, 0.0)
    vec4 _IsoRimCutDir; // @default vec4(0.0, 0.0, 0.0, 0.0)
} uMaterial;

layout(location = 0) out vec4 outColor;

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

// A threshold from a 4 x 4 ordered pattern on the screen, so a part-dissolved crown thins out
// evenly instead of in blotches.
float Pattern(vec2 pixel)
{
    const float order[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
    ivec2 p = ivec2(mod(pixel, 4.0));
    return (order[p.x + p.y * 4] + 0.5) / 16.0;
}

void main()
{
    if (abs(uMaterial._IsoRimCutShape.x - 2.0) < 0.5 && CutAmount(vWorld.xz) > Pattern(gl_FragCoord.xy))
    {
        discard;
    }
    outColor = vColor * uMaterial._Color;
}
