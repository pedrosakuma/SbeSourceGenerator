# Generator Architecture Diagram

## Before Decomposition

```
┌─────────────────────────────────────────────────────────────┐
│                    SBESourceGenerator                       │
│                     (~535 lines)                            │
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
                    │  (~97 lines)            │
                    └───────────┬─────────────┘
                                │
                  ┌─────────────┼─────────────┐
                  │             │             │
                  ▼             ▼             ▼
        ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
        │   Types      │ │  Messages    │ │  Utilities   │
        │  Generator   │ │  Generator   │ │  Generator   │
        └──────┬───────┘ └──────┬───────┘ └──────────────┘
               │                │
               │                │
               │                ▼
               │         ┌──────────────┐
               │         │   Parser     │
               │         │  Generator   │
               │         └──────────────┘
               │
               │
    ┌──────────┴───────────────────┐
    │                              │
    ▼                              ▼
┌───────────┐                  ┌───────────┐
│   Enums   │                  │   Types   │
│   Sets    │                  │Composites │
└───────────┘                  └───────────┘
```

## Component Details

### ICodeGenerator Interface
```
┌─────────────────────────────────────────┐
│          ICodeGenerator                 │
├─────────────────────────────────────────┤
│ + Generate(ns, xmlDoc, context)        │
│   : IEnumerable<(string, string)>      │
└─────────────────────────────────────────┘
                    △
                    │ implements
         ┌──────────┼──────────┬──────────┐
         │          │          │          │
         │          │          │          │
    ┌────▼────┐┌───▼────┐┌───▼────┐┌───▼────┐
    │ Types   ││Messages││ Parser ││Utilities│
    │Generator││Generator││Generator││Generator│
    └─────────┘└────────┘└────────┘└─────────┘
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
    │ • Composites     │              │                  │
    └──────┬───────────┘              └─────┬────────────┘
           │                                │
           │                                ├────────────┐
           │                                │            │
           ▼                                ▼            ▼
    ┌──────────────┐              ┌──────────────┐ ┌──────────────┐
    │ Type Files   │              │Message Files │ │ParserCodeGen │
    │ .cs          │              │ .cs          │ │              │
    └──────────────┘              └──────────────┘ └──────┬───────┘
                                                          │
                                                          ▼
                                                   ┌──────────────┐
                                                   │ Parser File  │
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
│ • Delegates to parser generation                           │
│                                                             │
│ Helper Methods:                                             │
│ • ToNativeType()                                            │
│ • GetUnderlyingType()                                       │
│ • GetTypeLength()                                           │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                  ParserCodeGenerator                        │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • Wraps existing ParserGenerator                           │
│ • Provides ICodeGenerator interface                        │
│ • Special method GenerateParser() for messages             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│               UtilitiesCodeGenerator                        │
├─────────────────────────────────────────────────────────────┤
│ Handles:                                                    │
│ • NumberExtensions                                          │
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
│  │    TypesCodeGeneratorTests (4 tests)           │    │
│  │  • Generate_WithSimpleEnum_ProducesEnumCode    │    │
│  │  • Generate_WithSimpleType_ProducesTypeCode    │    │
│  │  • Generate_WithComposite_ProducesCompositeCode│    │
│  │  • Generate_WithSet_ProducesSetCode            │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  MessagesCodeGeneratorTests (4 tests)          │    │
│  │  • Generate_WithSimpleMessage_ProducesCode     │    │
│  │  • Generate_WithConstants_ProducesConstants    │    │
│  │  • Generate_WithMultiple_ProducesMultiple      │    │
│  │  • Generate_WithMessages_GeneratesParser       │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  UtilitiesCodeGeneratorTests (2 tests)         │    │
│  │  • Generate_ProducesNumberExtensions           │    │
│  │  • Generate_UsesProvidedNamespace              │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  ParserCodeGeneratorTests (2 tests)            │    │
│  │  • GenerateParser_WithMessages_ProducesCode    │    │
│  │  • GenerateParser_WithMultiple_IncludesAll     │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│                     Total: 12 tests ✅                   │
└──────────────────────────────────────────────────────────┘
```

### Code Size Comparison

```
Before:
┌────────────────────────────────┐
│  SBESourceGenerator: 535 lines │
└────────────────────────────────┘

After:
┌────────────────────────────────┐
│  SBESourceGenerator:  97 lines │ ← 82% reduction
│  TypesCodeGenerator: 364 lines │
│  MessagesCodeGen:    175 lines │
│  ParserCodeGen:       28 lines │
│  UtilitiesCodeGen:    17 lines │
│  ICodeGenerator:      20 lines │
├────────────────────────────────┤
│  Total:              701 lines │ (better organized)
└────────────────────────────────┘
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
│  └──────┘  └────┬───┘  └─────────┘                    │
│                 │                                       │
│                 ▼                                       │
│             ┌────────┐                                 │
│             │Parser  │                                 │
│             └────────┘                                 │
│                                                         │
│  ✅ Easy to maintain                                   │
│  ✅ Fully tested (12 tests)                           │
│  ✅ Clear responsibilities                            │
│  ✅ Modular design                                    │
│  ✅ High cohesion                                     │
└─────────────────────────────────────────────────────────┘
```
