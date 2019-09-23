module TypeUtils

open Microsoft.CodeAnalysis.CSharp.Syntax

(* 
 * Classes: namespace * class name * Tree
 *)
type Env =
   { classes : (string * string * ClassDeclarationSyntax) list }

type Dependencies = (string * string) list

(*
* Find an existing transpiled class and report
* the dependency to it.
*)
let tryGetModelFromEnv (typeName: string) (env: Env) : (string * Dependencies) =
  let candidates = 
          List.filter (fun (_, className, _) -> className = typeName) env.classes
  in 
  if (candidates.IsEmpty) then (typeName, []) else 
     let (ns, className, _) = candidates.Head in 
         (className, [ (className, (Util.makeFilePath ns) + "/" + className) ])

(*
* Get the Typescript name closes to a given type
* in C#.
*)
let convertType (csharpType: string) (env: Env) : string * Dependencies =
   match csharpType with
   | "double"   -> ("number", [])
   | "int"      -> ("number", [])
   | "bool"     -> ("boolean", [])
   | "DateTime" -> ("Date", [])
   | "Guid"     -> ("string", [])
   | _ -> tryGetModelFromEnv csharpType env

(*
* Converter to help grab a certain field from
* the JSON object which comes from the server,
* and transform it into the correct type.
*)
let fromJSONObject (csharpType: string) (jsonObjectAccessor: string) =
   match csharpType with
   | "DateTime" -> "new Date(" + jsonObjectAccessor + " + 'Z')"
   | "double"   -> jsonObjectAccessor
   | "int"      -> jsonObjectAccessor
   | "bool"     -> jsonObjectAccessor
   | "Guid"     -> jsonObjectAccessor
   | "string"   -> jsonObjectAccessor
   | "void"     -> "undefined"
   | _ -> "new " + csharpType + "(" + jsonObjectAccessor + ")"

(*
* Converter to help create a JSON payload to
* send to the server.
*)
let toJSONObject (csharpType: string) (fieldAccessor: string) =
   match csharpType with
   | "DateTime" -> fieldAccessor + ".toJSON()"
   | "double"   -> fieldAccessor
   | "int"      -> fieldAccessor
   | "bool"     -> fieldAccessor
   | "Guid"     -> fieldAccessor
   | "string"   -> fieldAccessor
   | _ -> fieldAccessor + ".toJSON()"