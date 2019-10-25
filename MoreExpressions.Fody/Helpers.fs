[<AutoOpen>]
module internal Helpers

open Mono.Cecil
open Mono.Cecil.Cil
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

type MethodBody with
    member this.GetCorrespondingVariable(ins: Instruction) =
        match ins.OpCode.Code with
        | Code.Stloc_0 -> this.Variables.[0]
        | Code.Stloc_1 -> this.Variables.[1]
        | Code.Stloc_2 -> this.Variables.[2]
        | Code.Stloc_3 -> this.Variables.[3]
        | Code.Stloc_S -> this.Variables.[ins.Operand :?> int]
        | Code.Stloc   -> this.Variables.[ins.Operand :?> int]
        | _ -> failwith "Expected instruction to be a stloc instruction."

type StackBehaviour with
    member this.AddedToStack =
        match this with
        | StackBehaviour.Pop0
        | StackBehaviour.Push0 ->
            0
        | StackBehaviour.Push1
        | StackBehaviour.Pushi
        | StackBehaviour.Pushi8
        | StackBehaviour.Pushr4
        | StackBehaviour.Pushr8
        | StackBehaviour.Pushref ->
            1
        | StackBehaviour.Push1_push1 ->
            2
        | StackBehaviour.Popi
        | StackBehaviour.Pop1
        | StackBehaviour.Popref ->
            -1
        | StackBehaviour.Popi_pop1
        | StackBehaviour.Popi_popi
        | StackBehaviour.Popi_popi8
        | StackBehaviour.Popi_popr4
        | StackBehaviour.Popi_popr8
        | StackBehaviour.Popref_pop1
        | StackBehaviour.Pop1_pop1
        | StackBehaviour.Popref_popi ->
            -2
        | StackBehaviour.Popi_popi_popi
        | StackBehaviour.Popref_popi_popi
        | StackBehaviour.Popref_popi_popi8
        | StackBehaviour.Popref_popi_popr4
        | StackBehaviour.Popref_popi_popr8
        | StackBehaviour.Popref_popi_popref ->
            -3
        | _ ->
            invalidOp "Unexpected stack behaviour."

type Instruction with
    member this.Nop() =
        this.OpCode <- OpCodes.Nop
        this.Operand <- null

    member private this.AddedToStack =
        match this.OpCode.Code with
        | Code.Call | Code.Calli | Code.Callvirt ->
            match this.Operand with
            | :? MethodReference as method ->
                if method.ReturnType.Name = "Void" then 0 else 1
            | _ ->
                invalidOp "Invalid instruction operand."

        | _ ->
            this.OpCode.StackBehaviourPush.AddedToStack

    member private this.RemovedFromStack =
        match this.OpCode.Code with
        | Code.Call | Code.Calli | Code.Callvirt ->
            match this.Operand with
            | :? MethodReference as method ->
                method.Parameters.Count + (if method.HasThis then 1 else 0)
            | _ ->
                invalidOp "Invalid instruction operand."

        | _ ->
            this.OpCode.StackBehaviourPop.AddedToStack

    /// Returns the `Instruction` that will consume the value
    /// pushed on the stack by this instruction.
    member this.GetConsumingInstruction() =
        let mutable ins = this
        let mutable n = 1

        while n > 0 do
            ins <- ins.Next
            n <- n - ins.RemovedFromStack

            if n > 0 then
                n <- n + ins.AddedToStack

        ins
