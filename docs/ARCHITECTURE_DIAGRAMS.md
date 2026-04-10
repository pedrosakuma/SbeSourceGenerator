# Generator Architecture Diagram

## Before Decomposition

```
┌─────────────────────────────────────────────────────────────┐
│                    SBESourceGenerator                       │
│                     (~332 lines)                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  • Initialize(IncrementalGeneratorInitializationContext)    │
│  • CollectXmlSchemaFiles()                                  │
│  • BuildTransformationPipeline()                            │
│  • RegisterSourceGeneration()                               │
│  • GetNameAndContent()                                      │
│                                                             │
│  Type Generation:                                           │
│  • GenerateTypes()                                          │
│  • GenerateType()                                           │
│  • GenerateEnum()                                           │
│  • GenerateSet()                                            │
│  • GenerateComposite()                                      │
│                                                             │
│  Message Generation:                                        │
│  • GenerateMessages()                                       │
│  • GenerateParser()                                         │
│                                                             │
│  Helper Methods:                                            │
│  • ToNativeType()                                           │
│  • IsPrimitiveType()                                        │
│  • IsNullable()                                             │
│  • GetTypeLength()                                          │
│  • GetUnderlyingType()                                      │
│  • InsertQuotationsIfNeeded()                               │
│  • GetNamespaceFromPath()                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## After Decomposition

```
                    ┌─────────────────────────┐
                    │  SBESourceGenerator     │
                    │  (Orchestrator)         │
                    │  (~332 lines)           │
                    └───────────┬─────────────┘
                                │
                  ┌─────────────┼─────────────┐
                  │             │             │
                  ▼             ▼             ▼
        ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
        │   Types      │ │  Messages    │ │  Utilities   │
        │  Generator   │ │  Generator   │ │  Generator   │
        └──────┬───────┘ └──────┬───────┘ └──────┬──────┘
               │                │               │
               │                │               │
               ▼                ▼               ▼
        ┌───────────┐    ┌──────────────┐  ┌──────────────┐
        │ Types &   │    │ Messages &   │  │ Shared       │
        │ Composites│    │ Parsing APIs │  │ Utilities     │
        └───────────┘    └──────────────┘  └──────────────┘
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
         ┌──────────┼──────────┬──────────┐
         │          │          │          │
         │          │          │          │
       ┌────▼────┐┌───▼────┐┌───▼────┐
       │ Types   ││Messages││Utilities│
       │Generator││Generator││Generator│
       └─────────┘└────────┘└─────────┘
```

### Data Flow

```
XML Schema File
      │
      ▼
┌─────────────────────────────────────┐
│  SBESourceGenerator.GetNameAndContent│
└──────────────┬──────────────────────┘
               │
               │ Creates SchemaContext
               │
               ├──────────────────────────────────┐
               │                                  │
               ▼                                  ▼
    ┌──────────────────┐              ┌──────────────────┐
    │ TypesCodeGenerator│              │MessagesCodeGenerator│
    │                  │              │                  │
    │ Generates:       │              │ Generates:       │
    │ • Types          │              │ • Messages       │
    │ • Enums          │              │ • Fields         │
    │ • Sets           │              │ • Groups         │
    │ • Composites     │              │ • Parsing helpers│
    └──────┬───────────┘              └─────┬────────────┘
           │                                │
           │                                │
           ▼                                ▼
    ┌──────────────┐              ┌──────────────┐
    │ Type Files   │              │Message Files │
    │ .cs          │              │ .cs          │
    └──────────────┘              └──────┬───────┘
                                         │
                                         ▼
                                 ┌──────────────┐
                                 │ Utility Files│
                                 │ .cs          │
                                 └──────────────┘
```

### Generator Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                   TypesCodeGenerator                        │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Simple types (primitives with custom names)              │
│ • Enums (with nullable variants)                           │
│ • Sets (bitflag types)                                     │
│ • Composites (structured types)                            │
│ • Semantic type extensions                                 │
│                                                             │
│ Helper Methods:                                             │
│ • ToNativeType()                                            │
│ • IsPrimitiveType()                                         │
│ • IsNullable()                                              │
│ • GetTypeLength()                                           │
│ • InsertQuotationsIfNeeded()                                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                 MessagesCodeGenerator                       │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Message structures                                        │
│ • Message fields (regular and optional)                    │
│ • Message constants                                         │
│ • Message groups                                            │
│ • Message data fields                                       │
│ • Emits per-message parsing helpers                        │
│                                                             │
│ Helper Methods:                                             │
│ • ToNativeType()                                            │
│ • GetUnderlyingType()                                       │
│ • GetTypeLength()                                           │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│               UtilitiesCodeGenerator                        │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Future utility code                                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   SBESourceGenerator                        │
│                     (Orchestrator)                          │
├─────────────────────────────────────────────────────────────┤
│ Responsibilities:                                           │
│ • Collect XML schema files                                 │
│ • Create SchemaContext                                     │
│ • Instantiate specialized generators                       │
│ • Coordinate generation process                            │
│ • Register generated sources                               │
└─────────────────────────────────────────────────────────────┘
```

### Testing Structure

```
┌──────────────────────────────────────────────────────────┐
│              SbeCodeGenerator.Tests                      │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │    TypesCodeGeneratorTests (37 tests)          │    │
│  │  • Enums, sets, composites, types              │    │
│  │  • Optional fields, deprecated, char arrays    │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  MessagesCodeGeneratorTests (19 tests)         │    │
│  │  • Messages, fields, groups, varData           │    │
│  │  • Nested groups, constants, deprecated        │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  UtilitiesCodeGeneratorTests (4 tests)          │    │
│  │  • SpanReader, SpanWriter, EndianHelpers       │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  + SnapshotTests, ValidationTests, etc.        │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│                Total: 172 unit tests ✅                  │
└──────────────────────────────────────────────────────────┘
```

### Code Size Comparison

```
Before:
┌────────────────────────────────────────┐
│  SBESourceGenerator: 535 lines (v0.1)  │
└────────────────────────────────────────┘

After (current):
┌────────────────────────────────────────┐
│  SBESourceGenerator:  332 lines        │ ← orchestrator
│  TypesCodeGenerator:  411 lines        │
│  MessagesCodeGen:     426 lines        │
│  UtilitiesCodeGen:     32 lines        │
│  ValidationGenerator: 259 lines        │
│  ICodeGenerator:       23 lines        │
├────────────────────────────────────────┤
│  Total:             1,483 lines        │ (modular, testable)
└────────────────────────────────────────┘
```

## Benefits Visualization

```
┌─────────────────────────────────────────────────────────┐
│                   BEFORE                                │
│                                                         │
│  ┌───────────────────────────────────────────────┐    │
│  │         Monolithic SBESourceGenerator         │    │
│  │                                               │    │
│  │  ❌ Hard to maintain                          │    │
│  │  ❌ Difficult to test                         │    │
│  │  ❌ Mixed responsibilities                    │    │
│  │  ❌ 535 lines of code                         │    │
│  │  ❌ Low cohesion                              │    │
│  └───────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘

                         ↓ Refactoring

┌─────────────────────────────────────────────────────────┐
│                   AFTER                                 │
│                                                         │
│  ┌──────────────────────────────────────────┐          │
│  │    Orchestrator (97 lines)               │          │
│  └───┬──────────┬──────────┬────────────────┘          │
│      │          │          │                            │
│      ▼          ▼          ▼                            │
│  ┌──────┐  ┌────────┐  ┌─────────┐                    │
│  │Types │  │Messages│  │Utilities│                    │
│  └──────┘  └────────┘  └─────────┘                    │
│                                                         │
│  ✅ Easy to maintain                                   │
│  ✅ Fully tested (291 tests)                          │
│  ✅ Clear responsibilities                            │
│  ✅ Modular design                                    │
│  ✅ High cohesion                                     │
└─────────────────────────────────────────────────────────┘
```
