open System
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis
open System.Reflection
open Microsoft.CodeAnalysis.CSharp.Syntax
open ClassTranspiler

let programText = @"using System;
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

let tree = CSharpSyntaxTree.ParseText(programText)
let root = tree.GetCompilationUnitRoot()

let compilation = CSharpCompilation.Create("HelloWorld")
                    .AddReferences(MetadataReference.CreateFromFile(typeof<string>.Assembly.Location))
                    .AddSyntaxTrees(tree)

let model = compilation.GetSemanticModel(tree, false)
let myClasses = Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<ClassDeclarationSyntax>, i))
                           (Seq.cast<SyntaxNode> (root.DescendantNodes()))

let myClassesAsClasses = Seq.cast<ClassDeclarationSyntax> myClasses


printf "%s" (convertClass (Seq.head myClassesAsClasses))
for i in root.DescendantNodesAndSelf() do
    printf "\n\n%s ###::: %s\n" (i.GetType().Name) (i.ToString())