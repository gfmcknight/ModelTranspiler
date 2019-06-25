Set-Location ModelTranspiler
dotnet build
Set-Location ..

Remove-Item -Recurse -Force Tests\gen\*
"" >> Tests\gen\placeholder.txt

# TODO: This will need to evolve
dotnet .\ModelTranspiler\bin\Debug\netcoreapp2.1\ModelTranspiler.dll "TestSamples/" "Tests/gen/" "TestSamples.csproj"

Set-Location Tests/gen
Remove-Item gen/*.js*
Remove-Item tests/*js*
npm run build
npm run test tests/*.js
Set-Location ..