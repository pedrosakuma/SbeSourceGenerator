using System.Text;

namespace SbeSourceGenerator
{
    public interface IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0);
    }
}
