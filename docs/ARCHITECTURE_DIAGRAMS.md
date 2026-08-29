# Generator Architecture Diagram

> Diagram counts reflect the codebase at the time of writing (see file line counts in
> `src/SbeCodeGenerator/`). They will drift as the generator evolves — treat them as
> illustrative, not authoritative. For a prose description of the pipeline, see
> [sbe-generator.md](./sbe-generator.md#generator-pipeline).

## Historical Context: Before Decomposition (v0.1)

The generator originally lived in a single ~535-line `SBESourceGenerator` class that mixed
schema parsing, type generation, message generation, and helper logic together — hard to
test and maintain. It was decomposed into the orchestrator + specialized generators shown
below.

## Current Architecture

```
                    ┌─────────────────────────┐
                    │  SBESourceGenerator     │
                    │  (Orchestrator)         │
                    │  (~397 lines)           │
                    └───────────┬─────────────┘
                                │
            ┌───────────┬───────┼───────┬───────────┬───────────┐
            │           │       │       │           │           │
            ▼           ▼       ▼       ▼           ▼           ▼
      ┌──────────┐┌──────────┐┌──────────┐┌──────────────┐┌──────────┐
      │  Types   ││ Messages ││Dispatcher││  Utilities   ││Validation│
      │Generator ││Generator ││Generator ││  Generator   ││Generator │
      └────┬─────┘└────┬─────┘└────┬─────┘└──────┬───────┘└────┬─────┘
           │           │           │             │             │
           ▼           ▼           ▼             ▼             ▼
     ┌───────────┐┌──────────────┐┌───────────┐┌──────────────┐┌───────────┐
     │ Types &   ││ Messages &   ││Dispatcher ││ SpanReader,  ││ Validation│
     │ Composites││ Parsing APIs ││& Handler  ││SpanWriter,   ││ extension │
     │           ││              ││ interface ││EndianHelpers ││ methods   │
     └───────────┘└──────────────┘└───────────┘└──────────────┘└───────────┘
```

## Component Details

### ICodeGenerator Interface

```
┌─────────────────────────────────────────┐
│          ICodeGenerator                 │
├─────────────────────────────────────────┤
│ + Generate(ns, schema, context)        │
│   : IEnumerable<(string, string)>      │
└─────────────────────────────────────────┘
                    △
                    │ implements
     ┌──────────┬───┼──────────┬──────────────┬──────────┐
     │          │   │          │              │          │
   ┌─▼────┐ ┌───▼───┐ ┌──▼───────┐ ┌────▼─────┐ ┌─▼──────────┐
   │Types │ │Messages│ │Dispatcher│ │Utilities │ │Validation  │
   │Gen.  │ │Gen.    │ │Gen.      │ │Gen.      │ │Generator   │
   └──────┘ └────────┘ └──────────┘ └──────────┘ └────────────┘
```

`DispatcherGenerator` and `ValidationGenerator` are invoked independently of `ICodeGenerator`
callers where per-schema conditions apply (dispatcher only when messages exist; validation only
when `minValue`/`maxValue` constraints are present), but both implement the same generation
contract as the others.

### Data Flow

```
XML Schema File
      │
      ▼
┌───────────────────────────────────────┐
│  SBESourceGenerator (entry point)     │
│  namespace derivation + SchemaContext │
└──────────────┬─────────────────────────┘
               │
               │ passes shared SchemaContext to each generator
               │
   ┌───────────┼───────────┬───────────────┬───────────────┐
   │           │           │               │               │
   ▼           ▼           ▼               ▼               ▼
┌─────────┐┌──────────┐┌──────────┐┌───────────────┐┌──────────────┐
│Types    ││Messages  ││Dispatcher││Utilities       ││Validation    │
│CodeGen  ││CodeGen   ││Generator ││CodeGen         ││Generator     │
│         ││          ││          ││                ││(optional)    │
│enums,   ││messages, ││ISbeMessa-││SpanReader,     ││Validate()/   │
│sets,    ││fields,   ││geHandler,││SpanWriter,     ││TryValidate()/│
│composi- ││groups,   ││SbeDispat-││EndianHelpers   ││CreateValida- │
│tes,     ││varData,  ││cher      ││                ││ted()         │
│types    ││VersionMap││          ││                ││              │
└────┬────┘└────┬─────┘└────┬─────┘└───────┬────────┘└──────┬───────┘
     │          │           │              │                │
     └──────────┴───────────┴──────────────┴────────────────┘
                              │
                              ▼
                  sourceContext.AddSource() per file
```

### Generator Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                   TypesCodeGenerator                        │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Simple types (primitives with custom names)              │
│ • Enums (with nullable variants) and sets (flag enums)      │
│ • Composites (structured/nested types, <ref> elements)      │
│ • Derived numeric constants on decimal composites           │
│ • Semantic type extensions                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                 MessagesCodeGenerator                        │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Message structures, fields (regular/optional/constant)     │
│ • Repeating groups (nested, foreach enumerators when simple) │
│ • Variable-length data (varData)                             │
│ • Per-message parsing/encoding helpers and {Msg}VersionMap    │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                 DispatcherGenerator                          │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Per-schema ISbeMessageHandler interface                   │
│ • Zero-cost, devirtualized SbeDispatcher.Dispatch<T>         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│               UtilitiesCodeGenerator                         │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • SpanReader (sequential zero-copy binary reading)           │
│ • SpanWriter, EndianHelpers                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                 ValidationGenerator (optional)                │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Validate()/TryValidate()/CreateValidated() extension       │
│   methods for types/messages with minValue/maxValue          │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   SBESourceGenerator                          │
│                     (Orchestrator)                            │
├─────────────────────────────────────────────────────────────┤
│ Responsibilities:                                            │
│ • Collect XML schema files (AdditionalFiles)                 │
│ • Derive namespace, create SchemaContext                      │
│ • Instantiate and run specialized generators                  │
│ • Register generated sources                                  │
└─────────────────────────────────────────────────────────────┘
```

### Code Size Reference

Approximate line counts in `src/SbeCodeGenerator/` (excluding schema DTOs, field/type
definition builders, and diagnostics — see [sbe-generator.md](./sbe-generator.md) for the
full module breakdown):

```
┌──────────────────────────────────────────┐
│  SBESourceGenerator.cs:            397   │ ← orchestrator
│  Generators/TypesCodeGenerator.cs: 585   │
│  Generators/MessagesCodeGenerator.cs:645 │
│  Generators/DispatcherGenerator.cs: 110  │
│  Generators/UtilitiesCodeGenerator.cs: 32│
│  Generators/ValidationGenerator.cs: 259  │
│  Generators/ICodeGenerator.cs:      23   │
└──────────────────────────────────────────┘
```

### Testing Structure

```
┌──────────────────────────────────────────────────────────┐
│              SbeCodeGenerator.Tests                      │
├──────────────────────────────────────────────────────────┤
│  Enums, sets, composites, types, messages, fields,       │
│  groups, varData, dispatcher, utilities, validation,      │
│  snapshot tests, and diagnostics coverage.                │
│                                                            │
│                193 unit tests ✅                           │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│           SbeCodeGenerator.IntegrationTests               │
├──────────────────────────────────────────────────────────┤
│  End-to-end: schema → generated code → compiles → runs.   │
│  Covers schema versioning, byte order, groups/varData,     │
│  dispatcher, and semantic type conversion scenarios.       │
│                                                            │
│                168 integration tests ✅                    │
└──────────────────────────────────────────────────────────┘
```

## See Also

- [sbe-generator.md](./sbe-generator.md) - Full developer guide: pipeline, data structures, extension points
- [TESTING_GUIDE.md](./TESTING_GUIDE.md) - Testing infrastructure and how to run tests
