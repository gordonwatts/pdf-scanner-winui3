namespace IndicoPenSpike.Core.UndoRedo;

public sealed class UndoRedoService
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public event EventHandler? Changed;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public string? UndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    public string? RedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    public void Push(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryUndo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool TryRedo()
    {
        if (!CanRedo)
        {
            return false;
        }

        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
