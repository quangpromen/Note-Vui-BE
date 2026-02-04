using System;

namespace NoteVui.Application.Exceptions;

public class NoteLimitExceededException : Exception
{
    public NoteLimitExceededException(string message) : base(message)
    {
    }
}
