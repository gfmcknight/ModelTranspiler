module CodeTranspiler

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

open TypeUtils
open Util

type Coded = 
        | Ignored 
        | Transpiled of SyntaxNode * Env
        | Hardcoded of string
        // ClassName, MethodName, ReturnType
        | RPC of string * string * string

// Expect the rpcHandler to be one level above the gen/ folder
let rpcHandlerDependency = [("RPCHandler", "../RPCHandler")]

let rpcString = "throw \"NotImplemented: RPC\";"

let formatRPCTarget (env: Env) (className: string) (methodName: string) (returnType: string) =
    "\n" +
    "let result : any = await RPCHandler.handleRPCRequest(this, Array.from(arguments), '"+
    className + "', '" + methodName + "');\n" +
    "this._init(result.ThisObject);\n" +
    "return " + (fromJSONObject env returnType "result.ReturnValue") + ";" +
    "\n"

let transpileCode (node: SyntaxNode) (env: Env) : string =
    "throw \"NotImplemented: Transpiling\";\n"

let getTranpileDirective (node: MethodDeclarationSyntax) (className: string)
        (returnType: string) : (Coded * Dependencies) =

    if (hasAttribute "TranspileDirect" node.AttributeLists) then
        (Hardcoded
            (removeQuotes ((argFromAttribute 0 (getAttribute "TranspileDirect" node.AttributeLists)).ToString())), [])
    else if (hasAttribute "TranspileRPC" node.AttributeLists) then
        (
            RPC (
                className,
                node.Identifier.ToString(),
                returnType
            ),
            rpcHandlerDependency
        )
    else
        (Ignored, [])

let transpile (env: Env) (item: Coded) : string =
    match item with
      | Ignored               -> ""
      | Transpiled (node,env) -> transpileCode node env
      | Hardcoded code        -> code
      | RPC (className,
             methodName,
             returnType)      -> formatRPCTarget env className methodName returnType

