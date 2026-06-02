namespace IndicoPenSpike.Core.UndoRedo;

public sealed class DelegateUndoableAction : IUndoableAction
{
    private readonly Action _undo;
    private readonly Action _redo;

    public DelegateUndoableAction(string description, Action undo, Action redo)
    {
        Description = description;
        _undo = undo;
        _redo = redo;
    }

    public string Description { get; }

    public void Undo() => _undo();

    public void Redo() => _redo();
}
