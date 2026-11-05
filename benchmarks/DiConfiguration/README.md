# DI configuration measurements

Measured on 2026-11-05 with SDK 10.0.403, .NET 10.0.14, Windows build
10.0.26300, x64, 24 logical processors, and workstation GC.

## Decision

Repeated XML loading has a measurable cost. The existing
`AddTemplateEmail(MailConfiguration, ...)` overload already avoids that work:
load XML once during application setup and register the resulting configuration.
No additional cache, option, dependency, or change to library behavior is needed.
The file-path overload retains its existing per-helper file loading behavior.

This is an opt-in choice for applications that resolve helpers frequently or have
large template files. The measurements do not establish a production bottleneck
or justify changing all consumers to a shared configuration.

## Reproduce

Run from the repository root with the .NET 10 SDK:

```sh
dotnet build benchmarks/DiConfiguration/DiConfiguration.csproj --configuration Release
dotnet run --project benchmarks/DiConfiguration/DiConfiguration.csproj --configuration Release --no-build
```

The executable writes progress to stderr and JSON measurements to stdout. It uses
the existing DI dependency, creates and deletes its own temporary XML file, and
performs no SMTP operations. It is separate from the solution and CI tests.

Each case uses 1, 10, 100, or 1000 unique message types with 1024-character ASCII
HTML bodies. Both registrations use the same serialized configuration; the
shared instance is deserialized before timing. Provider construction, initial
loading, fixture generation, and output/isolation checks are excluded from timing.

The two scenarios measure resolving a transient `IEmailHelper`, and resolving it
then calling `ForType(...).Map(...).BuildMessage(...)`. Each case has 100 warmup
operations, a 100-operation calibration, and seven samples with a calibrated
iteration count. A full GC occurs before each sample, outside timing. Allocation
uses `GC.GetAllocatedBytesForCurrentThread()` and includes validation and, where
applicable, rendering. It is allocated bytes, not retained memory or process RSS.

This is a warm-file, single-threaded exploratory measurement with default runtime
JIT settings and fixed case order. OS caching, background activity, and JIT
tiering affect timings. The two scenarios are measured separately: do not
subtract their timings to estimate rendering cost. No actual email latency,
cold-storage access, or production throughput is measured.

## Results

First run: median microseconds per operation across seven samples.

| Templates | XML KiB | File: resolve | Shared: resolve | File: resolve + build | Shared: resolve + build |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1.35 | 68.80 | 0.36 | 44.82 | 0.91 |
| 10 | 11.94 | 67.42 | 1.01 | 59.65 | 1.89 |
| 100 | 118.03 | 217.06 | 8.88 | 193.69 | 11.22 |
| 1000 | 1180.62 | 1787.84 | 87.71 | 1761.25 | 93.05 |

Median allocated KiB per resolve + build operation:

| Templates | File | Shared |
| ---: | ---: | ---: |
| 1 | 22.14 | 4.58 |
| 10 | 53.80 | 8.15 |
| 100 | 307.04 | 45.96 |
| 1000 | 2710.37 | 424.11 |

A second independent process confirmed the direction and approximate scale:

| Templates | File: resolve + build, microseconds | Shared: resolve + build, microseconds |
| ---: | ---: | ---: |
| 1 | 42.63 | 0.77 |
| 10 | 52.46 | 1.77 |
| 100 | 209.57 | 10.61 |
| 1000 | 1852.60 | 113.43 |

Shared configuration still incurs validation for each new helper, so its cost
and allocation also grow with template count. This change does not bypass
validation, change DI lifetimes, or introduce immutable configuration models.

## Reuse and reload behavior

- File-path registration reads and validates XML on each helper resolution. New
  helpers see file changes; existing helpers and their builders keep the
  configuration loaded when that helper was created. A missing file fails the
  next resolution without invalidating existing helpers.
- Configuration-object registration shares the supplied instance without further
  XML reads. File edits or deletion do not affect helpers from that registration.
  To adopt a new snapshot, deserialize a fresh configuration and create a new
  registration/provider, for example on application restart. Existing helpers
  retain their original instance.
- The shared instance is mutable, not copied or frozen. Treat it as read-only
  during use. Every helper remains transient and every `ForType` creates a fresh
  builder with separate mappings. There is no file watcher or automatic reload.

`ServiceCollectionExtensionsTests` covers file updates, missing files, shared
configuration reuse, explicit replacement through a new provider, and mapping
isolation. The measurement executable also checks rendered output and interleaved
builder isolation before timing every template size.
