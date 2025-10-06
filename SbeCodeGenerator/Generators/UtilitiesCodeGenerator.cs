using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates utility code (e.g., NumberExtensions).
    /// </summary>
    public class UtilitiesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context)
        {
            StringBuilder sb = new StringBuilder();
            new NumberExtensions(ns).AppendFileContent(sb);
            yield return ($"Utilities\\NumberExtensions", sb.ToString());
        }
    }
}
