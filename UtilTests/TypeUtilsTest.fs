module TypeUtilsTest

open System
open Xunit

open TypeUtils

[<Fact>]
let ``clipGenericType no-generic-type`` () =
    Assert.Equal((None, "int"), (clipGenericType "int"))

[<Fact>]
let ``clipGenericType task-of-int`` () =
    Assert.Equal((Some "Task", "int"), (clipGenericType "Task<int>"))

[<Fact>]
let ``unwrapTask no-task`` () =
    Assert.Equal("int", (unwrapTasks "int"))

[<Fact>]
let ``unwrapTask void-task`` () =
    Assert.Equal("void", (unwrapTasks "Task"))

[<Fact>]
let ``unwrapTask task-with-type`` () =
    Assert.Equal("int", (unwrapTasks "Task<int>"))