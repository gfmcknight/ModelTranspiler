module TypeUtils

open Microsoft.CodeAnalysis.CSharp.Syntax
open System.Linq

type ClassInfo =
      { ns       : string
      ; name     : string
      ; tree     : ClassDeclarationSyntax
      ; baseType : string option
      }

type EnumInfo =
      { ns   : string
      ; name : string
      ; tree : EnumDeclarationSyntax
      }

(*
 * Classes: namespace * class name * Tree
 *)
type Env =
   { classes : ClassInfo list
   ; enums   : EnumInfo list
   }

type Dependencies = (string * string) list

(*
 * Find an existing transpiled class and report
 * the dependency to it.
 * Return format: name of class, list of dependencies for it, isClass
 *)
let tryGetModelFromEnv (typeName: string) (env: Env) : (string * Dependencies * bool) =
  let candidates =
          List.filter (fun (classInfo : ClassInfo) -> classInfo.name = typeName) env.classes
  in
  if (candidates.IsEmpty)

  then let enumCandidates =
               List.filter (fun (enumInfo : EnumInfo) -> enumInfo.name = typeName) env.enums
       in (
          if (enumCandidates.IsEmpty)
          then (typeName, [], true)
          else let head = enumCandidates.Head in
               (head.name, [ (head.name, (Util.makeFilePath head.ns) + "/" + head.name) ], false)
       )

  else
     let head = candidates.Head in
         (head.name, [ (head.name, (Util.makeFilePath head.ns) + "/" + head.name) ], true)

(* 
 * For a given "Type<T>", returns a tuple containing (Some "Type", "T")
 * If there is no generic
 *)
let clipGenericType (typeName : string) :  string option * string =
   if typeName.EndsWith("?")
   then (Some "Nullable", typeName.Substring(0, typeName.Length - 1))
   else
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
   | (None, "long")     -> ("number", [])
   | (None, "bool")     -> ("boolean", [])
   | (None, "DateTime") -> ("Date", [])
   | (None, "Guid")     -> ("string", [])
   | (None, "JObject")  -> ("object", [])

   | (Some "List", t)   -> 
        let (innerConvert, innerDeps) = (convertType t env) in
        ("Array<" + innerConvert + ">", innerDeps)

   | (Some "Dictionary", t) ->
        let mapTypes = List.map (fun (s: string) -> s.Trim()) 
                                    (t.Split(',') |> Array.toList) in
        let (keyType, keyDeps) = (convertType (mapTypes.Item(0)) env) in
        let (valueType, valueDeps) = (convertType (mapTypes.Item(1)) env) in
        ("Record<" + keyType + ", " + valueType + ">", keyDeps @ valueDeps)

   | (Some "Nullable", t) -> let (inner, deps) = convertType t env in
                              (inner + " | undefined", deps)

   | _ -> let (name, deps, _) = tryGetModelFromEnv csharpType env in (name, deps)

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
let rec fromJSONObject (env: Env) (csharpType: string) (jsonObjectAccessor: string) =
   match (clipGenericType csharpType) with
   | (None, "DateTime") -> "new Date(" + jsonObjectAccessor + ")"
   | (None, "double")   -> jsonObjectAccessor
   | (None, "int")      -> jsonObjectAccessor
   | (None, "long")     -> jsonObjectAccessor
   | (None, "bool")     -> jsonObjectAccessor
   | (None, "Guid")     -> jsonObjectAccessor
   | (None, "string")   -> jsonObjectAccessor
   | (None, "JObject")  -> jsonObjectAccessor
   | (None, "void")     -> "undefined"

   | (Some "List", t)   -> jsonObjectAccessor + ".map((t: any) => " + (fromJSONObject env t "t") + ")"
   | (Some "Dictionary", t) -> translateBetweenMapTypes t jsonObjectAccessor (fromJSONObject env)
   | (Some "Nullable", t) -> jsonObjectAccessor + " !== undefined ? " +
                              (fromJSONObject env t jsonObjectAccessor) + " : undefined"

   | _ -> let (_, _, isClass) = tryGetModelFromEnv csharpType env
          in match isClass with
               | true -> "new " + csharpType + "(" + jsonObjectAccessor + ")"
               | false -> jsonObjectAccessor



(*
 * Converter to help create a JSON payload to
 * send to the server.
 *)
let rec toJSONObject (env: Env) (csharpType: string) (fieldAccessor: string) =
   match (clipGenericType csharpType) with
   | (None, "DateTime") -> fieldAccessor + ".toJSON()"
   | (None, "double")   -> fieldAccessor
   | (None, "int")      -> fieldAccessor
   | (None, "long")     -> fieldAccessor
   | (None, "bool")     -> fieldAccessor
   | (None, "Guid")     -> fieldAccessor
   | (None, "string")   -> fieldAccessor
   | (None, "JObject")  -> fieldAccessor

   | (Some "List", t)   -> fieldAccessor + ".map(t => " + (toJSONObject env t "t") + ")"
   | (Some "Dictionary", t) -> translateBetweenMapTypes t fieldAccessor (toJSONObject env)
   | (Some "Nullable", t) -> fieldAccessor + " !== undefined ? " +
                              (toJSONObject env t fieldAccessor) + " : undefined"

   | _ -> let (_, _, isClass) = tryGetModelFromEnv csharpType env
          in match isClass with
               | true -> fieldAccessor + ".toJSON()"
               | false -> fieldAccessor

let defaultNullable (env: Env) (csharpType: string) : bool=
   match (clipGenericType csharpType) with
   | (None, "DateTime") -> true
   | (None, "double")   -> false
   | (None, "int")      -> false
   | (None, "long")     -> false
   | (None, "bool")     -> false
   | (None, "Guid")     -> false
   | (None, "string")   -> true
   | (None, "JObject")  -> true

   | (Some "List", t)   -> true
   | (Some "Dictionary", t) -> true
   | (Some "Nullable", t) -> false // already nullable

   | _ -> let (_, _, isClass) = tryGetModelFromEnv csharpType env
          in match isClass with
               | true -> true
               | false -> false