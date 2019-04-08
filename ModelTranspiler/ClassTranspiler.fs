module ClassTranspiler

open System
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open System.Reflection
open System.Linq.Expressions

let convertType csharpType =
    csharpType

let convertProperty (propertyDeclaration: PropertyDeclarationSyntax) = 
    let propertyType = convertType (propertyDeclaration.Type.ToString()) in
    let propertyName = propertyDeclaration.Identifier.ToString() in
    let fieldDecl = sprintf "private _%s : %s;\n" propertyName propertyType in

    let accessors = Seq.cast<AccessorDeclarationSyntax> 
                        (Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<AccessorDeclarationSyntax>, i))
                           (Seq.cast<SyntaxNode> (propertyDeclaration.DescendantNodes())))


    let convertAccessor (accessor: AccessorDeclarationSyntax) =
        match (accessor.ToString()) with
            | "get;" -> sprintf "get %s() : %s\n{\nreturn this._%s;\n}\n" propertyName propertyType propertyName
            | "set;" -> sprintf "set %s(new%s : %s)\n{\nthis._%s = new%s;\n}\n"
                                propertyName propertyName propertyType propertyName propertyName
            | _ -> ""
    in
    Seq.fold (+) fieldDecl (Seq.map convertAccessor accessors)
   

    

let convertClass (classDeclaration : ClassDeclarationSyntax) = 
    let properties = Seq.cast<PropertyDeclarationSyntax> (
                         Seq.filter (fun i -> TypeExtensions.IsInstanceOfType(typeof<PropertyDeclarationSyntax>, i))
                             (Seq.cast<SyntaxNode> (classDeclaration.DescendantNodes())))
    in
    let convertedProperties = Seq.fold (+) "" (Seq.map (fun prop -> (convertProperty prop) + "\n") properties)
    in
    "class " + classDeclaration.Identifier.ToString() + " {\n" + convertedProperties + "}"