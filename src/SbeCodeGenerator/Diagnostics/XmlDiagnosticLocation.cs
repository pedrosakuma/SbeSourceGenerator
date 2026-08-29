using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Xml;

namespace SbeSourceGenerator.Diagnostics
{
    /// <summary>
    /// Creates Roslyn <see cref="Location"/> instances for XML additional files.
    /// </summary>
    internal static class XmlDiagnosticLocation
    {
        public static Location Create(SourceText? sourceText, string? filePath, int lineNumber, int linePosition, int width = 1)
        {
            if (sourceText == null || string.IsNullOrWhiteSpace(filePath))
                return Location.None;

            string safeFilePath = filePath!;

            if (lineNumber <= 0 || linePosition <= 0)
                return Location.None;

            int lineIndex = lineNumber - 1;
            if (lineIndex >= sourceText.Lines.Count)
                return Location.None;

            var line = sourceText.Lines[lineIndex];
            int columnIndex = Math.Max(0, Math.Min(linePosition - 1, line.Span.Length));
            int start = line.Start + columnIndex;
            int remaining = line.End - start;
            int safeWidth = remaining == 0 ? 0 : Math.Max(1, Math.Min(width, remaining));

            var span = new TextSpan(start, safeWidth);
            var startPosition = new LinePosition(lineIndex, columnIndex);
            var endPosition = new LinePosition(lineIndex, columnIndex + safeWidth);
            return Location.Create(safeFilePath, span, new LinePositionSpan(startPosition, endPosition));
        }

        public static Location CreateFromLineInfo(SourceText? sourceText, string? filePath, IXmlLineInfo lineInfo, int width = 1)
        {
            if (lineInfo == null || !lineInfo.HasLineInfo())
                return Location.None;

            return Create(sourceText, filePath, lineInfo.LineNumber, lineInfo.LinePosition, width);
        }

        public static Location CreateFromException(SourceText? sourceText, string? filePath, XmlException exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return Create(sourceText, filePath, exception.LineNumber, exception.LinePosition);
        }
    }
}
