namespace global

open Fody
open Mono.Cecil
open Mono.Cecil.Cil
open System.Collections.Generic


type Jump = { Variable : VariableDefinition
            ; mutable Jumps : List<Instruction>
            }

type Label = { Variable : VariableDefinition
             ; Jump     : Jump
             ; mutable Instruction : Instruction option
             }


/// Weaver that replaces some function calls by some custom IL code.
type ModuleWeaver() as this =
    inherit BaseModuleWeaver()

    let logDebug str   = Printf.ksprintf this.LogDebug.Invoke str
    let logWarning str = Printf.ksprintf this.LogWarning.Invoke str
    let logError str   = Printf.ksprintf this.LogError.Invoke str

    override __.GetAssembliesForScanning() = Seq.empty
    override __.ShouldCleanReference = true

    override __.Execute() =
        for ty in __.ModuleDefinition.Types do
            for mt in ty.Methods do
                __.ProcessMethod(mt)

    member __.ProcessMethod(method : MethodDefinition) =
        let labels = List()

        for i, ins in method.Body.Instructions.GetMutableEnumerator() do
            match ins.OpCode.Code with
            | Code.Call ->
                match ins.Operand with
                | :? GenericInstanceMethod as ref when ref.DeclaringType.Name = "MoreExpressions" ->
                    match ref.Name with
                    | "Return" ->
                        let returnType = ref.GenericArguments.[0]

                        if returnType.IsUnit && method.ReturnType.IsVoid then
                            assert ((not << isNull) ins.Previous && ins.Previous.OpCode.Code = Code.Ldnull)

                            ins.Previous.OpCode <- OpCodes.Nop
                        //else if returnType.FullName <> method.ReturnType.FullName then
                        //    logError "[%A] Invalid return type: expected %A but got %A." method.FullName method.ReturnType returnType

                        ins.OpCode <- OpCodes.Ret
                        ins.Operand <- null

                    | "MakeLabel" ->
                        let returnType = ref.GenericArguments.[0]

                        let labelVar = method.Body.GetCorrespondingVariable(ins.Next.Next.Next.Next)
                        let jumpVar  = method.Body.GetCorrespondingVariable(ins.Next.Next.Next.Next.Next.Next.Next)

                        // We reuse the label variable to store the result of the expression
                        labelVar.VariableType <- returnType

                        // Keep track of those variables
                        let jump  = { Variable = jumpVar; Jumps = List(); }
                        let label = { Variable = labelVar; Jump = jump; Instruction = None; }

                        labels.Add(label)

                        for i = 0 to 7 do
                            method.Body.Instructions.[ins.Offset + i].Nop()

                    | _ ->
                        assert false

                | _ ->
                    ()

            | Code.Ldloc | Code.Ldloc_S | Code.Ldloc_0 | Code.Ldloc_1 | Code.Ldloc_2 | Code.Ldloc_3 ->
                let variable = match ins.OpCode.Code with
                               | Code.Ldloc_0 -> method.Body.Variables.[0]
                               | Code.Ldloc_1 -> method.Body.Variables.[1]
                               | Code.Ldloc_2 -> method.Body.Variables.[2]
                               | Code.Ldloc_3 -> method.Body.Variables.[3]
                               | _ -> match ins.Operand with
                                      | :? int as idx -> method.Body.Variables.[idx]
                                      | :? VariableDefinition as var -> var
                                      | _ -> failwith ""

                match Seq.tryFind (fun (x : Label) -> x.Variable = variable) labels with
                | Some label ->
                    match label.Instruction with
                    | None ->
                        label.Instruction <- Some ins
                    | Some _ ->
                        logError "A label in %A was set multiple times." method

                | None ->
                    match Seq.tryFind (fun (x : Label) -> x.Jump.Variable = variable) labels with
                    | Some label ->
                        label.Jump.Jumps.Add(ins)
                    | None ->
                        ()

            | _ ->
                ()

        for label in labels do
            let jumps = label.Jump.Jumps

            match label.Instruction with
            | None when jumps.Count = 0 ->
                logError "A label in %A is never set or jumped to." method
            | None ->
                logError "A label in %A is never set." method
            | Some _ when jumps.Count = 0 ->
                logError "A label in %A is never jumped to." method

            | Some ins ->
                // Replace
                //  ldloc <label>                   < ins
                //  <default-value>
                //  tail.?
                //  callvirt FSharpFunc.Invoke()    < next
                //
                // By
                //  <default-value>
                //  stloc <label>
                //  ldloc <label>                   < next
                //
                let next = ins.GetConsumingInstruction()

                ins.Nop()

                if next.Previous.OpCode.Code = Code.Tail then
                    next.Previous.OpCode <- OpCodes.Stloc
                    next.Previous.Operand <- label.Variable
                else
                    let i = method.Body.Instructions.IndexOf(next)
                    method.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Stloc, label.Variable))

                next.OpCode <- OpCodes.Ldloc
                next.Operand <- label.Variable

                for jump in jumps do
                    // Replace
                    //  ldloc <jump>                    < jump
                    //  <value>
                    //  callvirt FSharpFunc.Invoke()    < next
                    //  pop
                    //
                    // By
                    //  <value>
                    //  stloc <label>
                    //  br <label>                      < next
                    //
                    let jnext = jump.GetConsumingInstruction()

                    jump.Nop()

                    jnext.OpCode <- OpCodes.Stloc
                    jnext.Operand <- label.Variable

                    jnext.Next.OpCode <- OpCodes.Br
                    jnext.Next.Operand <- next
