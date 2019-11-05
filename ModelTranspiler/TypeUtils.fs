module TypeUtils

open Microsoft.CodeAnalysis.CSharp.Syntax
open System.Linq

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
 * For a given "Type<T>", returns a tuple containing (Some "Type", "T")
 * If there is no generic 
 *)
let clipGenericType (typeName : string) :  string option * string =
  let leftAnglePosition = typeName.IndexOf("<") in
  if leftAnglePosition = -1 then (None, typeName)
  else 
      (Some (typeName.Substring(0, leftAnglePosition)),
       typeName.Substring(leftAnglePosition + 1, typeName.Length - leftAnglePosition - 2))

(*
 * If we have an asynchronous method, then the return type will be wrapped in
 * a Task<>, which will be undesirable for our transpiler.
 *
 * As a result we will need to convert a return type of Task<T> into just T
 * before we know what type we're returning for RPC methods.
 *)
let unwrapTasks (csharpType: string) : string =
    match clipGenericType csharpType with
    | (Some "Task", t) -> t
    | (None, "Task") -> "void"
    | _ -> csharpType

(*
 * Get the Typescript name closes to a given type
 * in C#.
 *)
let rec convertType (csharpType: string) (env: Env) : string * Dependencies =
   match (clipGenericType csharpType) with
   | (None, "double")   -> ("number", [])
   | (None, "int")      -> ("number", [])
   | (None, "bool")     -> ("boolean", [])
   | (None, "DateTime") -> ("Date", [])
   | (None, "Guid")     -> ("string", [])
   | (None, "JObject")   -> ("object", [])

   | (Some "List", t)   -> 
        let (innerConvert, innerDeps) = (convertType t env) in
        ("Array<" + innerConvert + ">", innerDeps)

   | (Some "Dictionary", t) ->
        let mapTypes = List.map (fun (s: string) -> s.Trim()) 
                                    (t.Split(',') |> Array.toList) in
        let (keyType, keyDeps) = (convertType (mapTypes.Item(0)) env) in
        let (valueType, valueDeps) = (convertType (mapTypes.Item(1)) env) in
        ("Record<" + keyType + ", " + valueType + ">", keyDeps @ valueDeps)

   | _ -> tryGetModelFromEnv csharpType env

(*
 * Helper to the below two conversions that applies a
 * conversion along all keys and values of an object
 *
 * elementConverter's intended args are the following:
 * type -> accessor -> resulting expression
 *)
let translateBetweenMapTypes (typePairExpression: string) (accessor: string)
       (elementConverter: string -> string -> string) =
       let mapTypes = List.map (fun (s: string) -> s.Trim()) 
                                   (typePairExpression.Split(',') |> Array.toList) 
       in
       accessor + " ? [{}].concat(Object.entries(" + accessor + 
           ").map(kv => [" + 
               (elementConverter (mapTypes.Item(0)) "kv[0]") + ", " +
               (elementConverter (mapTypes.Item(1)) "kv[1]") + 
               "])).reduce((acc, kv) => { acc[kv[0]] = kv[1]; return acc; }) : null" 

(*
 * Converter to help grab a certain field from
 * the JSON object which comes from the server,
 * and transform it into the correct type.
 *)
let rec fromJSONObject (csharpType: string) (jsonObjectAccessor: string) =
   match (clipGenericType csharpType) with
   | (None, "DateTime") -> "new Date(" + jsonObjectAccessor + " + 'Z')"
   | (None, "double")   -> jsonObjectAccessor
   | (None, "int")      -> jsonObjectAccessor
   | (None, "bool")     -> jsonObjectAccessor
   | (None, "Guid")     -> jsonObjectAccessor
   | (None, "string")   -> jsonObjectAccessor
   | (None, "JObject")   -> jsonObjectAccessor
   | (None, "void")     -> "undefined"

   | (Some "List", t)   -> jsonObjectAccessor + ".map(t => " + (fromJSONObject t "t") + ")"
   | (Some "Dictionary", t) -> translateBetweenMapTypes t jsonObjectAccessor fromJSONObject

   | _ -> "new " + csharpType + "(" + jsonObjectAccessor + ")"

(*
 * Converter to help create a JSON payload to
 * send to the server.
 *)
let rec toJSONObject (csharpType: string) (fieldAccessor: string) =
   match (clipGenericType csharpType) with
   | (None, "DateTime") -> fieldAccessor + ".toJSON()"
   | (None, "double")   -> fieldAccessor
   | (None, "int")      -> fieldAccessor
   | (None, "bool")     -> fieldAccessor
   | (None, "Guid")     -> fieldAccessor
   | (None, "string")   -> fieldAccessor
   | (None, "JObject")  -> fieldAccessor

   | (Some "List", t)   -> fieldAccessor + ".map(t => " + (toJSONObject t "t") + ")"
   | (Some "Dictionary", t) -> translateBetweenMapTypes t fieldAccessor toJSONObject

   | _ -> fieldAccessor + ".toJSON()"