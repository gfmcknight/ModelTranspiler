module WorkspaceTranspiler

open System
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.MSBuild
open System.Reflection

open CodeTranspiler
open ClassTranspiler
open System.IO
open Util
open TypeUtils

let hasTranspileAttribute (classNode : ClassDeclarationSyntax) =
    hasAttribute "Transpile" classNode.AttributeLists

let transpileAndWriteDocument (env: Env) (originalPath : string) (newPath : string) (ns : string, name : string, tree : ClassDeclarationSyntax) =
    let newFilePath = (newPath + (makeFilePath ns) + "/" +  name + ".ts")
    let newDocumentText = convertClass env newPath ns tree in
    
    if newDocumentText.Length > 0
        then File.WriteAllText(newFilePath, newDocumentText); newFilePath
        else "(Not Written)"    

let envEntriesFromNamespace (ns : NamespaceDeclarationSyntax) : (string * string * ClassDeclarationSyntax) seq =
    let nsName = (ns.Name.ToString()) in
    Seq.map (fun (classDeclaration : ClassDeclarationSyntax) -> (nsName, (classDeclaration.Identifier.ToString()), classDeclaration))
        (getChildrenOfType<ClassDeclarationSyntax> ns |> Seq.filter hasTranspileAttribute)

let makeEnv (trees : seq<SyntaxTree>) : Env = 
    let nodes = Seq.map (fun (tree : SyntaxTree) -> tree.GetRoot()) trees
    let namespaces = Seq.concat (Seq.map getChildrenOfType<NamespaceDeclarationSyntax> nodes) in
    let envEntries = Seq.concat (Seq.map envEntriesFromNamespace namespaces) in
    {
        classes = Seq.toList envEntries
    }

let makeDirectoriesFromEnv (newpath : string) (env : Env) : seq<DirectoryInfo> =
    (Seq.map (fun (path : string) -> (Directory.CreateDirectory(newpath + "/" + path)))
         (Seq.map (fun ((ns : string), (name : string), (decl : ClassDeclarationSyntax)) -> ns.Replace('.', '/')) env.classes))

let transpileProject (path : string) (newpath : string) (projName : string) = 
    let workspace = MSBuildWorkspace.Create() in
    workspace.WorkspaceFailed.Add(fun (args : WorkspaceDiagnosticEventArgs) -> Console.WriteLine(args.Diagnostic.Message) );
    
    let project = runTask<Project> (workspace.OpenProjectAsync(path + projName)) in
    let documents = Seq.filter (fun (doc: Document) -> 
                                    not ((doc.Name.Contains("AssemblyAttributes.cs")) || 
                                         (doc.Name.Contains("AssemblyInfo.cs")))) project.Documents in
    
    let trees = Seq.map (fun (d: Document) -> runTask (d.GetSyntaxTreeAsync())) documents in
    let env = makeEnv trees
    in let y = (makeDirectoriesFromEnv newpath env) |> Seq.toList
    Seq.map (transpileAndWriteDocument env path newpath) env.classes