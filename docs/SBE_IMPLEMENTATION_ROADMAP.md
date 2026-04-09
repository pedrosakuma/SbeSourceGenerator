# SBE Code Generator — Roadmap

## Current Status

**Version**: 0.4.0  
**SBE 1.0 Compliance**: See [SBE_FEATURE_COMPLETENESS.md](./SBE_FEATURE_COMPLETENESS.md) for the detailed compliance matrix.

Spec compliance gaps are tracked as [GitHub issues labeled `spec-compliance`](https://github.com/pedrosakuma/SbeSourceGenerator/labels/spec-compliance).

---

## What's Been Delivered

| Version | Highlights |
|---------|-----------|
| **0.4.0** | Single-pass `XmlReader` parser (~2.4× throughput), LINQ elimination, string interpolation removal, collection pre-sizing |
| **0.3.0** | Diagnostics SBE007-SBE010, XXE fix, group loop guard, phase isolation, safe dictionary accessors |
| **0.2.0** | GroupSizeEncoding `numInGroup` default fix (`uint` → `ushort`), NuGet packaging, CI/CD pipeline |
| **0.1.0** | Initial release — primitives, composites, enums, sets, messages, groups, optional fields, varData, validation, byte order, schema versioning, deprecated fields |

---

## Infrastructure

- ✅ **CI/CD**: GitHub Actions with build, test, NuGet publish ([CICD_PIPELINE.md](./CICD_PIPELINE.md))
- ✅ **NuGet**: Published as `SbeSourceGenerator` with Source Link and snupkg
- ✅ **Diagnostics**: SBE001–SBE010 with structured error reporting
- ✅ **AOT compatibility**: Generated code is trimming/AOT safe (no reflection, no Marshal)
- ✅ **Benchmarks**: BenchmarkDotNet project in `benchmarks/`
- ✅ **Examples**: `SbeBinanceConsole` (live market data), `HighPerformanceMarketData` (AOT-compatible patterns)

---

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines and the [spec-compliance issues](https://github.com/pedrosakuma/SbeSourceGenerator/labels/spec-compliance) for prioritized gaps.
