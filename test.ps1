Remove-Item -Recurse -Force Tests\gen\*
"" >> Tests\gen\placeholder.txt

Remove-Item -Recurse -Force roundtrip\*.json

Set-Location ModelTranspiler
dotnet build
Set-Location ..

Set-Location RoundTripper
dotnet build
dotnet run ".." create
Set-Location ..

# TODO: This will need to evolve
dotnet .\ModelTranspiler\bin\Debug\netcoreapp2.1\ModelTranspiler.dll "TestSamples/" "Tests/gen/" "TestSamples.csproj"

Set-Location Tests/
Remove-Item tests/*js*
npm run build
npm run test tests/*.js
npm run roundtrip
Set-Location ../RoundTripper

dotnet run ".."
Set-Location ..