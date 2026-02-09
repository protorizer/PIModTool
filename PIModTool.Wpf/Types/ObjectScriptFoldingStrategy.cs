using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Wpf.Types
{
    public class ObjectScriptFoldingStrategy: AbstractFoldingStrategy
    {
        protected override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            List<NewFolding> foldings = new List<NewFolding>();
            Stack<int> startBracketOffsets = new Stack<int>();

            string fileContents = document.Text;
            for (int i = 0; i < fileContents.Length; i++)
            {
                switch (fileContents[i])
                {
                    case '{':
                        startBracketOffsets.Push(i);
                        break;
                    case '}':
                        if( startBracketOffsets.Count == 0 )
                        {
                            break;
                        }
                        int startOffset = startBracketOffsets.Pop();
                        int startLine = document.GetLineByOffset(startOffset).LineNumber;
                        int endLine = document.GetLineByOffset(i).LineNumber;

                        if(endLine > startLine)
                        {
                            foldings.Add(new NewFolding(startOffset, i + 1)
                            {
                                Name = "..."
                            });
                        }
                        break;
                }
            }

            foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

            return foldings;
        }
    }
}
