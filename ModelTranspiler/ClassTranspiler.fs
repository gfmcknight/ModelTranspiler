module ClassTranspiler

open System
open System.Reflection
open System.Linq.Expressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

open CodeTranspiler
open TypeUtils
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
        ; isOverride    : bool
        }

type ParamInfo =
        { name          : string
        ; declaredType  : string
        ; convertedType : string
        }

type MethodInfo =
        { parameters          : ParamInfo list
        ; name                : string
        ; transpileDirective  : Coded
        ; declaredReturnType  : string
        ; convertedReturnType : string
        }

type SubtypeInfo =
        { subtype : string
        ; value   : string
        }

type SubtypeFieldInfo =
        { discriminatingField : string
        ; subtypes            : SubtypeInfo list
        }


let getDeclaredType (typename: string) (attributes: SyntaxList<AttributeListSyntax>) =
    if (hasAttribute "TranspiledType" attributes)
            then removeQuotes (
                    (argFromAttribute 0
                        (getAttribute "TranspiledType" attributes)).ToString())
            else typename
(**************************************
  Reading features
***************************************)

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
    if (isNull accessor.Body)
    then (accessType, Simple)
    else raise (NotImplementedException("Not implemented: complex accessors"))

(*
 * Take a property declaration creates an internal Property,
 * which gives us easier access to the information we need to
 * walk the tree in order to discover.
 *)
let convertProperty (env: Env) (declaration: PropertyDeclarationSyntax) : Property * Dependencies =
    let declaredType = getDeclaredType (declaration.Type.ToString()) declaration.AttributeLists in
    let nullable = (defaultNullable env declaredType) &&
                   not (hasAttribute "JsonRequired" declaration.AttributeLists)
    let isOverride = declaration.Modifiers.ToString().Contains("override")
    let (convertedType, dependencies) =
            if nullable
            then convertType ("Nullable<" + declaredType + ">") env
            else convertType declaredType env in
    let declaredName = declaration.Identifier.ToString() in

    // If the user uses a [JsonProperty] attribute, we need to
    // remember the name because that's what we'll be using if
    // we try to unpack a JSON object that has been serialized
    let jsonName = (getAllAttributes declaration.AttributeLists)
                        |> Seq.tryFind (fun (attr: AttributeSyntax) -> (attr.Name.ToString()) = "JsonProperty")
                        |> function
                            | Some attribute ->
                                removeQuotes ((argFromAttribute 0 attribute).ToString())
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
    ; isOverride = isOverride
    },
    dependencies)

let collectProperties (env: Env) (declarations: seq<PropertyDeclarationSyntax>) =
    let filteredDeclarations = 
            (Seq.filter 
                (fun (d : PropertyDeclarationSyntax) -> not (hasAttribute "JsonIgnore" d.AttributeLists)) 
            declarations) in
     Seq.map (convertProperty env) filteredDeclarations

(*
 * Take a MethodDeclarationSyntax and gather all important facts
 * for transpiling into our internal representation
 *)

let convertMethod (env: Env) (className: string) (declaration: MethodDeclarationSyntax) : MethodInfo * Dependencies =
    let name = declaration.Identifier.ToString() in

    (*
     * Gathers the necessary information and dependency from a
     * parameter for transpilation.
     *)
    let convertParam (param : ParameterSyntax) : ParamInfo * Dependencies =
        let paramName = param.Identifier.ToString() in
        let declaredType = getDeclaredType (param.Type.ToString()) param.AttributeLists in
        let (convertedType, paramDependencies) = convertType declaredType env in
        (
            { name = paramName
            ; declaredType = declaredType
            ; convertedType = convertedType
            }
        , paramDependencies
        )
    in
    // Filter out all parameters with the [FromServices] attribute because
    // we will use dependency injection instead
    let filterFromServices (param: ParameterSyntax) : bool =
        not (hasAttribute "FromServices" param.AttributeLists)
    let (parameters, paramDependencies) =
        flipZipDirection (
            Seq.toList (
                Seq.map convertParam (
                    declaration.ParameterList.Parameters
                        |> (Seq.filter filterFromServices))))

    // Remove the Task<> from the C# type because we will never get one from
    // the server, and just care about the type sent from the server
    let declaredReturnType = getDeclaredType
                                (unwrapTasks (declaration.ReturnType.ToString()))
                                declaration.AttributeLists in
    let (convertedReturnType, returnDependency) = convertType declaredReturnType env in

    let (transpileDirective, transpileDependency) =
            getTranpileDirective declaration className declaredReturnType

    let dependencies = List.concat (returnDependency::transpileDependency::paramDependencies)

    (
    { parameters = parameters
    ; name = name
    ; transpileDirective = transpileDirective
    ; declaredReturnType = declaredReturnType
    ; convertedReturnType = convertedReturnType
    }
    , dependencies)

let collectMethods (env: Env) (declarations: seq<MethodDeclarationSyntax>) (className: string): seq<MethodInfo * Dependencies> =
    let filteredDeclarations =
        (Seq.filter
            (fun (d : MethodDeclarationSyntax) -> 
                match (getTranpileDirective d className "") with (Ignored, _) -> false | _ -> true)
        declarations) in
     Seq.map (convertMethod env className) filteredDeclarations

(*
 * Look at the attributes attached to the class in order to
 * determine what subtypes might be associated with it via
 * explicit JSONSubtype KnownSubtype annotations. The format
 * supported here is very strict here and must follow this
 * format exactly:
 *
 * [JsonConverter(typeof(JsonSubtypes), "SubTypeADiscriminatingField")]
 * [KnownSubType(typeof(SubASubTypeA), 1)]
 * [KnownSubType(typeof(SubASubTypeA), 2)]
 *)
let collectSubtypes (env: Env) (classDeclaration: ClassDeclarationSyntax) : (SubtypeFieldInfo option * Dependencies list)  =
    let attributeList = classDeclaration.AttributeLists
    if hasAttribute "JsonConverter" attributeList &&
        (getAttribute "JsonConverter" attributeList
            |> argFromAttribute 0
            |> fun (e: ExpressionSyntax) -> e.ToString()
            |> removeTypeOf
            |> fun (s: string) -> s.StartsWith("JsonSubtypes"))
    then
        let attributes =
            Seq.filter
                (fun (att: AttributeSyntax) -> att.Name.ToString() = "KnownSubType")
                (getAllAttributes attributeList)
        in
        let subtypes = Seq.map
                        (fun (att: AttributeSyntax) ->
                                { subtype = removeTypeOf ((argFromAttribute 0 att).ToString())
                                ; value = (argFromAttribute 1 att).ToString()
                                })
                        attributes
        in
        let discriminatingField =
            getAttribute "JsonConverter" attributeList
                |> argFromAttribute 1
                |> fun (e: ExpressionSyntax) -> e.ToString()
                |> removeQuotes
        in
        let dependencies = Seq.toList (Seq.map
                            (fun subtype ->
                                let _, dep, _ = tryGetModelFromEnv (subtype.subtype) env
                                in dep)
            subtypes)
        in
        (Some { discriminatingField = discriminatingField
             ; subtypes = Seq.toList subtypes
             }, dependencies)
    else (None, [])

(**************************************
  Code Generation
***************************************)

let createFieldList (properties : seq<Property>) : string =
    let createFieldString (property: Property) =
            sprintf "protected _%s : %s;\n" property.declaredName property.convertedType in
    // We don't want to output the fields that we are overriding, but we may
    // want to use some of the information (i.e. default values) for generating
    // other parts of the model
    let fieldStrings = Seq.map createFieldString
                        (Seq.filter (fun prop -> not prop.isOverride) properties) in
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
let createConstructor (env: Env) (properties: seq<Property>) (baseClassName: string option) =
    let superCall = match baseClassName with
                    | None -> ""
                    | Some _ -> "super(jsonData);\n"
    let propertySetters = Seq.map (fun (prop: Property) ->
                                        "if (jsonData." + prop.jsonName + ") {\n" +
                                        "this._" + prop.declaredName + " = " +
                                            (fromJSONObject env prop.declaredType ("jsonData." + prop.jsonName))
                                         + ";\n}\n") properties
    in "constructor (jsonData: any) {\n" + superCall + (Seq.fold (+) "" propertySetters) + "}\n"

let createInit (env: Env) (properties: seq<Property>) (baseClassName: string option) =
    let superCall = match baseClassName with
                            | None -> ""
                            | Some _ -> "super._init(jsonData);\n"
    let propertySetters = Seq.map (fun (prop: Property) ->
                                    "if (jsonData." + prop.jsonName + ") {\n" +
                                    "this._" + prop.declaredName + " = " +
                                        (fromJSONObject env prop.declaredType ("jsonData." + prop.jsonName))
                                     + ";\n}\n") properties
    in "_init (jsonData) {\n" + superCall + (Seq.fold (+) "" propertySetters) + "}\n"


let createToJSON (env: Env) (properties: seq<Property>) (baseClassName: string option) =
    let allSets = Seq.map (fun (prop: Property) -> 
                        prop.jsonName + ": " + (toJSONObject env prop.declaredType ("this._" + prop.declaredName)))
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

let createImports (genDirectory: string) (currentNS: string) (dependencies: Dependencies) : string =
    let currentDir = genDirectory + makeFilePath currentNS in
    let dedupedDependencies = List.distinctBy (fun (dep: string, _) -> dep) dependencies in
    let imports = List.map (fun (dep: string, fullpath: string) -> 
                            "import { " + dep + " } from '" + (relativePath currentDir (genDirectory + "internal")) + "';\n")
                            dedupedDependencies
    in List.fold (+) "" imports


let createMethod (env: Env) (info: MethodInfo) : string =
    let asyncModifier = match info.transpileDirective with RPC _ -> "async " | _ -> ""
    let returnType = match info.transpileDirective with
                        | RPC _ -> "Promise<" + info.convertedReturnType + ">"
                        | _ -> info.convertedReturnType
    asyncModifier + info.name + "(" +
    (commaSeparatedList (Seq.map (fun (param: ParamInfo) -> param.name + ": " + param.convertedType) info.parameters))
    + ") : " + returnType + " {"
    + (transpile env info.transpileDirective)
    + "}\n"

let createStaticDiscriminator (subtypes: SubtypeFieldInfo option)
        (className: string) (properties: Property list) : string =
    "static fromJSON(payload: any) : " + className + " {\n" +
    (match subtypes with
        | None -> "return new " + className + "(payload);\n"
        | Some subtypes ->
            let cases =
                List.map
                    (fun (subtype: SubtypeInfo) ->
                        "case " + subtype.value + ":\n" +
                        "return " + subtype.subtype + ".fromJSON(payload);\n")
                    subtypes.subtypes
            in
            let discriminator = Seq.find
                                    (fun (prop: Property) -> prop.declaredName = subtypes.discriminatingField)
                                    properties
            in
            "switch (payload." + discriminator.jsonName + ") {\n" +
            List.fold (+) "" cases +
            "default:\n" +
            "return new " + className + "(payload);\n" +
            "}\n") +
    "}\n"

(*
 * Reads a class declaration and transpiles it to a TypeScript
 * class.
 *)
let convertClass (env: Env) (genDirectory: string) (ns: string) (classDeclaration : ClassDeclarationSyntax) =
    let className = classDeclaration.Identifier.ToString()
    let baseClassInfo = match classDeclaration.BaseList with
                        | null -> None
                        | _    -> Some (tryGetModelFromEnv
                                            (classDeclaration.BaseList.GetFirstToken().GetNextToken().ToString())
                                            env)
    let (baseClassName: string option, baseClassDependencies : Dependencies) =
                                            match baseClassInfo with
                                               | None -> (None, [])
                                               | Some (name, dependencyList, _) -> ((Some name), dependencyList)

    let propertySyntax = Seq.cast<PropertyDeclarationSyntax> (
                            Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<PropertyDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (classDeclaration.DescendantNodes())))
    in
    let (properties, propertyDependeciesLists) =
        flipZipDirection (Seq.toList (collectProperties env propertySyntax)) in


    let methodSyntax = Seq.cast<MethodDeclarationSyntax> (
                            Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<MethodDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (classDeclaration.DescendantNodes())))
    let (methodInfos, methodDependencies) =
        flipZipDirection (Seq.toList (collectMethods env methodSyntax className))

    let (subtypeInfo, subtypeDependencies) =
        collectSubtypes env classDeclaration

    let dependencies = List.concat
                        (baseClassDependencies ::
                         propertyDependeciesLists @
                         methodDependencies @
                         subtypeDependencies)
    in

    let importList = createImports genDirectory ns dependencies
    let extendsClause = match baseClassName with
                        | None -> ""
                        | Some name -> " extends " + name
    let constructor = createConstructor env properties baseClassName in
    let fieldList = createFieldList properties in
    let accessors = createAccessors properties in
    let methods = (createInit env properties baseClassName) +
                  (createToJSON env properties baseClassName) +
                  (Seq.fold (+) "" (Seq.map (createMethod env) methodInfos)) +
                  (createStaticDiscriminator subtypeInfo className properties)
    in
    importList + PRELUDE +
        "export default class " + className + extendsClause
                                + " {\n" + FIELD_LIST_HEADER + fieldList
                                         + CONSTRUCTOR_HEADER + constructor
                                         + ACCESSORS_HEADER + accessors
                                         + METHODS_HEADER + methods
                                 + "}"
                                + POSTLUDE