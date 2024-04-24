namespace Dalby.GCode.Statements
{
    public static class StatementSyntaxExtensions
    {
        public static string ToFullText(this IEnumerable<StatementSyntax> statements) => string.Join(null, statements.Select(statement => statement.FullText));
    }
}
