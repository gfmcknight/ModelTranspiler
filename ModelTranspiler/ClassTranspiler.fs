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
    
let convertType (csharpType: string) = 
    match csharpType with
    | "double" -> "number"
    | "int"    -> "number"
    | _ -> csharpType

let convertProperty (declaration: PropertyDeclarationSyntax) : Property =
    let declaredType = declaration.Type.ToString() in
    let convertedType = convertType declaredType in
    let declaredName = declaration.Identifier.ToString() in
    let jsonName = (getAllAttributes declaration.AttributeLists) 
                        |> Seq.tryFind (fun (attr: AttributeSyntax) -> (attr.Name.ToString()) = "JsonProperty")
                        |> function
                            | Some attribute -> 
                                (attribute.ArgumentList.Arguments.First().Expression).ToString().Replace("\"", "")
                            | None -> declaredName
    in

    let accessors = Seq.filter (fun (a: AccessorDeclarationSyntax) -> not (a.Modifiers.ToString().Contains("private")))
                        (Seq.cast<AccessorDeclarationSyntax> 
                            (Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<AccessorDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (declaration.DescendantNodes()))))
    in
    let convertAccessor (accessor: AccessorDeclarationSyntax) : (AccessType * PropertyAccess) = 
        let accessType : AccessType = match (accessor.Keyword.ToString()) with
                                             | "get" -> Get
                                             | "set" -> Set
                                             | x -> raise (Exception("Invalid accessType keyword " + x))
        in
        if (accessor.Body = null)
        then (accessType, Simple) 
        else raise (NotImplementedException("Not implemented: complex accessors"))
    in
    let convertedAccessors = Seq.map convertAccessor accessors  in

    let get = Seq.tryHead (convertedAccessors
                           |> Seq.filter (fun (accessorType, _ : PropertyAccess) -> accessorType = Get)
                           |> Seq.map (fun (_, accessor) -> accessor)) in
    
    let set = Seq.tryHead (convertedAccessors 
                           |> Seq.filter (fun (accessorType, _ : PropertyAccess) -> accessorType = Set)
                           |> Seq.map (fun (_, accessor) -> accessor)) in
    
    { get = get
    ; set = set
    ; jsonName = jsonName
    ; declaredName = declaredName
    ; declaredType = declaredType
    ; convertedType = convertedType 
    }

let collectProperties (declarations: seq<PropertyDeclarationSyntax>) =
    let filteredDeclarations = 
            (Seq.filter 
                (fun (d : PropertyDeclarationSyntax) -> not (hasAttribute "JsonIgnore" d.AttributeLists)) 
            declarations) in
     Seq.map convertProperty filteredDeclarations

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

let createConstructor (properties: seq<Property>) =
    let propertySetters = Seq.map (fun (prop: Property) ->
                                        "if (jsonData." + prop.jsonName + ") {\n" +
                                        "this._" + prop.declaredName + " = jsonData." + prop.jsonName + ";\n}\n") properties
    in "constructor (jsonData) {\n" + (Seq.fold (+) "" propertySetters) + "}\n"

let convertClass (classDeclaration : ClassDeclarationSyntax) = 
    let propertySyntax = Seq.cast<PropertyDeclarationSyntax> (
                            Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<PropertyDeclarationSyntax>, i))
                                (Seq.cast<SyntaxNode> (classDeclaration.DescendantNodes())))
    in
    let properties = collectProperties propertySyntax

    let constructor = createConstructor properties in
    let fieldList = createFieldList properties in
    let accessors = createAccessors properties in
    PRELUDE + 
        "export default class " + classDeclaration.Identifier.ToString() 
                                + " {\n" + FIELD_LIST_HEADER + fieldList 
                                         + CONSTRUCTOR_HEADER + constructor 
                                         + ACCESSORS_HEADER + accessors + "}"
                                + POSTLUDE
