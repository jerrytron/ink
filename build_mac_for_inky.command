cd "`dirname "$0"`"

dotnet publish -c Release -r osx-x64 --self-contained /p:PublishTrimmed=true /p:PublishSingleFile=true -o BuildForInky inklecate/inklecate.csproj
mv BuildForInky/inklecate BuildForInky/inklecate_mac


# Copy the runtime and compiler debug symbols in
cp ink-engine-runtime/bin/Release/netstandard2.0/ink-engine-runtime.pdb BuildForInky/
cp compiler/bin/Release/netstandard2.0/ink_compiler.pdb BuildForInky/