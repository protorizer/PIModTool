using PIModTool.Core.Types;
using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib
{
    public static class CodeFormatting
    {
        // Define code formatting rules here
        private static readonly Dictionary<FileType, CodeFormattingRules> _formattingRules = new Dictionary<FileType, CodeFormattingRules>
        {
            {
                FileType.ObjectScript, new CodeFormattingRules
                {
                    AutoPairChars = new Dictionary<char, char> { { '{', '}' } },
                    BlockChars = new char[] { '{' },
                    IndentChars = "  "
                }
            },
            {
                FileType.EventScript, new CodeFormattingRules
                {
                    BlockWords = new Dictionary<string, string> { { "Begin", "End" } }
                }
            }
        };
        public static CodeFormattingAction ProcessInput(string currentLine, string input, FileType fileType)
        {
            if(!_formattingRules.TryGetValue(fileType, out CodeFormattingRules? rules) || input.Length != 1)
            {
                return new CodeFormattingAction { PreCursorText = "\n" };
            }

            if(input == "\n")
            {
                string trimmedLine = currentLine.Trim();


                int indents = CountLeadingTabs(currentLine, rules.IndentChars);
                foreach(KeyValuePair<string, string> blockRule in rules.BlockWords)
                {
                    if (trimmedLine.StartsWith(blockRule.Key))
                    {
                        return new CodeFormattingAction { PreCursorText = "\n" + Indent(rules.IndentChars, indents+1), PostCursorText = "\n" + Indent(rules.IndentChars, indents) + blockRule.Value };
                    }
                }

                // Slightly hacky logic, but should work for and be flexible with simple syntax like objectscript
                foreach(char blockChar in rules.BlockChars)
                {
                    if (trimmedLine.Contains(blockChar))
                    {
                        return new CodeFormattingAction { PreCursorText = "\n" + Indent(rules.IndentChars, indents+1), PostCursorText = "\n" + Indent(rules.IndentChars, indents) };
                    }
                }

                // Copy indentation of last line if nothing else fits
                return new CodeFormattingAction { PreCursorText = "\n" + Indent(rules.IndentChars, indents) };
            }
            else if (rules.AutoPairChars.TryGetValue(input[0], out char pair))
            {
                return new CodeFormattingAction { PostCursorText = pair.ToString() };
            }

            return new CodeFormattingAction();
        }

        private static int CountLeadingTabs(string text, string indentChars)
        {
            if (string.IsNullOrEmpty(indentChars)) { return 0; }
            int count = 0;
            int unitLen = indentChars.Length;
            int i = 0;
            while (i + unitLen <= text.Length)
            {
                // Check if substring matches the indent unit
                if (text.AsSpan(i, unitLen).SequenceEqual(indentChars))
                {
                    count++;
                    i += unitLen;
                }
                else if (char.IsWhiteSpace(text[i]))
                {
                    // Skip stray whitespace (not full indent unit)
                    i++;
                }
                else
                {
                    break;
                }
            }

            return count;
        }

        private static string Indent(string indent, int amount)
        {
            return string.Concat(Enumerable.Repeat(indent, amount));
        }
    }
}
