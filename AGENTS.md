# Agent Guidance

- Keep changes tightly scoped to the current spike.
- Prefer shared model changes in `src/IndicoPenSpike.Core` and UI changes in `src/IndicoPenSpike`.
- Update the console test harness whenever annotation or undo behavior changes.
- Run `dotnet build IndicoPenSpike.slnx` before committing UI or model changes.
- Run `dotnet run --project tests/IndicoPenSpike.Tests/IndicoPenSpike.Tests.csproj` after model changes.
- Do not introduce persistence, cloud sync, or Indico integration in this spike.
- Keep annotation data in memory only.
- Preserve page-relative stroke coordinates.
- Avoid broad refactors unless they are needed to complete the current implementation step.
