namespace global

open System

[<AutoOpen>]
module MoreExpressions =
    [<CompiledName("Return")>]
    let return' value =
        ignore <| value
        raise <| NotImplementedException()

    [<CompiledName("MakeLabel")>]
    let mklabel<'t> : ('t -> 't) * ('t -> unit) =
        raise <| NotImplementedException()

module Option =
    [<CompiledName("Try")>]
    let inline try' option =
        match option with
        | Some value -> value
        | None -> return' None
