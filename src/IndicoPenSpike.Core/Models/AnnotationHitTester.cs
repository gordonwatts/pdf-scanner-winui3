namespace IndicoPenSpike.Core.Models;

public static class AnnotationHitTester
{
    public static bool StrokeIntersectsPoint(AnnotationStroke stroke, double x, double y, double threshold)
    {
        if (stroke.Points.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < stroke.Points.Count - 1; i++)
        {
            if (DistanceToSegment(x, y, stroke.Points[i], stroke.Points[i + 1]) <= threshold)
            {
                return true;
            }
        }

        return stroke.Points.Any(point => Distance(x, y, point.X, point.Y) <= threshold);
    }

    private static double DistanceToSegment(double x, double y, StrokePoint start, StrokePoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if (dx == 0 && dy == 0)
        {
            return Distance(x, y, start.X, start.Y);
        }

        var t = ((x - start.X) * dx + (y - start.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));

        var projectionX = start.X + t * dx;
        var projectionY = start.Y + t * dy;
        return Distance(x, y, projectionX, projectionY);
    }

    private static double Distance(double x1, double y1, double x2, double y2) =>
        Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
}
