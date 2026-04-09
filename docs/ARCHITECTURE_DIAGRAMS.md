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
│ + Generate(ns, xmlDoc, context)        │
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
│  │    TypesCodeGeneratorTests (4 tests)           │    │
│  │  • Generate_WithSimpleEnum_ProducesEnumCode    │    │
│  │  • Generate_WithSimpleType_ProducesTypeCode    │    │
│  │  • Generate_WithComposite_ProducesCompositeCode│    │
│  │  • Generate_WithSet_ProducesSetCode            │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  MessagesCodeGeneratorTests (3 tests)          │    │
│  │  • Generate_WithSimpleMessage_ProducesCode     │    │
│  │  • Generate_WithConstants_ProducesConstants    │    │
│  │  • Generate_WithMultiple_ProducesMultiple      │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  UtilitiesCodeGeneratorTests (1 test)           │    │
│  │  • Generate_UsesProvidedNamespace              │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  MessageParsingHelpersTests (2 tests)          │    │
│  │  • MessagesIncludeTryParseHelper               │    │
│  │  • CompositesIncludeTryParseHelper             │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│                     Total: 11 tests ✅                   │
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
│  UtilitiesCodeGen:    17 lines │
│  ICodeGenerator:      20 lines │
├────────────────────────────────┤
│  Total:              673 lines │ (better organized)
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
│  └──────┘  └────────┘  └─────────┘                    │
│                                                         │
│  ✅ Easy to maintain                                   │
│  ✅ Fully tested (11 tests)                           │
│  ✅ Clear responsibilities                            │
│  ✅ Modular design                                    │
│  ✅ High cohesion                                     │
└─────────────────────────────────────────────────────────┘
```
