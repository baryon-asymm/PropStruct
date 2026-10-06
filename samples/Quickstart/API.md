# API.md — Quickstart

Namespace `PropStruct.Quickstart`. The library example as a running program over the
package surface; the source of the example in the root `API.md` and in the package
README. Not packed, part of no package. Built on 2026-10-04 by stage S3 of the
delivery. Everything not listed here is internal and may change.

## Entry point ✅

```csharp
namespace PropStruct.Quickstart;

internal static class Program
{
    public static int Main();   // 0 a run with an ok status, 1 a failed status, 2 an unreadable formulation
}
```

The program reads `inpt.dat` from the working directory, writes `results.m` and
`results.json` there and prints their two paths, one per line. Its reader is CI
(`.github`), which runs it in a scratch directory holding a copy of that formulation.

## The example regions ✅

| Marker | Holds |
|---|---|
| `// snippet-start: QuickstartUsings` … `// snippet-end` | the `using` lines of the example |
| `// snippet-start: Quickstart` … `// snippet-end` | the body of the example: formulation, options, the run, the two writers |

Quoted verbatim, after the common leading indentation is stripped, by the block of the
root [API.md](../../API.md) ("## Entry points": the `using` region, one blank line, the
body) and by the examples of the package README `docs/nuget/PropStruct.md` and of the
repository `README.md`; `tests/Protocol.Tests` compares them (`QuickstartExampleTests`).
