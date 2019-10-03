namespace global

open Fody
open Mono.Cecil
open Mono.Cecil.Cil


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
        for ins in method.Body.Instructions do
            match ins.Operand with
            | :? GenericInstanceMethod as ref when ref.Name = "Return" && ref.DeclaringType.Name = "MoreExpressions" ->
                let returnType = ref.GenericArguments.[0]

                if returnType.IsUnit && method.ReturnType.IsVoid then
                    assert ((not << isNull) ins.Previous && ins.Previous.OpCode.Code = Code.Ldnull)

                    ins.Previous.OpCode <- OpCodes.Nop
                //else if returnType.FullName <> method.ReturnType.FullName then
                //    logError "[%A] Invalid return type: expected %A but got %A." method.FullName method.ReturnType returnType

                ins.OpCode <- OpCodes.Ret
                ins.Operand <- null
            | _ ->
                ()
