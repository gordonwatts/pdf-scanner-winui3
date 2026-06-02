# Architecture

## Project Layout

- `src/IndicoPenSpike`
- `src/IndicoPenSpike.Core`
- `tests/IndicoPenSpike.Tests`

## App Flow

1. The app launches into a shell with menu bar, toolbar, and a scrollable document area.
2. `File > Open PDF` opens a local file picker.
3. The selected PDF is loaded with `Windows.Data.Pdf.PdfDocument`.
4. Each page is rendered to a bitmap and inserted into a vertical stack.
5. Each page gets an overlay canvas that handles pointer input and draws strokes.

## Rendering

- Pages are rendered in page order.
- Each page keeps its aspect ratio.
- The document sits on a light gray background with padding around each page.
- The filename appears above page 1.

## Annotation Model

- `AnnotationDocument` stores strokes grouped by page number.
- `AnnotationStroke` stores page-relative points, color, thickness, and input type.
- `StrokePoint` stores the per-point coordinates and pressure.
- The model is in-memory only.

## Ink and Erase

- The spike uses a custom pointer-driven overlay rather than `InkCanvas` because this package build does not expose the public WinUI ink surface.
- Ink mode draws black, fixed-thickness polylines.
- Eraser mode removes whole strokes by hit-testing the current pointer position against stored stroke geometry.
- Pen eraser input is detected by checking inverted/eraser pointer properties.

## Undo / Redo

- `UndoRedoService` tracks actions with undo and redo stacks.
- Stroke additions and erasures both push undoable actions.
- Undo removes the rendered stroke and the model entry.
- Redo restores the same annotation data.

## Pointer Diagnostics

- The status text shows pointer type, page, current tool mode, and pressure when available.
- Touch input is ignored by the ink layer so the `ScrollViewer` can handle scrolling normally.

## Limitations

- Cursor/tool-shape feedback is limited in this build; the spike currently relies on the toolbar mode state and pointer diagnostics.
- Persistence is intentionally omitted.
- Export is intentionally omitted.
