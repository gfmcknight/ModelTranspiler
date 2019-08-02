module CodeTranspiler

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

open Util

(* 
    Classes: namespace * class name * Tree
*)
type Env =
    { classes : (string * string * ClassDeclarationSyntax) list }

type Dependencies = (string * string) list

type Coded = 
        | Ignored 
        | Transpiled of SyntaxNode * Env
        | Hardcoded  of string
        | RPC of string

let rpcString = "throw \"NotImplemented: RPC\";"

let formatRPCTarget (target : string) = 
    rpcString

let transpileCode (node: SyntaxNode) (env: Env) : string =
    "throw \"NotImplemented: Transpiling\";\n"

let getTranpileDirective (node: MethodDeclarationSyntax) : Coded =
    if (hasAttribute "TranspileDirect" node.AttributeLists) then
        (Hardcoded
            (removeQuotes ((argFromAttribute 0 (getAttribute "TranspileDirect" node.AttributeLists)).ToString())))
    else if (hasAttribute "TranspileRPC" node.AttributeLists) then
        (RPC (node.Identifier.ToString()))
    else
        Ignored

let transpile (item: Coded) : string =
    match item with
      | Ignored               -> ""
      | Transpiled (node,env) -> transpileCode node env
      | Hardcoded code        -> code
      | RPC target            -> formatRPCTarget target

