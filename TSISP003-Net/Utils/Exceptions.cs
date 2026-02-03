using TSISP003_Net.SignControllerDataStore.Entities;

namespace TSISP003_Net.Utils;

/// <summary>
/// Exception thrown when a sign request is rejected.
/// </summary>
public class SignRequestRejectedException : Exception
{
    /// <summary>
    /// Gets the reject reply details.
    /// </summary>
    public RejectReply RejectReply { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignRequestRejectedException"/> class.
    /// </summary>
    /// <param name="rejectReply">The reject reply object containing details.</param>
    public SignRequestRejectedException(RejectReply rejectReply)
        : base($"Sign request was rejected: {rejectReply.ApplicationErrorCode}") // Customize message based on RejectReply properties
    {
        RejectReply = rejectReply;
    }
}