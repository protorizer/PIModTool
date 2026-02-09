using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Class to hold sets of rules for "QOL" formatting in the SmallFEditor ex. autocompleting braces, indenting, etc.
namespace PIModTool.Core.Types
{
    public class CodeFormattingRules
    {
        // What character(s) the language uses to indent. Defaults to tab
        public string IndentChars = "\t";

        // Every time a character is typed, we check in this to pair it (used for things like braces)
        public Dictionary<char, char> AutoPairChars { get; set; } = new Dictionary<char, char>();

        // Every time we type a newline, we check if the current line ends with this to indent the new block (used for curly braces)
        public char[] BlockChars { get; set; } = new char[0];

        // Every time we type a newline, we check if the current line ends with this to pair it and indent the new block (used for EventScript Begin/End blocks)
        public Dictionary<string, string> BlockWords { get; set; } = new Dictionary<string, string>();
    }
}
