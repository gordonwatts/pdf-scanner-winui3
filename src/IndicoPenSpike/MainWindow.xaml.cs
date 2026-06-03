using System.Collections.Generic;
using System.Linq;
using IndicoPenSpike.Core.Models;
using IndicoPenSpike.Core.UndoRedo;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using WinPointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace IndicoPenSpike;

public sealed partial class MainWindow : Window
{
    private readonly AnnotationDocument _annotationDocument = new();
    private readonly UndoRedoService _undoRedoService = new();
    private readonly List<PageSurface> _pages = new();
    private ToolMode _toolMode = ToolMode.Ink;
    private bool _suppressModeEvents;

    public MainWindow()
    {
        InitializeComponent();

        Title = "Indico Pen Spike";
        _annotationDocument.Changed += AnnotationDocument_Changed;
        _undoRedoService.Changed += UndoRedoService_Changed;
        InkModeButton.Checked += InkModeButton_Checked;
        InkModeButton.Unchecked += InkModeButton_Unchecked;
        EraserModeButton.Checked += EraserModeButton_Checked;
        EraserModeButton.Unchecked += EraserModeButton_Unchecked;

        _suppressModeEvents = true;
        SetToolMode(ToolMode.Ink);
        _suppressModeEvents = false;
        ShowEmptyState();
        UpdateStatus();
    }

    private void AnnotationDocument_Changed(object? sender, EventArgs e) => UpdateStatus();

    private void UndoRedoService_Changed(object? sender, EventArgs e) => UpdateStatus();

    private void UpdateStatus()
    {
        StrokeCountText.Text = $"{_annotationDocument.StrokeCount} strokes";
        UndoButton.IsEnabled = _undoRedoService.CanUndo;
        RedoButton.IsEnabled = _undoRedoService.CanRedo;
    }

    private async void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        await OpenPdfAsync();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        _undoRedoService.TryUndo();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        _undoRedoService.TryRedo();
    }

    private void InkModeButton_Checked(object sender, RoutedEventArgs e) => SetToolMode(ToolMode.Ink);

    private void InkModeButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_suppressModeEvents)
        {
            return;
        }

        if (EraserModeButton.IsChecked != true)
        {
            InkModeButton.IsChecked = true;
        }
    }

    private void EraserModeButton_Checked(object sender, RoutedEventArgs e) => SetToolMode(ToolMode.Eraser);

    private void EraserModeButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_suppressModeEvents)
        {
            return;
        }

        if (InkModeButton.IsChecked != true)
        {
            EraserModeButton.IsChecked = true;
        }
    }

    private async Task OpenPdfAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".pdf");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        await LoadPdfAsync(file);
    }

    private async Task LoadPdfAsync(StorageFile file)
    {
        _annotationDocument.Clear();
        _undoRedoService.Clear();
        _pages.Clear();
        DocumentStack.Children.Clear();
        EmptyStateOverlay.Visibility = Visibility.Collapsed;

        var pdfDocument = await PdfDocument.LoadFromFileAsync(file);
        var pageCount = (int)pdfDocument.PageCount;

        DocumentStack.Children.Add(new TextBlock
        {
            Text = file.Name,
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = new SolidColorBrush(Colors.Black)
        });

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var pageNumber = pageIndex + 1;
            using var page = pdfDocument.GetPage((uint)pageIndex);
            var pageSurface = await CreatePageSurfaceAsync(pageNumber, page, file.Name);
            _pages.Add(pageSurface);
            DocumentStack.Children.Add(pageSurface.Root);
        }

        SetToolMode(_toolMode);
        UpdateStatus();
    }

    private async Task<PageSurface> CreatePageSurfaceAsync(int pageNumber, PdfPage pdfPage, string fileName)
    {
        var displayWidth = Math.Max(700, DocumentScrollViewer.ActualWidth > 0 ? DocumentScrollViewer.ActualWidth - 96 : 900);
        var aspectRatio = pdfPage.Size.Height <= 0 ? 1.0 : pdfPage.Size.Width / pdfPage.Size.Height;
        var displayHeight = displayWidth / Math.Max(aspectRatio, 0.01);

        var bitmap = new BitmapImage();
        using (IRandomAccessStream stream = new InMemoryRandomAccessStream())
        {
            await pdfPage.RenderToStreamAsync(stream);
            stream.Seek(0);
            await bitmap.SetSourceAsync(stream);
        }

        return new PageSurface(
            pageNumber,
            fileName,
            bitmap,
            displayWidth,
            displayHeight,
            _annotationDocument,
            this,
            _toolMode,
            HandleStrokeCommitted,
            HandleStrokeErased);
    }

    private void HandleStrokeCommitted(PageSurface pageSurface, StrokeSet strokeSet)
    {
        _undoRedoService.Push(new DelegateUndoableAction(
            $"Add stroke on page {pageSurface.PageNumber}",
            undo: () => pageSurface.RemoveAnnotations(strokeSet.Annotations),
            redo: () => pageSurface.AddStrokeSet(strokeSet)));
        UpdateStatus();
    }

    private void HandleStrokeErased(PageSurface pageSurface, StrokeSet strokeSet)
    {
        _undoRedoService.Push(new DelegateUndoableAction(
            $"Erase stroke on page {pageSurface.PageNumber}",
            undo: () => pageSurface.AddStrokeSet(strokeSet),
            redo: () => pageSurface.RemoveAnnotations(strokeSet.Annotations)));
        UpdateStatus();
    }

    private void SetToolMode(ToolMode mode)
    {
        _toolMode = mode;
        _suppressModeEvents = true;
        InkModeButton.IsChecked = mode == ToolMode.Ink;
        EraserModeButton.IsChecked = mode == ToolMode.Eraser;
        _suppressModeEvents = false;

        foreach (var page in _pages)
        {
            page.SetToolMode(mode);
        }

        if (_pages.Count > 0)
        {
            PointerStatusText.Text = $"pointer: {mode.ToString().ToLowerInvariant()}";
        }
    }

    private void ShowEmptyState()
    {
        DocumentStack.Children.Clear();
        EmptyStateOverlay.Visibility = Visibility.Visible;
    }

    private void UpdatePointerStatus(string pointerType, int pageNumber, double pressure, ToolMode resolvedMode)
    {
        PointerStatusText.Text = $"pointer: {pointerType} | page: {pageNumber} | mode: {resolvedMode.ToString().ToLowerInvariant()} | pressure: {pressure:0.00}";
    }

    internal void RunSmokeTest()
    {
        var pageSurface = new PageSurface(
            pageNumber: 1,
            fileName: "smoke-test.pdf",
            bitmap: new BitmapImage(),
            width: 100,
            height: 100,
            annotationDocument: _annotationDocument,
            window: this,
            initialMode: _toolMode,
            strokeCommitted: HandleStrokeCommitted,
            strokeErased: HandleStrokeErased);

        DocumentStack.Children.Add(pageSurface.Root);
        DocumentStack.Children.Remove(pageSurface.Root);
    }

    private enum ToolMode
    {
        Ink,
        Eraser
    }

    private sealed record StrokeSet(int PageNumber, IReadOnlyList<AnnotationStroke> Annotations);

    private sealed class PageSurface
    {
        private readonly AnnotationDocument _annotationDocument;
        private readonly MainWindow _window;
        private readonly Canvas _strokeLayer;
        private readonly Dictionary<AnnotationStroke, Polyline> _visuals = new(ReferenceEqualityComparer.Instance);
        private readonly Action<PageSurface, StrokeSet> _strokeCommitted;
        private readonly Action<PageSurface, StrokeSet> _strokeErased;
        private readonly List<StrokePoint> _activeStrokePoints = new();
        private readonly Ellipse _hoverCursor;
        private Polyline? _activeStrokeVisual;
        private bool _isDrawing;
        private bool _isErasing;
        private uint? _activeTouchScrollPointerId;
        private Point _lastTouchScrollPoint;
        private Point _lastPointerPoint;

        public PageSurface(
            int pageNumber,
            string fileName,
            BitmapImage bitmap,
            double width,
            double height,
            AnnotationDocument annotationDocument,
            MainWindow window,
            ToolMode initialMode,
            Action<PageSurface, StrokeSet> strokeCommitted,
            Action<PageSurface, StrokeSet> strokeErased)
        {
            PageNumber = pageNumber;
            _annotationDocument = annotationDocument;
            _window = window;
            _strokeCommitted = strokeCommitted;
            _strokeErased = strokeErased;

            _strokeLayer = CreateStrokeLayer(width, height);
            _hoverCursor = CreateHoverCursor();
            Canvas.SetZIndex(_hoverCursor, 1000);
            _strokeLayer.Children.Add(_hoverCursor);

            var pageHeader = new TextBlock
            {
                Text = pageNumber == 1 ? fileName : $"Page {pageNumber}",
                FontSize = pageNumber == 1 ? 20 : 14,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = new SolidColorBrush(pageNumber == 1 ? Colors.Black : Colors.DimGray)
            };

            var pageFrame = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20),
                Child = new Grid
                {
                    Width = width,
                    Height = height,
                    Children =
                    {
                        new Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.Fill
                        },
                        _strokeLayer
                    }
                }
            };

            Root = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            Grid.SetRow(pageHeader, 0);
            Grid.SetRow(pageFrame, 1);
            Root.Children.Add(pageHeader);
            Root.Children.Add(pageFrame);

            SetToolMode(initialMode);
        }

        public int PageNumber { get; }

        public Grid Root { get; }

        public void SetToolMode(ToolMode mode)
        {
            UpdateHoverCursorAppearance(mode);
        }

        public void AddStrokeSet(StrokeSet strokeSet)
        {
            foreach (var annotation in strokeSet.Annotations)
            {
                if (_visuals.ContainsKey(annotation))
                {
                    continue;
                }

                var visual = CreateStrokeVisual(annotation);
                _visuals.Add(annotation, visual);
                _strokeLayer.Children.Add(visual);
                _annotationDocument.AddStroke(annotation);
            }
        }

        public void RemoveAnnotations(IReadOnlyList<AnnotationStroke> annotations)
        {
            var removed = new List<AnnotationStroke>();
            foreach (var annotation in annotations)
            {
                if (_visuals.TryGetValue(annotation, out var visual))
                {
                    _strokeLayer.Children.Remove(visual);
                    _visuals.Remove(annotation);
                    _annotationDocument.RemoveStroke(annotation);
                    removed.Add(annotation);
                }
            }
        }

        private Canvas CreateStrokeLayer(double width, double height)
        {
            var layer = new Canvas
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ManipulationMode = ManipulationModes.None
            };

            layer.PointerPressed += OnPointerPressed;
            layer.PointerMoved += OnPointerMoved;
            layer.PointerReleased += OnPointerReleased;
            layer.PointerCanceled += OnPointerCanceled;
            layer.PointerCaptureLost += OnPointerCaptureLost;
            layer.PointerEntered += OnPointerActivity;
            layer.PointerExited += OnPointerExited;
            layer.PointerMoved += OnPointerActivity;
            layer.PointerPressed += OnPointerActivity;
            layer.PointerReleased += OnPointerActivity;
            return layer;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_strokeLayer);
            if (point.PointerDeviceType == WinPointerDeviceType.Touch)
            {
                BeginTouchScroll(e, point);
                return;
            }

            TrackPointer(point);
            if (!IsPointerDown(point) && point.PointerDeviceType != WinPointerDeviceType.Pen)
            {
                return;
            }

            e.Handled = true;
            _lastPointerPoint = point.Position;

            if (ResolveMode(point) == ToolMode.Eraser)
            {
                _isErasing = true;
                _strokeLayer.CapturePointer(e.Pointer);
                EraseAtPoint(point.Position, point.PointerDeviceType, point.Properties.Pressure);
                return;
            }

            _isDrawing = true;
            _activeStrokePoints.Clear();
            _activeStrokeVisual = CreateStrokeVisual(null);
            _strokeLayer.Children.Add(_activeStrokeVisual);
            _activeStrokeVisual.Points.Add(point.Position);
            _activeStrokePoints.Add(new StrokePoint(point.Position.X, point.Position.Y, point.Properties.Pressure));
            _strokeLayer.CapturePointer(e.Pointer);
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_strokeLayer);
            if (point.PointerDeviceType == WinPointerDeviceType.Touch)
            {
                ContinueTouchScroll(e, point);
                return;
            }

            TrackPointer(point);
            _lastPointerPoint = point.Position;

            if (_isDrawing && _activeStrokeVisual is not null)
            {
                _activeStrokeVisual.Points.Add(point.Position);
                _activeStrokePoints.Add(new StrokePoint(point.Position.X, point.Position.Y, point.Properties.Pressure));
                e.Handled = true;
                return;
            }

            if (_isErasing)
            {
                e.Handled = true;
                if (IsPointerDown(point) || point.PointerDeviceType == WinPointerDeviceType.Pen)
                {
                    EraseAtPoint(point.Position, point.PointerDeviceType, point.Properties.Pressure);
                }
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_strokeLayer);
            if (point.PointerDeviceType == WinPointerDeviceType.Touch)
            {
                EndTouchScroll(e, point);
                return;
            }

            TrackPointer(point);
            if (_isDrawing)
            {
                e.Handled = true;
                FinishStroke(e, point);
                return;
            }

            if (_isErasing)
            {
                e.Handled = true;
                _isErasing = false;
                _strokeLayer.ReleasePointerCapture(e.Pointer);
            }
        }

        private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_strokeLayer);
            if (point.PointerDeviceType == WinPointerDeviceType.Touch)
            {
                EndTouchScroll(e, point);
                return;
            }

            if (_isDrawing)
            {
                CancelStroke();
            }

            if (_isErasing)
            {
                _isErasing = false;
                _strokeLayer.ReleasePointerCapture(e.Pointer);
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            CancelStroke();
            _isErasing = false;
            _activeTouchScrollPointerId = null;
        }

        private void OnPointerActivity(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_strokeLayer);
            if (point.PointerDeviceType != WinPointerDeviceType.Touch)
            {
                TrackPointer(point);
            }

            var pointerType = point.PointerDeviceType switch
            {
                WinPointerDeviceType.Mouse => "mouse",
                WinPointerDeviceType.Pen => point.Properties.IsEraser || point.Properties.IsInverted ? "eraser" : "pen",
                WinPointerDeviceType.Touch => "touch",
                _ => "unknown"
            };

            var pressure = point.PointerDeviceType == WinPointerDeviceType.Pen || point.PointerDeviceType == WinPointerDeviceType.Mouse
                ? point.Properties.Pressure
                : 0;

            _window.UpdatePointerStatus(pointerType, PageNumber, pressure, ResolveMode(point));
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _hoverCursor.Visibility = Visibility.Collapsed;
        }

        private void FinishStroke(PointerRoutedEventArgs routedEventArgs, PointerPoint point)
        {
            if (_activeStrokeVisual is null || _activeStrokePoints.Count == 0)
            {
                CancelStroke();
                return;
            }

            _activeStrokeVisual.Points.Add(point.Position);
            _activeStrokePoints.Add(new StrokePoint(point.Position.X, point.Position.Y, point.Properties.Pressure));

            var annotation = AnnotationStroke.Create(
                PageNumber,
                _activeStrokePoints,
                argbColor: 0xFF000000,
                thickness: 3.5,
                inputType: ResolveMode(point).ToString().ToLowerInvariant());

            _strokeLayer.Children.Remove(_activeStrokeVisual);
            _activeStrokeVisual = null;
            _isDrawing = false;
            _strokeLayer.ReleasePointerCapture(routedEventArgs.Pointer);
            AddStrokeSet(new StrokeSet(PageNumber, new[] { annotation }));
            _strokeCommitted(this, new StrokeSet(PageNumber, new[] { annotation }));
        }

        private void CancelStroke()
        {
            if (_activeStrokeVisual is not null)
            {
                _strokeLayer.Children.Remove(_activeStrokeVisual);
            }

            _activeStrokeVisual = null;
            _activeStrokePoints.Clear();
            _isDrawing = false;
        }

        private void EraseAtPoint(Point location, WinPointerDeviceType pointerType, float pressure)
        {
            var threshold = 14.0;
            var toRemove = _visuals
                .Where(pair => AnnotationHitTester.StrokeIntersectsPoint(pair.Key, location.X, location.Y, threshold))
                .Select(pair => pair.Key)
                .ToList();

            if (toRemove.Count == 0)
            {
                return;
            }

            foreach (var annotation in toRemove)
            {
                if (_visuals.TryGetValue(annotation, out var visual))
                {
                    _strokeLayer.Children.Remove(visual);
                    _visuals.Remove(annotation);
                    _annotationDocument.RemoveStroke(annotation);
                }
            }

            var strokeSet = new StrokeSet(PageNumber, toRemove);
            _strokeErased(this, strokeSet);
        }

        private void BeginTouchScroll(PointerRoutedEventArgs routedEventArgs, PointerPoint point)
        {
            if (_activeTouchScrollPointerId is not null)
            {
                return;
            }

            _activeTouchScrollPointerId = point.PointerId;
            _lastTouchScrollPoint = routedEventArgs.GetCurrentPoint(_window.DocumentScrollViewer).Position;
            _strokeLayer.CapturePointer(routedEventArgs.Pointer);
            routedEventArgs.Handled = true;
            _window.UpdatePointerStatus("touch", PageNumber, pressure: 0, ResolveMode(point));
        }

        private void ContinueTouchScroll(PointerRoutedEventArgs routedEventArgs, PointerPoint point)
        {
            if (_activeTouchScrollPointerId != point.PointerId)
            {
                return;
            }

            var currentPoint = routedEventArgs.GetCurrentPoint(_window.DocumentScrollViewer).Position;
            var deltaX = currentPoint.X - _lastTouchScrollPoint.X;
            var deltaY = currentPoint.Y - _lastTouchScrollPoint.Y;
            _lastTouchScrollPoint = currentPoint;

            _window.DocumentScrollViewer.ChangeView(
                _window.DocumentScrollViewer.HorizontalOffset - deltaX,
                _window.DocumentScrollViewer.VerticalOffset - deltaY,
                zoomFactor: null,
                disableAnimation: true);

            routedEventArgs.Handled = true;
            _window.UpdatePointerStatus("touch", PageNumber, pressure: 0, ResolveMode(point));
        }

        private void EndTouchScroll(PointerRoutedEventArgs routedEventArgs, PointerPoint point)
        {
            if (_activeTouchScrollPointerId != point.PointerId)
            {
                return;
            }

            _activeTouchScrollPointerId = null;
            _strokeLayer.ReleasePointerCapture(routedEventArgs.Pointer);
            routedEventArgs.Handled = true;
            _window.UpdatePointerStatus("touch", PageNumber, pressure: 0, ResolveMode(point));
        }

        private ToolMode ResolveMode(PointerPoint point)
        {
            if (_window._toolMode == ToolMode.Eraser)
            {
                return ToolMode.Eraser;
            }

            return point.PointerDeviceType == WinPointerDeviceType.Pen && (point.Properties.IsEraser || point.Properties.IsInverted)
                ? ToolMode.Eraser
                : ToolMode.Ink;
        }

        private void TrackPointer(PointerPoint point)
        {
            UpdateHoverCursorAppearance(ResolveMode(point));

            var cursorSize = _hoverCursor.Width;
            Canvas.SetLeft(_hoverCursor, point.Position.X - cursorSize / 2);
            Canvas.SetTop(_hoverCursor, point.Position.Y - cursorSize / 2);
            _hoverCursor.Visibility = Visibility.Visible;
        }

        private void UpdateHoverCursorAppearance(ToolMode mode)
        {
            if (_hoverCursor is null)
            {
                return;
            }

            if (mode == ToolMode.Eraser)
            {
                _hoverCursor.Width = 22;
                _hoverCursor.Height = 22;
                _hoverCursor.Fill = new SolidColorBrush(Colors.Transparent);
                _hoverCursor.Stroke = new SolidColorBrush(Colors.DimGray);
                _hoverCursor.StrokeThickness = 2;
                return;
            }

            _hoverCursor.Width = 8;
            _hoverCursor.Height = 8;
            _hoverCursor.Fill = new SolidColorBrush(Colors.Black);
            _hoverCursor.Stroke = new SolidColorBrush(Colors.White);
            _hoverCursor.StrokeThickness = 1;
        }

        private static Ellipse CreateHoverCursor() =>
            new()
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Colors.Black),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

        private static bool IsPointerDown(PointerPoint point) =>
            point.PointerDeviceType == WinPointerDeviceType.Mouse
                ? point.Properties.IsLeftButtonPressed
                : point.IsInContact || point.Properties.Pressure > 0;

        private Polyline CreateStrokeVisual(AnnotationStroke? annotation)
        {
            var line = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = annotation?.Thickness ?? 3.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false
            };

            if (annotation is not null)
            {
                foreach (var strokePoint in annotation.Points)
                {
                    line.Points.Add(new Point(strokePoint.X, strokePoint.Y));
                }
            }

            return line;
        }
    }
}
