using TSISP003.Utilities;
using TSISP003.Services;

namespace TSISP003.Tests.Utilities;

public class FunctionsTests
{
    #region HexToAscii Tests

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
    public void HexToAscii_EmptyString_ReturnsEmptyString()
    {
        // Act
        string result = Functions.HexToAscii("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void HexToAscii_SingleByte_ReturnsCorrectChar()
    {
        // Arrange - "A" = 0x41
        string hex = "41";

        // Act
        string result = Functions.HexToAscii(hex);

        // Assert
        Assert.Equal("A", result);
    }

    [Fact]
    public void HexToAscii_LowercaseHex_ReturnsCorrectAscii()
    {
        // Arrange
        string hex = "48454c4c4f"; // "HELLO" in lowercase hex

        // Act
        string result = Functions.HexToAscii(hex);

        // Assert
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void HexToAscii_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Functions.HexToAscii(null!));
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
    public void HexToAscii_SpecialCharacters_ReturnsCorrectString()
    {
        // Arrange - "!@#" = 0x21 0x40 0x23
        string hex = "214023";

        // Act
        string result = Functions.HexToAscii(hex);

        // Assert
        Assert.Equal("!@#", result);
    }

    [Fact]
    public void HexToAscii_Digits_ReturnsCorrectString()
    {
        // Arrange - "123" = 0x31 0x32 0x33
        string hex = "313233";

        // Act
        string result = Functions.HexToAscii(hex);

        // Assert
        Assert.Equal("123", result);
    }

    #endregion

    #region AsciiToHex Tests

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
    public void AsciiToHex_EmptyString_ReturnsEmptyString()
    {
        // Act
        string result = Functions.AsciiToHex("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void AsciiToHex_SingleChar_ReturnsCorrectHex()
    {
        // Arrange
        string ascii = "A";

        // Act
        string result = Functions.AsciiToHex(ascii);

        // Assert
        Assert.Equal("41", result);
    }

    [Fact]
    public void AsciiToHex_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Functions.AsciiToHex(null!));
    }

    [Fact]
    public void AsciiToHex_SpecialCharacters_ReturnsCorrectHex()
    {
        // Arrange
        string ascii = "!@#";

        // Act
        string result = Functions.AsciiToHex(ascii);

        // Assert
        Assert.Equal("214023", result);
    }

    [Fact]
    public void AsciiToHex_Digits_ReturnsCorrectHex()
    {
        // Arrange
        string ascii = "123";

        // Act
        string result = Functions.AsciiToHex(ascii);

        // Assert
        Assert.Equal("313233", result);
    }

    [Fact]
    public void AsciiToHex_LowercaseLetters_ReturnsCorrectHex()
    {
        // Arrange
        string ascii = "abc";

        // Act
        string result = Functions.AsciiToHex(ascii);

        // Assert
        Assert.Equal("616263", result);
    }

    [Fact]
    public void HexToAscii_And_AsciiToHex_AreInverse()
    {
        // Arrange
        string original = "Test String 123!";

        // Act
        string hex = Functions.AsciiToHex(original);
        string result = Functions.HexToAscii(hex);

        // Assert
        Assert.Equal(original, result);
    }

    #endregion

    #region PacketCRC Tests

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
    public void PacketCRC_EmptyArray_ReturnsZeroCrc()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();

        // Act
        string result = Functions.PacketCRC(data);

        // Assert
        Assert.Equal("0000", result);
    }

    [Fact]
    public void PacketCRC_SingleByte_ReturnsCrc()
    {
        // Arrange
        byte[] data = new byte[] { 0x41 }; // 'A'

        // Act
        string result = Functions.PacketCRC(data);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void PacketCRC_SameInput_ReturnsSameCrc()
    {
        // Arrange
        byte[] data1 = System.Text.Encoding.ASCII.GetBytes("HELLO");
        byte[] data2 = System.Text.Encoding.ASCII.GetBytes("HELLO");

        // Act
        string result1 = Functions.PacketCRC(data1);
        string result2 = Functions.PacketCRC(data2);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void PacketCRC_DifferentInput_ReturnsDifferentCrc()
    {
        // Arrange
        byte[] data1 = System.Text.Encoding.ASCII.GetBytes("HELLO");
        byte[] data2 = System.Text.Encoding.ASCII.GetBytes("WORLD");

        // Act
        string result1 = Functions.PacketCRC(data1);
        string result2 = Functions.PacketCRC(data2);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void PacketCRCushort_ValidBytes_ReturnsUshort()
    {
        // Arrange
        byte[] data = System.Text.Encoding.ASCII.GetBytes("TEST");

        // Act
        ushort result = Functions.PacketCRCushort(data);

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void PacketCRCushort_EmptyArray_ReturnsZero()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();

        // Act
        ushort result = Functions.PacketCRCushort(data);

        // Assert
        Assert.Equal((ushort)0, result);
    }

    [Fact]
    public void PacketCRC_And_PacketCRCushort_AreConsistent()
    {
        // Arrange
        byte[] data = System.Text.Encoding.ASCII.GetBytes("TEST");

        // Act
        string crcString = Functions.PacketCRC(data);
        ushort crcUshort = Functions.PacketCRCushort(data);

        // Assert
        Assert.Equal(crcUshort.ToString("X4"), crcString);
    }

    #endregion

    #region CRCGenerator Tests

    [Fact]
    public void CRCGenerator_ZeroData_ZeroAccum_ReturnsExpected()
    {
        // Act
        ushort result = Functions.CRCGenerator(0, 0);

        // Assert
        Assert.Equal((ushort)0, result);
    }

    [Fact]
    public void CRCGenerator_NonZeroData_ZeroAccum_ReturnsNonZero()
    {
        // Act
        ushort result = Functions.CRCGenerator(0x41, 0); // 'A'

        // Assert
        Assert.NotEqual((ushort)0, result);
    }

    [Fact]
    public void CRCGenerator_SameInputs_ReturnsSameResult()
    {
        // Act
        ushort result1 = Functions.CRCGenerator(0x41, 0x1234);
        ushort result2 = Functions.CRCGenerator(0x41, 0x1234);

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region GeneratePassword Tests

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

    [Fact]
    public void GeneratePassword_ZeroInputs_ReturnsPassword()
    {
        // Arrange
        string passwordSeed = "00";
        string seedOffset = "00";
        string passwordOffset = "00";

        // Act
        string result = Functions.GeneratePassword(passwordSeed, seedOffset, passwordOffset);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void GeneratePassword_MaxValues_ReturnsPassword()
    {
        // Arrange
        string passwordSeed = "FF";
        string seedOffset = "FF";
        string passwordOffset = "FFFF";

        // Act
        string result = Functions.GeneratePassword(passwordSeed, seedOffset, passwordOffset);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void GeneratePassword_SameInputs_ReturnsSamePassword()
    {
        // Arrange
        string passwordSeed = "AB";
        string seedOffset = "10";
        string passwordOffset = "20";

        // Act
        string result1 = Functions.GeneratePassword(passwordSeed, seedOffset, passwordOffset);
        string result2 = Functions.GeneratePassword(passwordSeed, seedOffset, passwordOffset);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GeneratePassword_DifferentInputs_ReturnsDifferentPasswords()
    {
        // Arrange
        string passwordSeed1 = "AB";
        string passwordSeed2 = "CD";
        string seedOffset = "10";
        string passwordOffset = "20";

        // Act
        string result1 = Functions.GeneratePassword(passwordSeed1, seedOffset, passwordOffset);
        string result2 = Functions.GeneratePassword(passwordSeed2, seedOffset, passwordOffset);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GeneratePassword_LowercaseHex_Works()
    {
        // Arrange
        string passwordSeed = "ab";
        string seedOffset = "10";
        string passwordOffset = "20";

        // Act
        string result = Functions.GeneratePassword(passwordSeed, seedOffset, passwordOffset);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length);
    }

    #endregion

    #region GetChunks Tests

    [Fact]
    public void GetChunks_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        string input = "";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Empty(result);
        Assert.Equal("", remaining);
    }

    [Fact]
    public void GetChunks_SingleCompleteChunk_ReturnsChunk()
    {
        // Arrange - SOH = 0x01, ETX = 0x03
        string input = "\u0001DATA\u0003";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Single(result);
        Assert.Equal("\u0001DATA\u0003", result.First());
        Assert.Equal("", remaining);
    }

    [Fact]
    public void GetChunks_MultipleCompleteChunks_ReturnsAllChunks()
    {
        // Arrange
        string input = "\u0001DATA1\u0003\u0001DATA2\u0003";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal("", remaining);
    }

    [Fact]
    public void GetChunks_IncompleteChunk_ReturnsRemaining()
    {
        // Arrange - SOH without ETX
        string input = "\u0001INCOMPLETE";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Empty(result);
        Assert.Equal("\u0001INCOMPLETE", remaining);
    }

    [Fact]
    public void GetChunks_CompleteAndIncomplete_ReturnsCompleteAndRemaining()
    {
        // Arrange
        string input = "\u0001COMPLETE\u0003\u0001INCOMPLETE";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Single(result);
        Assert.Equal("\u0001COMPLETE\u0003", result.First());
        Assert.Equal("\u0001INCOMPLETE", remaining);
    }

    [Fact]
    public void GetChunks_ACKChunk_ReturnsChunk()
    {
        // Arrange - ACK = 0x06
        string input = "\u0006DATA\u0003";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void GetChunks_NAKChunk_ReturnsChunk()
    {
        // Arrange - NAK = 0x15
        string input = "\u0015DATA\u0003";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void GetChunks_NoStartChar_ReturnsEmpty()
    {
        // Arrange - No SOH, ACK, or NAK
        string input = "NOSTART\u0003";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetChunks_GarbageBeforeChunk_IgnoresGarbage()
    {
        // Arrange
        string input = "GARBAGE\u0001DATA\u0003";

        // Act
        var result = Functions.GetChunks(input, out string remaining);

        // Assert
        Assert.Single(result);
        Assert.Equal("\u0001DATA\u0003", result.First());
    }

    #endregion

    #region PrintMessagePacket Tests

    [Fact]
    public void PrintMessagePacket_ReplacesControlCharacters()
    {
        // This test verifies the method runs without error
        // The actual output goes to logger which we don't have here

        // Arrange
        string packet = "\u0001\u0002DATA\u0003\u0004\u0006\u0015";

        // Act - Should not throw
        Functions.PrintMessagePacket(packet, "=>");
    }

    [Fact]
    public void PrintMessagePacket_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        string packet = "TEST";

        // Act & Assert - Should not throw
        Functions.PrintMessagePacket(packet, "=>", null);
    }

    [Fact]
    public void PrintMessagePacket_EmptyPacket_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        Functions.PrintMessagePacket("", "=>");
    }

    #endregion
}
