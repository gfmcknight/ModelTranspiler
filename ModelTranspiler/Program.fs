open System
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis
open System.Reflection
open Microsoft.CodeAnalysis.CSharp.Syntax
open ClassTranspiler

let defaultProgramText = @"using System;
using System.Collections.Generic;
using System.Text;
 
namespace HelloWorld
{
    class Program
    {
        public int MyNumber { get; private set; }
        public double MyOtherProp { get; set; }
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}"

[<EntryPoint>]
let Main(args: string []) =

    let programText = (match List.ofArray(args) with
                        | []      -> defaultProgramText
                        | file::_ -> (System.IO.File.ReadAllText(file)))
    in

    let tree = CSharpSyntaxTree.ParseText(programText) in
    let root = tree.GetCompilationUnitRoot()

    let compilation = CSharpCompilation.Create("HelloWorld")
                        .AddReferences(MetadataReference.CreateFromFile(typeof<string>.Assembly.Location))
                        .AddSyntaxTrees(tree) in

    let model = compilation.GetSemanticModel(tree, false) in
    let myClasses = Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<ClassDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (root.DescendantNodes()))
    in

    let myClassesAsClasses = Seq.cast<ClassDeclarationSyntax> myClasses in


    printf "%s" (convertClass (Seq.head myClassesAsClasses))
    if ((Seq.length args) = 0) then Console.ReadLine(); 0 else 0
(*for i in root.DescendantNodesAndSelf() do
    printf "\n\n%s ###::: %s\n" (i.GetType().Name) (i.ToString()) *)