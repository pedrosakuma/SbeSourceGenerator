## Release 0.8.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SBE011 | SbeSourceGenerator | Warning | Set choice bit position exceeds encoding type width.
SBE012 | SbeSourceGenerator | Warning | Invalid SbeAssumeHostEndianness MSBuild property value.

### Changed Rules

Rule ID | New Category | New Severity | Old Severity | Notes
--------|--------------|--------------|--------------|------
SBE007 | SbeSourceGenerator | Info | Warning | Now informational — big-endian is fully supported with conditional byte swap.

## Release 0.3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SBE007 | SbeSourceGenerator | Warning | Non-native byte order.
SBE008 | SbeSourceGenerator | Error | Unresolved type reference.
SBE009 | SbeSourceGenerator | Warning | Invalid numeric constraint.
SBE010 | SbeSourceGenerator | Warning | Unknown primitive type fallback.

## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SBE001 | SbeSourceGenerator | Error | Invalid integer attribute value.
SBE002 | SbeSourceGenerator | Error | Missing required attribute.
SBE003 | SbeSourceGenerator | Error | Invalid enum flag value.
SBE004 | SbeSourceGenerator | Error | Malformed schema file.
SBE005 | SbeSourceGenerator | Warning | Unsupported schema construct.
SBE006 | SbeSourceGenerator | Error | Invalid type length.
