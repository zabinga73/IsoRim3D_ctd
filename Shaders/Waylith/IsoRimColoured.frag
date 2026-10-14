#version 450
// IsoRim for RimWorld 1.7 (Waylith engine): vertex colours times the material's colour, like
// Unity's Hidden/Internal-Colored, which the mod uses on RimWorld 1.5 and 1.6. The mod lights
// faces through the material colour, so this has to multiply, not replace.

layout(location = 0) in vec4 vColor;

layout(set = 2, binding = 0) uniform IsoRimColouredMaterial
{
    vec4 _Color; // @default vec4(1.0, 1.0, 1.0, 1.0)
} uMaterial;

layout(location = 0) out vec4 outColor;

void main()
{
    outColor = vColor * uMaterial._Color;
}
