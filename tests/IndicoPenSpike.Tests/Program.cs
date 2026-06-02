using IndicoPenSpike.Core.Models;
using IndicoPenSpike.Core.UndoRedo;

var tests = new List<(string Name, Action Run)>
{
    (nameof(TestStrokeCountChanges), TestStrokeCountChanges),
    (nameof(TestStrokesGroupedByPage), TestStrokesGroupedByPage),
    (nameof(TestClearingAnnotations), TestClearingAnnotations),
    (nameof(TestUndoRedoService), TestUndoRedoService),
    (nameof(TestRedoHistoryClearedAfterNewAction), TestRedoHistoryClearedAfterNewAction),
};

var failures = new List<string>();

foreach (var (name, run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Test failures:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    Environment.ExitCode = 1;
    return;
}

Console.WriteLine("All tests passed.");

static void TestStrokeCountChanges()
{
    var document = new AnnotationDocument();
    Assert.Equal(0, document.StrokeCount);

    document.AddStroke(CreateStroke(pageNumber: 1, xOffset: 0));
    Assert.Equal(1, document.StrokeCount);

    document.AddStroke(CreateStroke(pageNumber: 1, xOffset: 10));
    Assert.Equal(2, document.StrokeCount);
}

static void TestStrokesGroupedByPage()
{
    var document = new AnnotationDocument();
    var pageOne = CreateStroke(pageNumber: 1, xOffset: 0);
    var pageTwo = CreateStroke(pageNumber: 2, xOffset: 20);

    document.AddStroke(pageOne);
    document.AddStroke(pageTwo);

    Assert.Equal(1, document.GetStrokesForPage(1).Count);
    Assert.Equal(1, document.GetStrokesForPage(2).Count);
    Assert.True(document.GetStrokesForPage(1).Contains(pageOne));
    Assert.True(document.GetStrokesForPage(2).Contains(pageTwo));
}

static void TestClearingAnnotations()
{
    var document = new AnnotationDocument();
    document.AddStroke(CreateStroke(pageNumber: 1, xOffset: 0));
    document.AddStroke(CreateStroke(pageNumber: 2, xOffset: 20));

    document.Clear();

    Assert.Equal(0, document.StrokeCount);
    Assert.Empty(document.GetAllStrokes());
}

static void TestUndoRedoService()
{
    var service = new UndoRedoService();
    var calls = new List<string>();

    service.Push(new DelegateUndoableAction(
        "stroke",
        undo: () => calls.Add("undo"),
        redo: () => calls.Add("redo")));

    Assert.True(service.CanUndo);
    Assert.False(service.CanRedo);

    Assert.True(service.TryUndo());
    Assert.SequenceEqual(new[] { "undo" }, calls);
    Assert.True(service.CanRedo);

    Assert.True(service.TryRedo());
    Assert.SequenceEqual(new[] { "undo", "redo" }, calls);
}

static void TestRedoHistoryClearedAfterNewAction()
{
    var service = new UndoRedoService();

    service.Push(new DelegateUndoableAction("first", undo: () => { }, redo: () => { }));
    Assert.True(service.TryUndo());
    Assert.True(service.CanRedo);

    service.Push(new DelegateUndoableAction("second", undo: () => { }, redo: () => { }));
    Assert.False(service.CanRedo);
}

static AnnotationStroke CreateStroke(int pageNumber, double xOffset)
{
    return AnnotationStroke.Create(
        pageNumber,
        new[]
        {
            new StrokePoint(10 + xOffset, 15),
            new StrokePoint(25 + xOffset, 35),
            new StrokePoint(40 + xOffset, 45),
        },
        inputType: "mouse");
}

static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null) => True(!condition, message ?? "Expected condition to be false.");

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void Empty<T>(IEnumerable<T> items)
    {
        if (items.Any())
        {
            throw new InvalidOperationException("Expected sequence to be empty.");
        }
    }

    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException("Expected sequences to match.");
        }
    }
}
