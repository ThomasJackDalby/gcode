﻿namespace Dalby.GCode.Trivias
{
    public record WhitespaceTrivia(int Length) : TriviaSyntax(new string(' ', Length));
}