Set-Location ModelTranspiler
dotnet build
Set-Location ..

Remove-Item Tests\gen\*.ts

# TODO: This will need to evolve
dotnet .\ModelTranspiler\bin\Debug\netcoreapp2.1\ModelTranspiler.dll .\TestSamples\Class1.cs > Tests\gen\Class1.ts

Set-Location Tests/
Remove-Item gen/*.js*
Remove-Item tests/*js*
npm run build
npm run test tests/*.js
Set-Location ..