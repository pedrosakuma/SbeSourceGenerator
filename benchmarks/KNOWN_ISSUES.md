# Known Issues - Phase 4

## ~~Benchmark Project Compilation Issues~~ (FIXED)

### Issue (Resolved)
The benchmark project previously failed to compile due to a code generation bug in the source generator where `GetNumInGroupType()` returned `"uint"` (uint32) as a fallback instead of `"ushort"` (uint16, the SBE spec default).

### Fix
- Changed fallback in `MessagesCodeGenerator.GetNumInGroupType()` from `"uint"` to `"ushort"`
- Changed default in `GroupDefinition.NumInGroupType` from `"uint"` to `"ushort"`
- Added unit tests for both uint16 and uint32 `numInGroup` schemas

### Status
**Fixed** — All benchmarks re-enabled and `.disabled` suffix removed.
