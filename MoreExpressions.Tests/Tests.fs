module Tests

open Swensen.Unquote
open NUnit.Framework


let ``return inside expression``() =
    let a =
        if return' 0 then
            1
        else
            2

    a

[<Test>]
let ``should return zero inside expression``() =
    ``return inside expression``() =! 0


let ``return one instead of two``() =
    return' 1

    2

[<Test>]
let ``should return one instead of two``() =
    ``return one instead of two``() =! 1


[<Test>]
let ``should replace return in statement``() =
    return' ()

    0 =! 1


let ``only even numbers``(i : int) =
    if i % 2 = 0 then
        Some i
    else
        None

let ``return None instead of value``() =
    let a = Option.try' (``only even numbers`` 1)
    let b = Option.try' (``only even numbers`` 2)

    Some (a + b)

let ``return Some (2 + 4)``() =
    let a = Option.try' (``only even numbers`` 2)
    let b = Option.try' (``only even numbers`` 4)

    Some (a + b)

[<Test>]
let ``should return None``() =
    ``return None instead of value``() =! None

[<Test>]
let ``should return Some 6``() =
    ``return Some (2 + 4)``() =! Some 6


let ``error if not even numbers``(i : int) =
    if i % 2 = 0 then
        Ok i
    else
        Error "Expected even number"

let ``return Error instead of value``() =
    let a = Result.try' (``error if not even numbers`` 1)
    let b = Result.try' (``error if not even numbers`` 2)

    Ok (a + b)

let ``return Ok (2 + 4)``() =
    let a = Result.try' (``error if not even numbers`` 2)
    let b = Result.try' (``error if not even numbers`` 4)

    Ok (a + b)

[<Test>]
let ``should return Error``() =
    ``return Error instead of value``() =! Error "Expected even number"

[<Test>]
let ``should return Ok 6``() =
    ``return Ok (2 + 4)``() =! Ok 6


let ``sum from 1 to n``(upto: int) =
    let label, jump = mklabel
    let mutable sum = 0

    for i = 1 to upto do
        jump 42

        sum <- sum + i

    label 0, sum

[<Test>]
let ``should return 42, 0``() =
    let label, sum = ``sum from 1 to n`` 10

    label =! 42
    sum =! 0

[<Test>]
let ``should return 0, 0``() =
    let label, sum = ``sum from 1 to n`` 0

    label =! 0
    sum =! 0
