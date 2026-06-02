namespace IndicoPenSpike.Core.Models;

public sealed record AnnotationStroke(
    int PageNumber,
    IReadOnlyList<StrokePoint> Points,
    uint ArgbColor,
    double Thickness,
    string InputType)
{
    public static AnnotationStroke Create(
        int pageNumber,
        IEnumerable<StrokePoint> points,
        uint argbColor = 0xFF000000,
        double thickness = 3.0,
        string inputType = "unknown")
    {
        var pointList = points.ToArray();
        return new AnnotationStroke(pageNumber, pointList, argbColor, thickness, inputType);
    }
}
