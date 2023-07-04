namespace Yarn.Compiler.Upgrader
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    public class FormatFunctionUpgrader : ILanguageUpgrader
    {
        public UpgradeResult Upgrade(UpgradeJob upgradeJob)
        {
            List<UpgradeResult.OutputFile> outputFiles = new List<UpgradeResult.OutputFile>();

            foreach (CompilationJob.File file in upgradeJob.Files)
            {
                List<TextReplacement> replacements = new List<TextReplacement>();

                ICharStream input = CharStreams.fromstring(file.Source);
                YarnSpinnerV1Lexer lexer = new YarnSpinnerV1Lexer(input);
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                YarnSpinnerV1Parser parser = new YarnSpinnerV1Parser(tokens);

                YarnSpinnerV1Parser.DialogueContext tree = parser.dialogue();

                ParseTreeWalker walker = new ParseTreeWalker();

                FormatFunctionListener formatFunctionListener = new FormatFunctionListener(file.Source, parser, (replacement) => replacements.Add(replacement));

                walker.Walk(formatFunctionListener, tree);

                outputFiles.Add(new UpgradeResult.OutputFile(file.FileName, replacements, file.Source));
            }

            return new UpgradeResult
            {
                Files = outputFiles,
            };
        }

        private class FormatFunctionListener : YarnSpinnerV1ParserBaseListener
        {
            private string contents;
            private YarnSpinnerV1Parser parser;
            private Action<TextReplacement> replacementCallback;

            public FormatFunctionListener(string contents, YarnSpinnerV1Parser parser, Action<TextReplacement> replacementCallback)
            {
                this.contents = contents;
                this.parser = parser;
                this.replacementCallback = replacementCallback;
            }

            public override void ExitFormat_function(YarnSpinnerV1Parser.Format_functionContext context)
            {
                // V1: [select {$gender} male="male" female="female" other="other"]
                //  function_name: "select" variable: "$gender" key_value_pair="male="male"..."
                //
                // V2: [select value={$gender} male="male" female="female" other="other"/]
                string formatFunctionType = context.function_name?.Text;
                YarnSpinnerV1Parser.VariableContext variableContext = context.variable();

                if (formatFunctionType == null || variableContext == null) {
                    // Not actually a format function, but the parser may
                    // have misinterpreted it? Do nothing here.
                    return;
                }
                
                string variableName = variableContext.GetText();

                StringBuilder sb = new StringBuilder();
                sb.Append($"{formatFunctionType} value={{{variableName}}}");

                foreach (YarnSpinnerV1Parser.Key_value_pairContext kvp in context.key_value_pair())
                {
                    sb.Append($" {kvp.GetText()}");
                }

                sb.Append(" /");

                // '[' and ']' are tokens that wrap this format_function,
                // so we're just replacing its innards
                int originalLength = context.Stop.StopIndex + 1 - context.Start.StartIndex;
                int originalStart = context.Start.StartIndex;
                string originalText = this.contents.Substring(originalStart, originalLength);

                TextReplacement replacement = new TextReplacement()
                                              {
                                                  Start = context.Start.StartIndex,
                                                  StartLine = context.Start.Line,
                                                  OriginalText = originalText,
                                                  ReplacementText = sb.ToString(),
                                                  Comment = "Format functions have been replaced with markup.",
                                              };

                // Deliver the replacement!
                this.replacementCallback(replacement);
            }
        }
    }
}
