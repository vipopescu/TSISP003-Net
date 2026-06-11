namespace TSISP003.Application.DTOs;

public class AckReplyDto
{
}

public class RejectReplyDto
{
    public byte ApplicationErrorCode { get; set; }
    public string? ApplicationErrorDescription { get; set; }
}
