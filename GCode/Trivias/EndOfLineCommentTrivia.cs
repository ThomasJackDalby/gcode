namespace Dalby.GCode.Trivias
{
    public record EndOfLineCommentTrivia(string Comment) : TriviaSyntax($"; {Comment}");
}