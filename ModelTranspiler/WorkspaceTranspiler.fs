module WorkspaceTranspiler

open System
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.MSBuild

open CodeTranspiler
open ClassTranspiler
open System.IO
open Util
open TypeUtils

let hasTranspileAttribute (classNode : ClassDeclarationSyntax) =
    hasAttribute "Transpile" classNode.AttributeLists

let transpileAndWriteDocument (env: Env) (originalPath : string) (newPath : string) (cls: ClassInfo) =
    let newFilePath = (newPath + (makeFilePath cls.ns) + "/" +  cls.name + ".ts")
    let newDocumentText = convertClass env newPath cls.ns cls.tree in
    
    if newDocumentText.Length > 0
        then File.WriteAllText(newFilePath, newDocumentText); newFilePath
        else "(Not Written)"

let envEntriesFromNamespace (ns : NamespaceDeclarationSyntax) : ClassInfo seq =
    let nsName = (ns.Name.ToString()) in

    Seq.map (fun (classDeclaration : ClassDeclarationSyntax) ->
                let baseType = match classDeclaration.BaseList with
                               | null -> None
                               | _    -> Some (classDeclaration.BaseList.GetFirstToken().GetNextToken().ToString())
                in
                { ns = nsName
                ; name = (classDeclaration.Identifier.ToString())
                ; tree = classDeclaration
                ; baseType = baseType
                }
            )
        (getChildrenOfType<ClassDeclarationSyntax> ns |> Seq.filter hasTranspileAttribute)

let makeEnv (trees : seq<SyntaxTree>) : Env = 
    let nodes = Seq.map (fun (tree : SyntaxTree) -> tree.GetRoot()) trees
    let namespaces = Seq.collect getChildrenOfType<NamespaceDeclarationSyntax> nodes in
    let envEntries = Seq.collect envEntriesFromNamespace namespaces in
    {
        classes = Seq.toList envEntries
    }

let makeInternalModule (genDirectory: string) (env: Env) =
    let rec sources (remaining : ClassInfo list) (all : ClassInfo list) : ClassInfo list * ClassInfo list  =
        match remaining with
        | [] -> ([], [])
        | _  -> let (sourceList, nonSourceList) = sources remaining.Tail all in
                let curr = remaining.Head
                if List.exists (fun (cls: ClassInfo) ->
                                    match cls.baseType with
                                    | None -> false
                                    | Some name -> name = curr.name) all
                then (sourceList, curr::nonSourceList)
                else (curr::sourceList, nonSourceList)
    in
    let rec sortByDeps (classes : ClassInfo list) : ClassInfo list =
        match classes with
        | [] -> []
        | _  -> let (sourceList, nonSourceList) = sources classes classes in
                    (sortByDeps nonSourceList) @ sourceList

    let internalModuleImports =
        "import RPCHandler from '../RPCHandler';\n" +
        Seq.fold (+) ""
            (Seq.map
                (fun (cls: ClassInfo) ->
                    "import " + cls.name + " from './" + (makeFilePath cls.ns) + "/" + cls.name + "';\n"
                )
            (sortByDeps (Seq.toList env.classes)))
    let exports =
        "export { " +
            (commaSeparatedList
                (Seq.map
                    (fun (cls: ClassInfo) -> cls.name)
                env.classes)
            ) +
            ", RPCHandler" +
            " };\n"
    in File.WriteAllText(genDirectory + "internal.ts", internalModuleImports + exports); "import.js"

let makeDirectoriesFromEnv (newpath : string) (env : Env) : seq<DirectoryInfo> =
    let pathFromClassInfo = (fun (classInfo : ClassInfo) -> classInfo.ns.Replace('.', '/')) in
    let createDir = (fun (path : string) -> (Directory.CreateDirectory(newpath + "/" + path))) in
    Seq.map (pathFromClassInfo >> createDir) env.classes

let transpileProject (path : string) (newpath : string) (projName : string) =
    let workspace = MSBuildWorkspace.Create() in
    workspace.WorkspaceFailed.Add(fun (args : WorkspaceDiagnosticEventArgs) -> Console.WriteLine(args.Diagnostic.Message) );

    let project = runTask<Project> (workspace.OpenProjectAsync(path + projName)) in
    let documents = Seq.filter (fun (doc: Document) -> 
                                    not ((doc.Name.Contains("AssemblyAttributes.cs")) || 
                                         (doc.Name.Contains("AssemblyInfo.cs")))) project.Documents in
    
    let trees = Seq.map (fun (d: Document) -> runTask (d.GetSyntaxTreeAsync())) documents in
    let env = makeEnv trees
    in let y = ((makeDirectoriesFromEnv newpath env) |> Seq.toList) in
    let internalWrite = Seq.singleton (makeInternalModule newpath env) in
    Seq.append internalWrite (Seq.map (transpileAndWriteDocument env path newpath) env.classes)