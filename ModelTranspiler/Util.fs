module Util

open System
open System.Reflection
open System.Linq.Expressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open System.Threading.Tasks

let getChildrenOfType<'T> (node : SyntaxNode) =
    (Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<'T>, i))
        (Seq.cast<SyntaxNode> (node.DescendantNodes())))
    |> Seq.cast<'T>

let getAllAttributes (attributes: SyntaxList<AttributeListSyntax>) =
    let allAttributeLists = Seq.cast<AttributeListSyntax> attributes in 
    Seq.collect (fun (a : AttributeListSyntax) -> a.Attributes) allAttributeLists

let hasAttribute (name : string) (attributes: SyntaxList<AttributeListSyntax>) =
    Seq.exists (fun (attributeNode: AttributeSyntax) -> (attributeNode.Name.ToString()) = name) 
                 (getAllAttributes attributes)

let getAttribute (name: string) (attributes: SyntaxList<AttributeListSyntax>) =
    Seq.find (fun (attributeNode: AttributeSyntax) -> (attributeNode.Name.ToString()) = name)
                (getAllAttributes attributes)

let argFromAttribute (arg: int) (attribute: AttributeSyntax) =
    (attribute.ArgumentList.Arguments.Item arg).Expression

let removeTypeOf (str: string) =
    let firstParen = str.IndexOf('(') in
    let lastParen = str.LastIndexOf(')') in
    str.Substring(firstParen + 1, lastParen - firstParen - 1)

let removeQuotes (str: string) =
    let firstQuote = str.IndexOf('"') in
    let lastQuote = str.LastIndexOf('"') in
    str.Substring(firstQuote + 1, lastQuote - firstQuote - 1)

let runTask<'T> (task: Task<'T>) =
    (task.Wait())
    task.Result

let relativePath (source: string) (destination: string) : string =
    let sourceParts = Seq.cast<string> (source.Split('/'))
    let destParts = Seq.cast<string>(destination.Split('/'))
    let rec cullMatching (source: seq<string>) (dest: seq<string>) =
        if (Seq.isEmpty source) || (Seq.isEmpty destination) 
            then (source, dest)
            else if Seq.head source = Seq.head dest 
                then cullMatching (Seq.tail source) (Seq.tail dest) 
                else (source, dest)
    in
    let (culledSource, culledDest) = cullMatching sourceParts destParts in
    let backtracks = "." + (Seq.fold (+) "" (Seq.map (fun _ -> "/..") culledSource)) in
    (Seq.fold (fun (acc: string) (elem: string) -> 
            acc + "/" + elem) backtracks culledDest)


let makeFilePath (ns: string) : string =
    (ns.Replace(".", "/"))

let flipZipDirection<'T, 'R> (sequence : ('T * 'R) list) : ('T list * 'R list) =
     List.fold (fun (s1, s2) (v1, v2) -> ([v1]@s1, [v2]@s2)) ([], []) sequence

let commaSeparatedList (items: seq<string>) : string =
    Seq.fold (fun (acc:string) (elem:string) -> match acc with "" -> elem | _ -> acc + ", " + elem)  "" items