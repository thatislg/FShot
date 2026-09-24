module FShot.UI.Tests.SingleInstanceTests

open System
open System.Threading
open FShot.Platform.Win32.Lifecycle
open Xunit

[<Fact>]
let ``IpcMessage serializes and deserializes round-trip``() =
    let args = [| "gui"; "-d"; "3000" |]
    let msg = IpcMessage.Create args
    let json = SingleInstance.serializeMessage msg
    Assert.False(String.IsNullOrWhiteSpace json)

    match SingleInstance.deserializeMessage json with
    | Some decoded ->
        Assert.Equal<string[]>(args, decoded.Arguments)
        Assert.True(decoded.Timestamp > DateTime.MinValue)
    | None -> Assert.Fail("Expected Some decoded message")

[<Fact>]
let ``deserializeMessage returns None for invalid JSON``() =
    let result = SingleInstance.deserializeMessage "not json"
    Assert.Equal(None, result)

[<Fact>]
let ``SingleInstance result cases are distinct``() =
    let results = [
        SecondaryInstance
        MultipleAllowed
        FirstInstance (new Mutex())
    ]
    Assert.Equal(3, results |> List.distinct |> List.length)

[<Fact>]
let ``enforce with allowMultiple returns MultipleAllowed without IPC``() =
    use cts = new CancellationTokenSource()
    let mutable receivedArgs: string[] option = None
    let onCommand (args: string[]) = receivedArgs <- Some args
    let result = SingleInstance.enforce [| "gui" |] true cts.Token onCommand
    Assert.Equal(MultipleAllowed, result)
    Assert.Equal(None, receivedArgs)
