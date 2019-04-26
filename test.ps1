Set-Location ModelTranspiler
dotnet build
Set-Location ..

# TODO: This will need to evolve
dotnet .\ModelTranspiler\bin\Debug\netcoreapp2.1\ModelTranspiler.dll .\TestSamples\Class1.cs > Tests\gen\Class1.ts

Set-Location Tests/
npm run test
Set-Location ..