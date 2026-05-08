namespace GraderTool.Ai.Parsing;

public sealed class AiReviewParseException : Exception
{
    public AiReviewParseException(string message)
        : base(message)
    {
    }

    public AiReviewParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
