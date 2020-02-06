module EnumTranspiler

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

open TypeUtils
open Util

let convertEnumMember (string: bool) (mem: EnumMemberDeclarationSyntax) : string =
    if string
    then (mem.Identifier.ToString() + " = \"" + mem.Identifier.ToString() + "\"")
    else mem.ToString()


let convertEnum (env: Env) (enumDeclaration: EnumDeclarationSyntax) =
    let enumName = enumDeclaration.Identifier.ToString()
    in
    let stringBacked =
        hasAttribute "JsonConverter" enumDeclaration.AttributeLists &&
            (argFromAttribute 0 (
                getAttribute "JsonConverter" enumDeclaration.AttributeLists)).ToString()
                            = "typeof(StringEnumConverter)"
    in
    let members = getChildrenOfType<EnumMemberDeclarationSyntax> enumDeclaration in
    "enum " + enumName + " {\n" +
        commaSeparatedLineList (Seq.map (convertEnumMember stringBacked) members) +
        "\n}\n" +
        "export default " + enumName + ";"
