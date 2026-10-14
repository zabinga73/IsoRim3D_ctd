#!/bin/sh
# Copies what players need into ../IsoRim_Release, ready to upload to the Steam Workshop.
# Run build.sh first.
cd "$(dirname "$0")" || exit 1
for v in 1.5 1.6 1.7; do
  [ -f $v/Assemblies/IsoRim.dll ] || { echo "$v/Assemblies/IsoRim.dll is missing: run build.sh first."; exit 1; }
done
OUT=../IsoRim_Release
rm -rf "$OUT" && mkdir -p "$OUT"
for d in About 1.5 1.6 1.7 Defs Models Textures LUTs Shaders; do cp -r "$d" "$OUT/"; done
cp README.txt LICENSE "$OUT/"
echo "Done: $OUT"
