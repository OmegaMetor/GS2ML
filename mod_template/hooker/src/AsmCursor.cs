using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace GMHooker;

// totally not stolen from MonoMod
public class AsmCursor {
    public int index {
        get => _index;
        set {
            if(value < 0 || value > _code.Instructions.Count) return;
            _index = value;
        }
    }

    private int _index;

    private readonly UndertaleData _data;
    private readonly UndertaleCode _code;
    private readonly Dictionary<string, UndertaleVariable> _locals;
    private readonly Dictionary<string, UndertaleInstruction> _labels = new();
    private readonly Dictionary<UndertaleInstruction, string> _labelTargets = new();

    public AsmCursor(UndertaleData data, UndertaleCode code, UndertaleCodeLocals locals) {
        _data = data;
        _code = code;
        _locals = locals.GetLocalVars(data);
    }

    public UndertaleInstruction GetCurrent() => _code.Instructions[index];

    public void Emit(UndertaleInstruction instruction) {
        _code.Instructions.Insert(index, instruction);
        InstructionChanged();
        index++;
    }

    public void Emit(string source) => Emit(Assemble(source));

    public void Replace(UndertaleInstruction instruction) {
        _code.Instructions[index] = instruction;
        InstructionChanged();
    }

    public void Replace(string source) => Replace(Assemble(source));

    public void DefineLabel(string name) => _labels.Add(name, GetCurrent());

    public bool GotoFirst(string match) => GotoFirst(Assemble(match));
    public bool GotoLast(string match) => GotoLast(Assemble(match));
    public bool GotoNext(string match) => GotoNext(Assemble(match));
    public bool GotoPrev(string match) => GotoPrev(Assemble(match));

    public bool GotoFirst(UndertaleInstruction match) => GotoFirst(instruction => instruction.Match(match));
    public bool GotoLast(UndertaleInstruction match) => GotoLast(instruction => instruction.Match(match));
    public bool GotoNext(UndertaleInstruction match) => GotoNext(instruction => instruction.Match(match));
    public bool GotoPrev(UndertaleInstruction match) => GotoPrev(instruction => instruction.Match(match));

    public bool GotoFirst(Predicate<UndertaleInstruction> match) => TrySetIndex(_code.Instructions.FindIndex(match));
    public bool GotoLast(Predicate<UndertaleInstruction> match) => TrySetIndex(_code.Instructions.FindLastIndex(match));
    public bool GotoNext(Predicate<UndertaleInstruction> match) => IsIndexValid(index + 1) &&
        TrySetIndex(_code.Instructions.FindIndex(index + 1, match));
    public bool GotoPrev(Predicate<UndertaleInstruction> match) => IsIndexValid(index - 1) &&
        TrySetIndex(_code.Instructions.FindLastIndex(index - 1, index - 2, match));

    private bool TrySetIndex(int index) {
        if(!IsIndexValid(index))
            return false;
        this.index = index;
        return true;
    }

    private bool IsIndexValid(int index) => index >= 0 && index < _code.Instructions.Count;

    private UndertaleInstruction Assemble(string source) {
        UndertaleInstruction instruction = Assembler.AssembleOne(source, _data.Functions, _data.Variables,
            _data.Strings, _locals, out string? label, _data);

        if(label is not null)
            _labelTargets.Add(instruction, label);

        return instruction;
    }

    private void InstructionChanged() {
        _code.UpdateAddresses();
        foreach((UndertaleInstruction? target, string? label) in _labelTargets)
            target.JumpOffset = (int)_labels[label].Address - (int)target.Address;
    }
}
