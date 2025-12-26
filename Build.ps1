# Build
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Run
./bin/Release/net8.0/atqr extract -?