module WorkspaceTranspiler

open System
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.MSBuild
open System.Reflection

open ClassTranspiler
open System.IO
open Util

let runTask<'T> (task: Task<'T>) =
    (task.Wait())
    task.Result

let hasTranspileAttribute (classNode : ClassDeclarationSyntax) =
    hasAttribute "Transpile" classNode.AttributeLists

let transpileDocument (document : Document) (tree : SyntaxTree) = 
    let myClasses = Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<ClassDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (tree.GetRoot().DescendantNodes()))
    in
    let myClassesAsClasses = Seq.cast<ClassDeclarationSyntax> myClasses in
    let annotatedClasses = Seq.filter hasTranspileAttribute myClassesAsClasses
    printfn "%s" document.FilePath;
    let transpilations = Seq.fold (+) "" (Seq.map convertClass annotatedClasses) in
    transpilations

let transpileAndWriteDocument (originalPath : string) (newPath : string) (document : Document, tree : SyntaxTree) =
    let newFilePath = (newPath + document.Name.Replace(".cs", ".ts"))
    let newDocumentText = transpileDocument document tree in
    
    if newDocumentText.Length > 0
        then File.WriteAllText(newFilePath, newDocumentText); newFilePath
        else "(Not Written)"
    

let transpileProject (path : string) (newpath : string) (projName : string) = 
    let workspace = MSBuildWorkspace.Create() in
    workspace.WorkspaceFailed.Add(fun (args : WorkspaceDiagnosticEventArgs) -> Console.WriteLine(args.Diagnostic.Message) );
    
    let project = runTask<Project> (workspace.OpenProjectAsync(path + projName)) in
    let documents = Seq.filter (fun (doc: Document) -> 
                                    not ((doc.Name.Contains("AssemblyAttributes.cs")) || 
                                         (doc.Name.Contains("AssemblyInfo.cs")))) project.Documents in
    
    let trees = Seq.map (fun (d: Document) -> runTask (d.GetSyntaxTreeAsync())) documents in
    Seq.map (transpileAndWriteDocument path newpath) (Seq.zip documents trees)