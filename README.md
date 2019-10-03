MoreExpressions.Fody
====================

[Fody](https://github.com/Fody/Home) weaver that adds new expressions
to .NET through method calls.

This is mainly developed with F# in mind, and currently doesn't really
work. The tests pass though, which probably means something.


## `return` expression

Transforms any call to `MoreExpressions.Return` to a return expression.

```fsharp
let ``return inside expression``() =
    let a =
        if return' 0 then
            1
        else
            2

    a

let ``return zero instead of one``() =
    return' 0

    1
```

Both of the above functions return `0`. In C#, you could write the following:

```csharp
int ReturnInsideExpression()
{
    int a = 2;

    if (MoreExpressions.Return(0))
        a = 1;

    return a;
}

int ReturnZeroInsteadOfOne()
{
    MoreExpressions.Return(0);

    return 1;
}
```

Using `MoreExpressions.Return`, more powerful functions can be implemented;
for instance, the Rust `try!` macro is trivial to add to F#:

```fsharp
let inline try' option =
    match option with
    | Some value -> value
    | None -> return' None
```

This can be used this way:

```fsharp
let ``only even numbers``(i : int) =
    if i % 2 = 0 then
        Some i
    else
        None

let x =
    let a = try' (``only even numbers`` 1)
    let b = try' (``only even numbers`` 2)

    Some (a + b)

let y =
    let a = try' (``only even numbers`` 2)
    let b = try' (``only even numbers`` 4)

    Some (a + b)
```

Obviously, `x` will be `None` (because `1` is not even) and `y`
will be `Some 6` (because both `2` and `4` are even).
