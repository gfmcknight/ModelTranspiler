module ClassTranspiler

open System
open System.Reflection
open System.Linq.Expressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

open CodeTranspiler
open Util

type AccessType = Get | Set 
type PropertyAccess = Simple | Written of Coded

let PRELUDE = "\n/** BEGIN AUTO-GENERATED CODE **/\n\n"
let POSTLUDE = "\n\n/** END AUTO-GENERATED CODE **/\n"

let FIELD_LIST_HEADER = "\n/** AUTO-GENERATED FIELD LIST **/\n\n"
let CONSTRUCTOR_HEADER = "\n/** AUTO-GENERATED CONSTRUCTOR **/\n\n"
let ACCESSORS_HEADER = "\n/** AUTO-GENERATED GETTERS AND SETTERS **/\n\n"
let METHODS_HEADER = "\n/** AUTO-GENERATED METHODS **/\n\n"

type Property = 
        { get           : PropertyAccess option
        ; set           : PropertyAccess option
        ; jsonName      : string
        ; declaredName  : string
        ; declaredType  : string
        ; convertedType : string
        }

(**************************************
  Reading features
***************************************)

(*
 * Find an existing transpiled class and report
 * the dependency to it.
 *)
let tryGetModelFromEnv (typeName: string) (env: Env) : (string * (string * string) list) =
   let candidates = 
           List.filter (fun (_, className, _) -> className = typeName) env.classes
   in 
   if (candidates.IsEmpty) then (typeName, []) else 
      let (ns, className, _) = candidates.Head in 
          (className, [ (className, (makeFilePath ns) + "/" + className) ])

(*
 * Get the Typescript name closes to a given type
 * in C#.
 *)
let convertType (csharpType: string) (env: Env) : string * (string * string) list = 
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
    | "string"     -> jsonObjectAccessor
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

(*
 * Convert an accessor declaration into an internal representation
 * of accesses, namely, one that, in the future, will be responsive
 * accessor bodies and 
 *
 * The type (getter/setter) is not a part of the internal representation,
 * but will be needed later, so the return value is a tuple (type, accessor)
 * where the type distinguishes get/set and the accessor 
 *)
let convertAccessor (accessor: AccessorDeclarationSyntax) : (AccessType * PropertyAccess) = 
    let accessType : AccessType = match (accessor.Keyword.ToString()) with
                                         | "get" -> Get
                                         | "set" -> Set
                                         | x -> raise (Exception("Invalid accessType keyword " + x))
    in
    if (accessor.Body = null)
    then (accessType, Simple) 
    else raise (NotImplementedException("Not implemented: complex accessors"))

(*
 * Take a property declaration creates an internal Property,
 * which gives us easier access to the information we need to
 * walk the tree in order to discover.
 *)
let convertProperty (env: Env) (declaration: PropertyDeclarationSyntax) : Property * (string * string) list =
    let declaredType = declaration.Type.ToString() in
    let (convertedType, dependencies) = convertType declaredType env in
    let declaredName = declaration.Identifier.ToString() in

    // If the user uses a [JsonProperty] attribute, we need to
    // remember the name because that's what we'll be using if
    // we try to unpack a JSON object that has been serialized
    let jsonName = (getAllAttributes declaration.AttributeLists)
                        |> Seq.tryFind (fun (attr: AttributeSyntax) -> (attr.Name.ToString()) = "JsonProperty")
                        |> function
                            | Some attribute ->
                                removeQuotes (attribute.ArgumentList.Arguments.First().Expression.ToString())
                            | None -> declaredName
    in

    // Get all of the accessor declarations which are not private
    // by exploring all children of the property node. 
    let accessors = Seq.filter (fun (a: AccessorDeclarationSyntax) -> not (a.Modifiers.ToString().Contains("private")))
                        (Seq.cast<AccessorDeclarationSyntax> 
                            (Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<AccessorDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (declaration.DescendantNodes()))))
    in
    let convertedAccessors = Seq.map convertAccessor accessors  in

    // First grab all of the accessors that are of the wrong type
    // (ie. setter when looking for a header) and then discard the
    // accessor type information -- we're about to place the
    // getter/setter into our property so we no longer need this
    // information
    let get = Seq.tryHead (convertedAccessors
                           |> Seq.filter (fun (accessorType, _ : PropertyAccess) -> accessorType = Get)
                           |> Seq.map (fun (_, accessor) -> accessor))
    in
    
    let set = Seq.tryHead (convertedAccessors 
                           |> Seq.filter (fun (accessorType, _ : PropertyAccess) -> accessorType = Set)
                           |> Seq.map (fun (_, accessor) -> accessor)) 
    in
    
    (
    { get = get
    ; set = set
    ; jsonName = jsonName
    ; declaredName = declaredName
    ; declaredType = declaredType
    ; convertedType = convertedType 
    },
    dependencies)

let collectProperties (env: Env) (declarations: seq<PropertyDeclarationSyntax>) =
    let filteredDeclarations = 
            (Seq.filter 
                (fun (d : PropertyDeclarationSyntax) -> not (hasAttribute "JsonIgnore" d.AttributeLists)) 
            declarations) in
     Seq.map (convertProperty env) filteredDeclarations

(**************************************
  Code Generation
***************************************)

let createFieldList (properties : seq<Property>) : string =
    let createFieldString (property: Property) = 
            sprintf "private _%s : %s;\n" property.declaredName property.convertedType in
    let fieldStrings = Seq.map createFieldString properties in
    Seq.fold (+) "" fieldStrings

let createAccessors (properties : seq<Property>) =
    let createAccessorsString (property: Property) =
        let getString = match property.get with
                        | None -> ""
                        | Some get -> 
                            match get with
                            | Simple -> sprintf "get %s() : %s\n{\nreturn this._%s;\n}\n" 
                                            property.declaredName property.convertedType property.declaredName
                            | _ -> raise (NotImplementedException("Not implemented: coded accessor"))
        in
        let setString = match property.set with
                        | None -> ""
                        | Some get -> 
                            match get with
                            | Simple -> sprintf "set %s(new%s : %s)\n{\nthis._%s = new%s;\n}\n"
                                            property.declaredName property.declaredName property.convertedType
                                            property.declaredName property.declaredName
                            | _ -> raise (NotImplementedException("Not implemented: coded accessor"))
        in
        getString + setString
    in
    Seq.fold (+) "" (Seq.map createAccessorsString properties)

(*
 * Generates a constructor that should hydrate a JSON object
 * that has been serialized into a model class at the TypeScript
 * level. Since this assumes the object was serialized, we must
 * use the JSON name for each property.
 *)
let createConstructor (properties: seq<Property>) (baseClassName: string option) =
    let superCall = match baseClassName with
                    | None -> ""
                    | Some _ -> "super(jsonData);\n"
    let propertySetters = Seq.map (fun (prop: Property) ->
                                        "if (jsonData." + prop.jsonName + ") {\n" +
                                        "this._" + prop.declaredName + " = " +
                                            (fromJSONObject prop.declaredType ("jsonData." + prop.jsonName)) 
                                         + ";\n}\n") properties
    in "constructor (jsonData) {\n" + superCall + (Seq.fold (+) "" propertySetters) + "}\n"

let createToJSON (properties: seq<Property>) (baseClassName: string option) = 
    let allSets = Seq.map (fun (prop: Property) -> 
                        prop.jsonName + ": " + (toJSONObject prop.declaredType ("this._" + prop.declaredName)))
                            properties in
    let thisClassJson = 
        if (Seq.isEmpty allSets) then "{}"
        else
            let firstSet = Seq.head allSets in
            let remainingSets = Seq.map (fun x -> ",\n" + x) (Seq.tail allSets)
            in "{\n" + (Seq.fold (+) firstSet remainingSets) + "\n}"
    in
    let body = match baseClassName with
                | None -> "return " + thisClassJson + ";\n"
                | Some _ -> "var baseClassJSON = super.toJSON();\n" +
                            "return Object.assign(baseClassJSON , " + thisClassJson + ");\n"
    in
    "toJSON() {\n" + body + "}\n"

let createImports (genDirectory: string) (currentNS: string) (dependencies: (string * string) list) : string =
    let currentDir = genDirectory + makeFilePath currentNS in
    let imports = List.map (fun (dep: string, fullpath: string) -> 
                            "import " + dep + " from '" + (relativePath currentDir (genDirectory + fullpath)) + "';\n") dependencies
    in List.fold (+) "" imports

(*
 * Reads a class declaration and transpiles it to a TypeScript
 * class.
 *)
let convertClass (env: Env) (genDirectory: string) (ns: string) (classDeclaration : ClassDeclarationSyntax) = 
    let baseClassInfo = match classDeclaration.BaseList with 
                        | null -> None 
                        | _    -> Some (tryGetModelFromEnv (classDeclaration.BaseList.GetFirstToken().GetNextToken().ToString()) env)
    let (baseClassName: string option, baseClassDependencies : (string * string) list ) = 
                                            match baseClassInfo with 
                                               | None -> (None, []) 
                                               | Some (name, dependencyList) -> ((Some name), dependencyList)

    let propertySyntax = Seq.cast<PropertyDeclarationSyntax> (
                            Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<PropertyDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (classDeclaration.DescendantNodes())))
    in
    let (properties, dependeciesLists) = 
        Seq.fold (fun (s1, s2) (v1, v2) -> ([v1]@s1, [v2]@s2)) ([], []) (collectProperties env propertySyntax)
    let dependencies = List.concat (baseClassDependencies::dependeciesLists) in

    let importList = createImports genDirectory ns dependencies
    let extendsClause = match baseClassName with
                        | None -> ""
                        | Some name -> " extends " + name
    let constructor = createConstructor properties baseClassName in
    let fieldList = createFieldList properties in
    let accessors = createAccessors properties in
    let methods = createToJSON properties baseClassName in
    importList + PRELUDE + 
        "export default class " + classDeclaration.Identifier.ToString() + extendsClause
                                + " {\n" + FIELD_LIST_HEADER + fieldList 
                                         + CONSTRUCTOR_HEADER + constructor 
                                         + ACCESSORS_HEADER + accessors 
                                         + METHODS_HEADER + methods + "}"
                                + POSTLUDE