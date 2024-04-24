using Dalby.Common.Extensions;
using Dalby.GCode.Statements;
using Dalby.GCode.Tokens;
using Dalby.GCode.Trivias;

namespace Dalby.GCode
{
    public class GCodeFormatter
    {
        public static GCodeFormatter Default { get; } = new();

        public bool NormaliseWhiteSpace { get; set; } = true;
        public bool RemoveAllComments { get; set; } = false;

        public IEnumerable<StatementSyntax> Format(IEnumerable<StatementSyntax> statements)
        {
            StatementSyntax? previousStatement = null;
            foreach (StatementSyntax statement in statements)
            {
                previousStatement = normaliseWhitespace(statement, previousStatement);
                yield return previousStatement;
            }
        }

        private StatementSyntax normaliseWhitespace(StatementSyntax statement, StatementSyntax? previousStatement)
        {
            if (statement is CommandStatementSyntax commandStatement) return normaliseStatementWhitespace(commandStatement);
            return statement;
        }

        private CommandStatementSyntax normaliseStatementWhitespace(CommandStatementSyntax commandStatement)
        {
            bool noArguments = commandStatement.Arguments is null || commandStatement.Arguments.Length == 0;

            CommandToken commandToken = (CommandToken)normaliseTokenWhitespace(commandStatement.Command, true, noArguments);
            if (noArguments) return new CommandStatementSyntax(commandToken);

            ArgumentToken[] argumentTokens = commandStatement.Arguments!
                .Select((token, i) => (ArgumentToken)normaliseTokenWhitespace(token, false, i == commandStatement.Arguments!.Length - 1))
                .ToArray();
            return new CommandStatementSyntax(commandToken, argumentTokens);
        }

        private Token normaliseTokenWhitespace(Token token, bool firstTokenOfStatement, bool lastTokenOfStatement)
        {
            TriviaSyntax[]? leadingTrivia = firstTokenOfStatement
                ? normaliseTrivia(token.LeadingTrivia)
                : normaliseTrivia(token.LeadingTrivia, null, null, typeof(EndOfLineTrivia), typeof(EndOfLineCommentTrivia));

            TriviaSyntax[]? trailingTrivia = lastTokenOfStatement
                ? normaliseTrivia(token.TrailingTrivia, new WhitespaceTrivia(1), new EndOfLineTrivia())
                : normaliseTrivia(token.TrailingTrivia, new WhitespaceTrivia(1), new WhitespaceTrivia(1), typeof(EndOfLineTrivia), typeof(EndOfLineCommentTrivia));

            return token
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
        }

        private static TriviaSyntax[]? normaliseTrivia(IEnumerable<TriviaSyntax>? source, TriviaSyntax? prefix = null, TriviaSyntax? suffix = null, params Type[] excludedTypes)
        {
            source ??= [];

            HashSet<Type> excludedTypeSet = new(excludedTypes) { typeof(WhitespaceTrivia) };

            // strip all whitespace && remove disallowed tokens
            List<TriviaSyntax> trivia = source
                .Where(trivia => !excludedTypeSet.Contains(trivia.GetType()))
                .ToList();

            // EndOfLineCommentTrivias must be immediatly followed by an EndOfLineTrivia
            for (int i = trivia.Count - 2; i >= 0; i--)
            {
                if (trivia[i] is EndOfLineCommentTrivia && trivia[i + 1] is not EndOfLineTrivia) trivia.Insert(i + 1, new EndOfLineTrivia());
            }

            // set up whitespace
            if (trivia.Any(t => t.Equals(new EndOfLineTrivia())))
            {
                trivia = trivia.Split(t => t.Equals(new EndOfLineTrivia()))
                    .Select(line => line.Join(new WhitespaceTrivia(1)))
                    .Join([new EndOfLineTrivia()])
                    .SelectMany(t => t)
                    .ToList();
            }
            else
            {
                trivia = trivia
                    .Join(new WhitespaceTrivia(1))
                    .ToList();
            }

            if (prefix is not null && (!trivia.FirstOrDefault()?.Equals(prefix) ?? false)) trivia.Insert(0, prefix);
            if (suffix is not null && (!trivia.LastOrDefault()?.Equals(suffix) ?? true)) trivia.Add(suffix);

            // if there is whitespace before an end of line trivia, remove it SHOULD NOT HAPPEN NOW
            for (int i = trivia.Count - 2; i >= 0; i--)
            {
                if (trivia[i] is WhitespaceTrivia && trivia[i + 1] is EndOfLineTrivia) trivia.RemoveAt(i);
            }

            // if there is whitespace after an end of line trivia, remove it SHOULD NOT HAPPEN NOW
            for (int i = trivia.Count - 2; i >= 0; i--)
            {
                if (trivia[i] is EndOfLineTrivia && trivia[i + 1] is WhitespaceTrivia) trivia.RemoveAt(i + 1);
            }

            return trivia.Count == 0 ? null : ([.. trivia]);
        }
    }
}
