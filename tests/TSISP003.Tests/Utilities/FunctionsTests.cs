using TSISP003.Utilities;

namespace TSISP003.Tests.Utilities;

public class FunctionsTests
{
    [Fact]
    public void HexToAscii_ValidHex_ReturnsAsciiString()
    {
        // Arrange
        string hex = "48454C4C4F"; // "HELLO" in hex

        // Act
        string result = Functions.HexToAscii(hex);

        // Assert
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void AsciiToHex_ValidAscii_ReturnsHexString()
    {
        // Arrange
        string ascii = "HELLO";

        // Act
        string result = Functions.AsciiToHex(ascii);

        // Assert
        Assert.Equal("48454C4C4F", result);
    }

    [Fact]
    public void HexToAscii_OddLengthHex_ThrowsArgumentException()
    {
        // Arrange
        string hex = "48454C4C4"; // Odd length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Functions.HexToAscii(hex));
    }

    [Fact]
    public void PacketCRC_ValidBytes_ReturnsCrcString()
    {
        // Arrange
        byte[] data = System.Text.Encoding.ASCII.GetBytes("TEST");

        // Act
        string result = Functions.PacketCRC(data);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length); // CRC should be 4 hex characters
    }

    [Fact]
    public void GeneratePassword_ValidInputs_ReturnsPassword()
    {
        // Arrange
        string passwordSeed = "AB";
        string seedOffset = "10";
        string passwordOffset = "20";

        // Act
        string result = Functions.GeneratePassword(passwordSeed, seedOffset, passwordOffset);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length); // Password should be 4 hex characters
    }
}
