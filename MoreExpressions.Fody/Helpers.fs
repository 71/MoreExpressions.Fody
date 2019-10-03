[<AutoOpen>]
module internal Helpers

open Mono.Cecil
open Mono.Collections.Generic

type TypeReference with
    member this.IsUnit = this.FullName = "Microsoft.FSharp.Core.Unit"
    member this.IsVoid = this.FullName = "System.Void"

type Collection<'T> with
    /// Returns an enumerator that allows suppression of the current item
    /// while enumeration.
    member this.GetMutableEnumerator() = seq {
        let mutable i = 0

        while i < this.Count do
            let j = this.Count

            yield i, this.[i]

            i <- i + 1 + (this.Count - j)
    }

    /// Replaces the specified (inclusive) range by the given items.
    member this.ReplaceRange(from: int, upto: int, items: 'T seq) =
        for i = 0 to from - upto do
            this.RemoveAt(i)

        for i, item in Seq.indexed items do
            this.Insert(from + i, item)
