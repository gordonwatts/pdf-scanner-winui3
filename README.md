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

## Tablet Publish

To run this on an ARM64 tablet without installing the SDK, publish a self-contained ARM64 folder build:

```powershell
dotnet publish src/IndicoPenSpike/IndicoPenSpike.csproj -p:PublishProfile=TabletArm64SelfContained
```

Copy the published folder from `src/IndicoPenSpike/bin/Release/publish/arm64/` to the tablet and run `IndicoPenSpike.exe` from there.

If you want a smaller deployable folder instead, you can keep the app framework-dependent and install the matching Windows App Runtime and .NET 8 runtime on the tablet. That still avoids a full SDK install, but it adds a runtime prerequisite.

This app is not architecture-agnostic at publish time. The managed source can stay shared, but the output you copy to the tablet must be built for the target CPU architecture. For your tablet, that means `win-arm64`.

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
