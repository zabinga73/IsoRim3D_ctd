#!/bin/sh
# Builds the mod's code for RimWorld 1.7, 1.6 and 1.5 (needs the .NET 10 SDK or later).
set -e
cd "$(dirname "$0")/Source"
rm -rf ../Assemblies obj
for v in 1.7 1.6 1.5; do
  echo "===== Building for RimWorld $v ====="
  dotnet build -c Release -nologo -p:RimWorldVersion=$v
done
echo "Done. 1.5/Assemblies, 1.6/Assemblies and 1.7/Assemblies are ready."
