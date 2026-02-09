using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace PIModTool.Wpf.Types
{
    public class EventScriptFoldingStrategy: AbstractFoldingStrategy
    {
        private static readonly Regex BeginRegex = new(@"^\s*Begin\s+(\w+)", RegexOptions.Compiled);
        private static readonly Regex EndRegex = new(@"^\s*End\b(?!\w)", RegexOptions.Compiled);

        protected override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            List<NewFolding> foldings = new List<NewFolding>();
            Stack<int> startBlockOffsets = new Stack<int>();

            foreach(DocumentLine line in document.Lines)
            {
                string text = document.GetText(line).TrimEnd();
                Match isBegin = BeginRegex.Match(text);
                if (isBegin.Success)
                {
                    startBlockOffsets.Push(line.EndOffset);
                    continue;
                }

                var isEnd = EndRegex.Match(text);
                if (isEnd.Success && startBlockOffsets.Count() > 0)
                {
                    int startOffset = startBlockOffsets.Pop();
                    int startLine = document.GetLineByOffset(startOffset).LineNumber;
                    int endLine = line.LineNumber;

                    if (endLine > startLine)
                    {
                        foldings.Add(new NewFolding(startOffset, line.EndOffset)
                        {
                            Name = "..."
                        });
                    }
                }
            }

            foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

            return foldings;
        }
    }
}
