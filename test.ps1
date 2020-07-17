Remove-Item -Recurse -Force Tests\gen\*
"" >> Tests\gen\placeholder.txt

Remove-Item -Recurse -Force roundtrip\*.json

Set-Location ModelTranspiler
dotnet build
Set-Location ..

Set-Location UtilTests
dotnet build
dotnet test
Set-Location ..

Set-Location RoundTripper
dotnet build
dotnet run ".." create
Set-Location ..

dotnet .\ModelTranspiler\bin\Debug\netcoreapp3.1\ModelTranspiler.dll "TestSamples/" "Tests/gen/" "TestSamples.csproj"

Set-Location SampleBackend
dotnet build
$backend = Start-Process dotnet -ArgumentList "run" -PassThru
Set-Location ..

Set-Location Tests/
Remove-Item tests/*js*
npm run build
npm run test tests/*.js
npm run roundtrip
Set-Location ../RoundTripper

dotnet run ".."
Set-Location ..

taskkill.exe /pid $backend.Id /t /f