#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): the texture times the material's colour, cut out
// below half alpha like the game's Cutout shader, which the mod uses on RimWorld 1.5 and 1.6.

layout(location = 1) in vec2 vUv;

layout(set = 2, binding = 0) uniform IsoRimTexturedMaterial
{
    vec4 _Color; // @default vec4(1.0, 1.0, 1.0, 1.0)
} uMaterial;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;

layout(location = 0) out vec4 outColor;

void main()
{
    vec4 c = texture(_MainTex, vUv) * uMaterial._Color;
    if (c.a < 0.5)
    {
        discard;
    }
    outColor = vec4(c.rgb, 1.0);
}
