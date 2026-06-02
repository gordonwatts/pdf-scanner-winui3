# Indico Pen Spike

WinUI 3 spike for PDF rendering, pen annotation, erasing, undo/redo, and pointer diagnostics.

## Prerequisites

- Windows 11
- .NET 8 SDK
- A local PDF file to open

## Build

```powershell
dotnet build IndicoPenSpike.slnx
```

## Run

```powershell
dotnet run --project src/IndicoPenSpike/IndicoPenSpike.csproj
```

## Test

The repository uses a small console-based test harness for the shared annotation model:

```powershell
dotnet run --project tests/IndicoPenSpike.Tests/IndicoPenSpike.Tests.csproj
```

## Manual Validation

- Open a local PDF.
- Confirm the filename appears above page 1.
- Scroll vertically through pages.
- Draw with mouse or pen.
- Switch to eraser mode and remove a stroke.
- Undo and redo both stroke creation and erasure.
- Confirm touch does not create ink and still scrolls the document.
- Watch pointer diagnostics update while interacting.

## Notes

- Strokes are stored in memory only.
- The annotation model is page-relative and grouped by page number.
- The current spike uses a custom overlay renderer for ink.
