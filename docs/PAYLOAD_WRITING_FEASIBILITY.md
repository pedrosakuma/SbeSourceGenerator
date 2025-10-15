# SBE Payload Writing Support - Feasibility Study

**Date**: 2025-10-15  
**Status**: Analysis Complete  
**Related Issue**: Analyze changes needed to enable payload writing  

## Executive Summary

This document presents a comprehensive feasibility analysis for adding **payload writing (encoding)** support to the SBE Source Generator. Currently, the project is **100% focused on reading** (decoding/parsing), but the architecture can be extended to support writing with relatively small, incremental changes.

### Key Findings

✅ **Feasible**: Writing implementation is completely viable  
✅ **Solid Architecture**: Existing foundation is well-structured  
✅ **Minimal Breaking Changes**: Can be added incrementally  
📊 **Estimated Effort**: Medium (3-4 sprints for complete implementation)

---

## 1. Current Architecture Analysis

### 1.1 Read-Only Focus

The project currently implements **only read operations**:

**Current Generated Code:**
```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct TradeData
{
    [FieldOffset(0)]
    public long TradeId;
    
    [FieldOffset(8)]
    public long Price;
    
    // ONLY parsing methods exist
    public static bool TryParse(ReadOnlySpan<byte> buffer, 
                               out TradeData message, 
                               out ReadOnlySpan<byte> variableData)
    {
        var reader = new SpanReader(buffer);
        if (!reader.TryRead<TradeData>(out message))
        {
            variableData = default;
            return false;
        }
        variableData = reader.Remaining;
        return true;
    }
}
```

**Current Limitations for Writing:**

| Component | Status | Impact on Writing |
|-----------|--------|-------------------|
| `SpanReader` | ✅ Exists | Need symmetric `SpanWriter` |
| `EndianHelpers.Write*` | ✅ **Already implemented** | Ready for use! |
| Encoding methods | ❌ Missing | No `TryEncode`, `WriteTo`, etc. |
| Group writers | ❌ Missing | No API for writing repeating groups |
| Builders | ❌ Missing | No builder pattern for incremental construction |

### 1.2 Existing Components Useful for Writing

**Already Working Components:**

| Component | Status | Purpose |
|-----------|--------|---------|
| `EndianHelpers` | ✅ **Implemented** | Write methods already exist! |
| `[StructLayout(Explicit)]` | ✅ Implemented | Enables direct buffer copy |
| `SpanReader` | ✅ Implemented | Model for SpanWriter |
| Field offsets | ✅ Implemented | Essential for writing at correct offset |
| Type lengths | ✅ Implemented | Needed for size calculation |
| Blittable types | ✅ Implemented | Enables fast copy via MemoryMarshal |

**Missing Components:**

| Component | Priority | Effort |
|-----------|----------|--------|
| `SpanWriter` | 🔴 High | Medium (1 week) |
| `TryEncode` methods | 🔴 High | Medium (2 weeks) |
| Group writing API | 🟡 Medium | High (3 weeks) |
| Optional builders | 🟢 Low | Medium (2 weeks) |
| VarData encoding | 🟡 Medium | High (2-3 weeks) |

---

## 2. Proposed Design: Writing API

### 2.1 Main Approach: TryEncode Methods

**Recommended**: Add `TryEncode` methods to existing structs.

```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct TradeData
{
    [FieldOffset(0)]
    public long TradeId;
    
    [FieldOffset(8)]
    public long Price;
    
    public const int MESSAGE_SIZE = 16;
    
    // EXISTING: Parsing (reading)
    public static bool TryParse(ReadOnlySpan<byte> buffer, 
                               out TradeData message, 
                               out ReadOnlySpan<byte> variableData)
    {
        // ... existing implementation
    }
    
    // NEW: Encoding (writing)
    public bool TryEncode(Span<byte> buffer, out int bytesWritten)
    {
        if (buffer.Length < MESSAGE_SIZE)
        {
            bytesWritten = 0;
            return false;
        }
        
        var writer = new SpanWriter(buffer);
        writer.Write(this);
        bytesWritten = MESSAGE_SIZE;
        return true;
    }
}
```

**Advantages:**
- ✅ Doesn't break existing code
- ✅ Symmetric API with TryParse
- ✅ Zero allocations
- ✅ Safe (bounds checking)

### 2.2 SpanWriter: Symmetric to SpanReader

```csharp
/// <summary>
/// A ref struct providing sequential writing of binary data to a Span.
/// Symmetric counterpart to SpanReader for encoding SBE messages.
/// </summary>
public ref struct SpanWriter
{
    private Span<byte> _buffer;
    private int _position;
    
    public SpanWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }
    
    public readonly int BytesWritten => _position;
    public readonly int RemainingBytes => _buffer.Length - _position;
    public readonly Span<byte> Remaining => _buffer.Slice(_position);
    
    /// <summary>
    /// Writes a blittable structure to the buffer and advances position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWrite<T>(T value) where T : struct
    {
        int size = Unsafe.SizeOf<T>();
        if (RemainingBytes < size)
            return false;
        
        MemoryMarshal.Write(_buffer.Slice(_position), ref value);
        _position += size;
        return true;
    }
    
    /// <summary>
    /// Writes bytes to the buffer and advances position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (RemainingBytes < bytes.Length)
            return false;
        
        bytes.CopyTo(_buffer.Slice(_position));
        _position += bytes.Length;
        return true;
    }
    
    /// <summary>
    /// Skips bytes (for padding/alignment).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySkip(int count)
    {
        if (RemainingBytes < count)
            return false;
        
        _buffer.Slice(_position, count).Clear();
        _position += count;
        return true;
    }
    
    /// <summary>
    /// Writes using custom encoder delegate.
    /// </summary>
    public bool TryWriteWith<T>(SpanEncoder<T> encoder, T value)
    {
        if (encoder(Remaining, value, out int bytesWritten))
        {
            _position += bytesWritten;
            return true;
        }
        return false;
    }
}

public delegate bool SpanEncoder<T>(Span<byte> buffer, T value, out int bytesWritten);
```

**Features:**
- ✅ Symmetric API to SpanReader
- ✅ Zero allocations (ref struct)
- ✅ Custom encoder support (extensibility)
- ✅ Automatic bounds checking
- ✅ Automatic position tracking

---

## 3. Implementation Plan

### Phase 1: Foundation (Sprints 1-2)

**Goals:**
- Implement SpanWriter
- Add simple TryEncode methods
- Create basic tests

**Deliverables:**
- ✅ `Runtime/SpanWriter.cs` implemented
- ✅ Unit tests for SpanWriter (15 tests)
- ✅ TryEncode generation for simple messages
- ✅ Documentation: SPAN_WRITER_DESIGN.md

**Effort:** 2 sprints (4 weeks)

### Phase 2: Simple Messages (Sprint 3)

**Goals:**
- Encoding for messages without groups
- Encoding for messages with optional fields
- Round-trip testing

**Deliverables:**
- ✅ TryEncode for all message types
- ✅ Round-trip tests (25 tests)
- ✅ Documentation: ENCODING_GUIDE.md
- ✅ Example: SimpleEncodingExample

**Effort:** 1 sprint (2 weeks)

### Phase 3: Groups and VarData (Sprints 4-5)

**Goals:**
- Writing API for repeating groups
- VarData encoding support
- Advanced tests

**Deliverables:**
- ✅ Group writer API
- ✅ VarData encoding
- ✅ Complex tests (20 tests)
- ✅ Documentation: GROUPS_ENCODING.md

**Effort:** 2 sprints (4 weeks)

### Phase 4: Optimization & Documentation (Sprint 6)

**Goals:**
- Performance benchmarks
- Complete documentation
- Advanced examples

**Deliverables:**
- ✅ Benchmarks (5 tests)
- ✅ All guides updated
- ✅ Complex examples
- ✅ Blog post about encoding

**Effort:** 1 sprint (2 weeks)

---

## 4. Impact Assessment

### 4.1 Code Generation Changes

**Files to Modify:**

| File | Changes | Breaking? |
|------|---------|-----------|
| `Runtime/SpanWriter.cs` | New file | ❌ No |
| `MessagesCodeGenerator.cs` | Add TryEncode generation | ❌ No |
| `GroupDefinition.cs` | Add group writer API | ❌ No |
| `DataFieldDefinition.cs` | Add varData encoding | ❌ No |

### 4.2 Test Impact

**New Tests Required:**

| Category | Existing | New | Total |
|----------|----------|-----|-------|
| Unit (SpanWriter) | 0 | ~15 | 15 |
| Unit (Encoding) | 0 | ~20 | 20 |
| Integration (Round-trip) | 0 | ~25 | 25 |
| Performance | 0 | ~5 | 5 |
| **Total** | **0** | **~65** | **65** |

### 4.3 Documentation Impact

**New Documents:**
- ENCODING_GUIDE.md
- SPAN_WRITER_DESIGN.md
- GROUPS_ENCODING.md
- ROUNDTRIP_TESTING.md

**Updated Documents:**
- README.md (add encoding section)
- SBE_FEATURE_COMPLETENESS.md (mark encoding as implemented)
- SBE_IMPLEMENTATION_ROADMAP.md (update roadmap)
- TESTING_GUIDE.md (add encoding tests)

### 4.4 Examples Impact

**Updated Examples:**
- `PcapSbePocConsole` - Add encoding example

**New Examples:**
- `SimpleEncodingExample` - Basic encoding
- `RoundTripExample` - Encode + decode validation
- `PerformanceExample` - Encoding benchmarks

---

## 5. Risks and Mitigations

### 5.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking changes to existing API | Low | High | Add features, don't modify |
| Unsatisfactory performance | Medium | Medium | Benchmarks from phase 1 |
| Group complexity | High | Medium | Prototype early, iterate |
| Edge case bugs | Medium | High | Comprehensive tests, fuzzing |

### 5.2 Project Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Optimistic estimates | High | Medium | 20% buffer in timeline |
| Lack of user feedback | Medium | Medium | Preview releases, RFC |
| Scope creep | Medium | High | Well-defined MVP, clear phases |

---

## 6. Success Metrics

### 6.1 Technical Metrics

- ✅ **Zero allocations** in encoding hot path
- ✅ **>95% test coverage** for new code
- ✅ **<100ns** for simple message encoding
- ✅ **100% compatibility** with existing decoders

### 6.2 Quality Metrics

- ✅ Complete documentation (all guides created)
- ✅ 3+ working examples
- ✅ Zero regressions in existing tests
- ✅ API review approved by maintainers

### 6.3 Adoption Metrics

- ✅ 1+ real user using encoding
- ✅ Positive community feedback
- ✅ Bug issues <5% of features

---

## 7. Alternatives Considered

### 7.1 Alternative 1: Full Generated Code (Rejected)

**Description:** Generate complete encoding code without SpanWriter.

**Pros:**
- No SpanWriter dependency
- Maximum performance

**Cons:**
- ❌ Too much generated code (hard to debug)
- ❌ Difficult to maintain consistency
- ❌ Logic duplication

**Decision:** ❌ Rejected - prefer composition

### 7.2 Alternative 2: Builders Only (Rejected)

**Description:** Only builders, no direct TryEncode.

**Pros:**
- Automatic validation
- Fluent API

**Cons:**
- ❌ Unnecessary overhead for simple cases
- ❌ More generated code
- ❌ Less flexible

**Decision:** ❌ Rejected - TryEncode more flexible

### 7.3 Alternative 3: Hybrid (Chosen)

**Description:** TryEncode + SpanWriter + optional builders.

**Pros:**
- ✅ Flexibility
- ✅ Performance when needed
- ✅ Easy for simple cases

**Cons:**
- ⚠️ More generated code
- ⚠️ Multiple ways to do same thing

**Decision:** ✅ **Chosen** - best balance

---

## 8. Next Steps

### 8.1 Immediate (Next 2 Weeks)

1. ✅ **Approve this analysis** - Discussion with stakeholders
2. ⏳ **Create RFC** for encoding API
3. ⏳ **Prototype SpanWriter** - Basic implementation
4. ⏳ **Validate performance** - Initial benchmarks

### 8.2 Short Term (Next Month)

1. ⏳ Implement complete Phase 1
2. ⏳ Create preview release for feedback
3. ⏳ Write initial documentation
4. ⏳ Create simple example

### 8.3 Medium Term (Next 3 Months)

1. ⏳ Implement Phases 2-3
2. ⏳ Beta release with complete encoding
3. ⏳ Complete documentation
4. ⏳ Performance validation

---

## 9. Conclusion

Implementing SBE payload writing support is **completely feasible** and can be done in an **incremental, non-breaking** manner. The existing architecture is solid and provides a good foundation.

### Recommendations

1. ✅ **Proceed with implementation** following 4-phase plan
2. ✅ **Start with MVP** (Phases 1-2) to validate approach
3. ✅ **Collect feedback** via preview releases
4. ✅ **Maintain compatibility** with existing code

### Final Estimate

| Item | Estimate |
|------|----------|
| **Development** | 12 weeks (6 sprints) |
| **Testing** | Included in development |
| **Documentation** | 2 additional weeks |
| **Review & Polish** | 2 additional weeks |
| **Total** | **16 weeks (~4 months)** |

---

## Appendices

### A. References

- [SpanReader Implementation](../src/SbeCodeGenerator/Runtime/SpanReader.cs)
- [EndianHelpers Implementation](../src/SbeCodeGenerator/Generators/EndianHelpers.cs)
- [SBE Feature Completeness](./SBE_FEATURE_COMPLETENESS.md)
- [SBE Implementation Roadmap](./SBE_IMPLEMENTATION_ROADMAP.md)

### B. Related Documents

- [PAYLOAD_WRITING_ANALYSIS.md](./PAYLOAD_WRITING_ANALYSIS.md) - Portuguese version (more detailed)

### C. Contact

For discussions about this analysis:
- Open issue on GitHub
- Discuss in related Pull Request
- Contact maintainers

---

**Prepared By:** GitHub Copilot  
**Created:** 2025-10-15  
**Version:** 1.0  
**Status:** ✅ Complete - Awaiting Approval
