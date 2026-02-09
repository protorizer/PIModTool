using PIModTool.Lib.Types;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace PIModTool.Lib
{
    public static class SyntaxValidation
    {

        // TODO: Make SyntaxMarkerService actually comprehensible and add a hover tooltip to show the error
        // Current implementation is just cobbled together copy-pasted code from online
        // Try checking these https://stackoverflow.com/questions/11149907/showing-invalid-xml-syntax-with-avalonedit
        // https://stackoverflow.com/questions/22276759/avalonedit-show-syntax-error

        public static List<SyntaxError> Validate(string text, FileType type)
        {
            switch (type)
            {
                case FileType.XML:
                    return XMLValidation(text);
                case FileType.ObjectScript:
                    return ObjectScriptValidation(text);
                case FileType.EventScript:
                    return EventScriptValidation(text);
                default:
                    return new List<SyntaxError>();
            }
        }

        // For now, just checking if braces are correct and assignments have values
        // Messy asf, might refactor later
        private static List<SyntaxError> ObjectScriptValidation(string text)
        {
            List<SyntaxError> errors = new List<SyntaxError>();

            // Split text into lines by newlines, iterate through each character of each line
            // Reduce to single pass with a braceError bool to avoid checking braces if they're already known invalid
            int numOpenBraces = 0; // If < 0, short circuit pass with an error. if > 0 at end, add error to end of file
            bool braceError = false; // If true, don't bother checking rest of braces
            bool multiLineComment = false; // While true, ignore errors and only check for end of comment

            string[] lines = text.Split('\n');
            for(int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                bool unassignedVar = false; // Set to true when seeing an =, then false if we get a definition after
                bool singleLineComment = false; // Ignore rest of line when true
                int varIndex = 0;

                for (int i = 0; i < lines[lineNum].Length; i++)
                {
                    if(!singleLineComment && !multiLineComment)
                    {
                        switch (lines[lineNum][i])
                        {
                            case '{':
                                numOpenBraces++;
                                break;
                            case '}':
                                numOpenBraces--;
                                break;
                            case '=':
                                unassignedVar = true;
                                varIndex = i;
                                break;
                            case '*':
                                // Lazy but functional multiline comment detection
                                if(i > 0 && lines[lineNum][i-1] == '/')
                                {
                                    multiLineComment = true;
                                }
                                break;
                            case '/':
                                // Lazy but functional comment detection
                                if (i > 0 && lines[lineNum][i - 1] == '/')
                                {
                                    // Ignore rest of line
                                    singleLineComment = true;
                                }

                                break;
                            default:
                                if (!Char.IsWhiteSpace(lines[lineNum][i]) && unassignedVar)
                                {
                                    unassignedVar = false;
                                }
                                break;
                        }
                    }
                    else if (multiLineComment)
                    {
                        if (i > 0 && lines[lineNum][i] == '/' && lines[lineNum][i - 1] == '*')
                        {
                            // End multiline comment
                            multiLineComment = false;
                        }
                    }

                    // Add 1 to all lineNum and columns since they're 1 indexed on the actual doc

                    if (!braceError && numOpenBraces < 0)
                    {
                        errors.Add(new SyntaxError
                        {
                            Line = lineNum + 1,
                            Column = i + 1,
                            Length = 1,
                            Message = "Unexpected symbol '}'."
                        });
                        braceError = true;
                    }
                }
                if (unassignedVar)
                {
                    errors.Add(new SyntaxError
                    {
                        Line = lineNum + 1,
                        Column = varIndex + 1,
                        Length = 1,
                        Message = "Expected value after assignment operator '='."
                    });
                }
            }

            if(!braceError && numOpenBraces > 0)
            {
                errors.Add(new SyntaxError
                {
                    Line = lines.Length - 1,
                    Column = lines[lines.Length-1].Length-1,
                    Length = 1,
                    Message = "Expected symbol '}'."
                });
            }

            return errors;
        }

        // For now, just Begin/End block detection
        private static List<SyntaxError> EventScriptValidation(string text)
        {
            List<SyntaxError> errors = new List<SyntaxError>();
            Regex beginKeyword = new Regex(@"\bBegin\b");
            Regex endKeyword = new Regex(@"\bEnd\b");

            int numBegins = 0; // Equivalent to an open bracket
            bool blockError = false;
            bool multiLineComment = false;

            string[] lines = text.Split('\n');
            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                if (lines[lineNum].Contains("/*"))
                {
                    multiLineComment = true;
                }

                if (multiLineComment)
                {
                    if (lines[lineNum].Contains("*/"))
                    {
                        multiLineComment = false;
                        // Parse anything after the end of the comment if it exists
                        lines[lineNum] = lines[lineNum].Substring(0, lines[lineNum].IndexOf("*/") + 1); 
                    }
                }

                if (!multiLineComment)
                {
                    foreach(Match beginMatch in beginKeyword.Matches(lines[lineNum]))
                    {
                        numBegins++;
                    }
                    foreach(Match endMatch in endKeyword.Matches(lines[lineNum]))
                    {
                        numBegins--;
                        if(!blockError && numBegins < 0)
                        {
                            errors.Add(new SyntaxError
                            {
                                Line = lineNum + 1,
                                Column = endMatch.Index + 1,
                                Length = endMatch.Length,
                                Message = "Unexpected symbol 'End'."
                            });
                            blockError = true;
                        }
                    }
                }
            }

            if(!blockError && numBegins > 0)
            {
                errors.Add(new SyntaxError { 
                    Line = lines.Length,
                    Column = lines[lines.Length - 1].Length,
                    Length = 1,
                    Message = "Expected symbol 'End'."
                });
            }

            return errors;
        }

        private static List<SyntaxError> XMLValidation(string text)
        {
            List<SyntaxError> errors = new List<SyntaxError>();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(text);
            }
            catch (XmlException ex)
            {
                errors.Add(new SyntaxError
                {
                    Line = ex.LineNumber,
                    Column = ex.LinePosition,
                    Length = 1,
                    Message = ex.Message,
                });
            }

            return errors;
        }
    }
}
