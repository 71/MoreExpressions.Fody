namespace global

open System

[<AutoOpen>]
module MoreExpressions =
    /// Returns early with the given value from the calling function.
    [<CompiledName("Return")>]
    let return' value =
        ignore <| value
        raise <| NotImplementedException()

    /// Returns a pair of `label`, `jump` functions that can be used to implement
    /// `goto`s in F#.
    [<CompiledName("MakeLabel")>]
    let mklabel<'t> : ('t -> 't) * ('t -> unit) =
        raise <| NotImplementedException()

module Option =
    /// Returns the value of the given `option` if it is `Some`,
    /// or returns early with `None` from the calling function.
    [<CompiledName("Try")>]
    let inline try' option =
        match option with
        | Some value -> value
        | None -> return' None

module Result =
    /// Returns the value of the given `result` if it is `Ok`,
    /// or returns early with `Error` from the calling function.
    [<CompiledName("Try")>]
    let inline try' (result : Result<'a, 'b>) =
        match result with
        | Ok value -> value
        | Error err -> return' (Result<'a, 'b>.Error err)
