using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Wpf.Types
{
    public class MarkupFoldingStrategy: AbstractFoldingStrategy
    {
        private readonly XmlFoldingStrategy _xml = new XmlFoldingStrategy();

        // provide a trivial CreateNewFoldings so abstract base compiles (not used)
        protected override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document) => Enumerable.Empty<NewFolding>();

        public override void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            if (manager == null || document == null)
            {
                return;
            }
            _xml.UpdateFoldings(manager, document);
        }
    }
}
