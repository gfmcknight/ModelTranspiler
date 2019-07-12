open System
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis
open System.Reflection
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.Build.Locator

open ClassTranspiler
open WorkspaceTranspiler

[<EntryPoint>]
let Main(args: string []) =
    let (fromPath, toPath, projName, waitForInput) =
        match (args |> Array.toList) with
            | [] -> ("C:\\Users\\gfm13\\Source\\Repos\\ModelTranspiler\\TestSamples\\",
                     "C:\\Users\\gfm13\\Source\\Repos\\ModelTranspiler\\Tests\\gen\\",
                     "TestSamples.csproj", true)
            | (a::b::c::[]) -> (a, b, c, false)
            | _ -> printfn "Must specify 3 args or none"; exit(1); ("","","", false)
    in

    MSBuildLocator.RegisterDefaults() |> ignore;
    transpileProject fromPath toPath projName |> Seq.map ((+) ";") |> Seq.fold (+) "" |> (printfn "%s");
    if waitForInput then
        Console.ReadLine() |> ignore; 0
    else 0
