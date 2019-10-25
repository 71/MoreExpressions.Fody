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


## `label`, `jump` expressions

The `mklabel` function returns a pair of functions that can be used
to implement jumps in your F# code.

```fsharp
let label, jump = mklabel

for i = 0 to n - 1 do
    let isOver = complicatedComputation i n

    if isOver then
        jump true

let endedEarly = label false
```

The first function, `label`, takes a default value and either returns this value
if the `label` expression is reached naturally, or the expression passed to `jump`
otherwise.

In the example above, if `complicatedComputation i n` returns `true`, then
`jump true` will be called which will immediately jump to the `endedEarly` assignment;
here, `label false` will return `true`.

If instead `complicatedComputation i n` never returned `true`, then `label false` would
return its default value; that is, `false`.

Do watch out, though: anything that happens between `jump` and `label` will not be invoked
in case of a jump, which is extremely unsafe (it will break your stack and break all
variables that should have been in the meantime). Therefore this structure should
only be used in very specific circumstances.


## Gotchas

Since IL code is modified, so things that rely on local variables like F# quotations
may not work in functions that use one of the above expressions.
