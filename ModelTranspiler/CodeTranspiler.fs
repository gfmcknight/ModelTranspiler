module CodeTranspiler

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

(* 
    Classes: new path * class name * Tree
*)
type Env =
    { classes : (string * string * ClassDeclarationSyntax) list }

type Coded = 
        | Ignored 
        | Transpiled of SyntaxNode * Env
        | Hardcoded  of string
        | RPC of string

let rpcString = "throw \"NotImplemented: RPC\";"

let formatRPCTarget (target : string) = 
    rpcString

let transpileCode (node: SyntaxNode) (env: Env) =
    "throw \"NotImplemented: Transpiling\";"

let transpile (item: Coded) =
    match item with
      | Ignored               -> ""
      | Transpiled (node,env) -> transpileCode node env
      | Hardcoded code        -> code
      | RPC target            -> formatRPCTarget target

