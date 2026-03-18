using TSISP003.Domain.Interfaces;

namespace TSISP003.Domain.Entities;

public class RejectReply : ISignResponse
{
    public byte RejectedMiCode { get; set; }
    public byte ApplicationErrorCode { get; set; }
}
