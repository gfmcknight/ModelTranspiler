module Util

open System
open System.Reflection
open System.Linq.Expressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

let getAllAttributes (attributes: SyntaxList<AttributeListSyntax>) =
    let allAttributeLists = Seq.cast<AttributeListSyntax> attributes in 
    Seq.concat (Seq.map (fun (a : AttributeListSyntax) -> a.Attributes) allAttributeLists)

let hasAttribute (name : string) (attributes: SyntaxList<AttributeListSyntax>) =
    Seq.exists (fun (attributeNode: AttributeSyntax) -> (attributeNode.Name.ToString()) = name) 
                 (getAllAttributes attributes)