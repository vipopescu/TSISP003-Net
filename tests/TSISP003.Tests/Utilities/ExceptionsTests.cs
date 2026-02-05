using TSISP003.Domain.Entities;
using TSISP003.Utilities;

namespace TSISP003.Tests.Utilities;

public class ExceptionsTests
{
    [Fact]
    public void SignRequestRejectedException_Constructor_SetsRejectReply()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x02 };

        // Act
        var exception = new SignRequestRejectedException(rejectReply);

        // Assert
        Assert.Same(rejectReply, exception.RejectReply);
    }

    [Fact]
    public void SignRequestRejectedException_Constructor_SetsMessage()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x02 };

        // Act
        var exception = new SignRequestRejectedException(rejectReply);

        // Assert
        Assert.Contains("Sign request was rejected", exception.Message);
        Assert.Contains("2", exception.Message); // Error code in message
    }

    [Fact]
    public void SignRequestRejectedException_IsException()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x02 };

        // Act
        var exception = new SignRequestRejectedException(rejectReply);

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void SignRequestRejectedException_CanBeThrown()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x21 }; // Incorrect password

        // Act & Assert
        var thrown = Assert.Throws<SignRequestRejectedException>((Action)(() =>
        {
            throw new SignRequestRejectedException(rejectReply);
        }));

        Assert.Equal(0x21, thrown.RejectReply.ApplicationErrorCode);
    }

    [Fact]
    public void SignRequestRejectedException_CanBeCaught()
    {
        // Arrange
        var rejectReply = new RejectReply { ApplicationErrorCode = 0x05 };
        RejectReply? caughtReply = null;

        // Act
        try
        {
            throw new SignRequestRejectedException(rejectReply);
        }
        catch (SignRequestRejectedException ex)
        {
            caughtReply = ex.RejectReply;
        }

        // Assert
        Assert.NotNull(caughtReply);
        Assert.Equal(0x05, caughtReply.ApplicationErrorCode);
    }
}
