using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Wpf.Types
{
    public abstract class AbstractFoldingStrategy
    {
        protected abstract IEnumerable<NewFolding> CreateNewFoldings(TextDocument document);

        public virtual void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            if (manager == null || document == null)
            {
                return;
            }

            IEnumerable<NewFolding> foldings = CreateNewFoldings(document);

            // TODO: -1 parameter is index of the first parsing error, and preserves code folds from after that
            // After syntax parsing is implemented, find a way to implement this feature
            manager.UpdateFoldings(foldings, -1);
        }
    }
}
