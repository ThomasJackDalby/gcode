﻿namespace Dalby.GCode.Trivias
{
    public record CommentTrivia(string Comment) : TriviaSyntax($"({Comment})");
}