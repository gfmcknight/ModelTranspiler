module CodeTranspiler

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp

(* TODO *)
type Env = int 

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

