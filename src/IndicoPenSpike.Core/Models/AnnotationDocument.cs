namespace IndicoPenSpike.Core.Models;

public sealed class AnnotationDocument
{
    private readonly Dictionary<int, List<AnnotationStroke>> _strokesByPage = new();

    public event EventHandler? Changed;

    public int StrokeCount { get; private set; }

    public IReadOnlyDictionary<int, IReadOnlyList<AnnotationStroke>> StrokesByPage =>
        _strokesByPage.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<AnnotationStroke>)pair.Value.AsReadOnly());

    public AnnotationStroke AddStroke(AnnotationStroke stroke)
    {
        if (!_strokesByPage.TryGetValue(stroke.PageNumber, out var strokes))
        {
            strokes = new List<AnnotationStroke>();
            _strokesByPage.Add(stroke.PageNumber, strokes);
        }

        strokes.Add(stroke);
        StrokeCount++;
        Changed?.Invoke(this, EventArgs.Empty);
        return stroke;
    }

    public bool RemoveStroke(AnnotationStroke stroke)
    {
        if (!_strokesByPage.TryGetValue(stroke.PageNumber, out var strokes))
        {
            return false;
        }

        var removed = strokes.Remove(stroke);
        if (!removed)
        {
            return false;
        }

        if (strokes.Count == 0)
        {
            _strokesByPage.Remove(stroke.PageNumber);
        }

        StrokeCount--;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public IReadOnlyList<AnnotationStroke> GetStrokesForPage(int pageNumber)
    {
        if (_strokesByPage.TryGetValue(pageNumber, out var strokes))
        {
            return strokes.AsReadOnly();
        }

        return Array.Empty<AnnotationStroke>();
    }

    public IReadOnlyList<AnnotationStroke> GetAllStrokes() =>
        _strokesByPage.Values.SelectMany(strokes => strokes).ToArray();

    public void Clear()
    {
        _strokesByPage.Clear();
        StrokeCount = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
