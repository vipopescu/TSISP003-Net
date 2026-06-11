using TSISP003.Domain.Entities;

namespace TSISP003.Domain.Exceptions;

public class SignRequestRejectedException : Exception
{
    public RejectReply RejectReply { get; }

    public SignRequestRejectedException(RejectReply rejectReply)
        : base($"Sign request rejected. Error code: {rejectReply.ApplicationErrorCode}")
    {
        RejectReply = rejectReply;
    }
}
