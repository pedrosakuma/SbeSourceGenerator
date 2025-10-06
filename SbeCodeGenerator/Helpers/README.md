# XML Parsing Helpers

This directory contains helper utilities for safe and consistent XML attribute access.

## Purpose

These helpers were introduced to:
- Provide guard methods for safe XML attribute retrieval
- Eliminate repetitive null-checking and error handling code
- Standardize error messages across the codebase
- Reduce the risk of NullReferenceException errors

## Files

- **XmlParsingHelpers.cs**: Static utility class with extension methods for XmlElement

## Available Methods

### Basic Attribute Access
- **`GetAttributeOrEmpty(string attributeName)`**: Returns attribute value or empty string if not found
- **`GetAttributeOrDefault(string attributeName, string defaultValue)`**: Returns attribute value or specified default
- **`GetRequiredAttribute(string attributeName)`**: Returns attribute value or throws exception if missing

### Integer Attribute Access
- **`GetIntAttributeOrNull(string attributeName)`**: Returns nullable int, validates format
- **`GetIntAttributeOrDefault(string attributeName, int defaultValue)`**: Returns int with default fallback

### Attribute Existence
- **`HasAttribute(string attributeName)`**: Checks if attribute exists and has a value

### Inner Text Access
- **`GetInnerTextOrEmpty()`**: Returns inner text or empty string

## Usage Examples

### Before (Direct Access)
```csharp
var name = element.GetAttribute("name");
var offset = element.GetAttribute("offset") == "" ? null : int.Parse(element.GetAttribute("offset"));
if (element.GetAttribute("nullValue") == "")
{
    // ...
}
```

### After (Using Helpers)
```csharp
var name = element.GetAttributeOrEmpty("name");
var offset = element.GetIntAttributeOrNull("offset");
if (!element.HasAttribute("nullValue"))
{
    // ...
}
```

## Error Handling

All helper methods include:
- Null argument validation
- Descriptive error messages
- Proper exception types
- Context information (element name, attribute name)

## Design Principles

- **Null Safety**: All methods handle null inputs gracefully
- **Consistency**: All methods follow the same naming and behavior patterns
- **Clarity**: Method names clearly describe their behavior
- **Validation**: Type conversion methods validate input and provide clear errors
