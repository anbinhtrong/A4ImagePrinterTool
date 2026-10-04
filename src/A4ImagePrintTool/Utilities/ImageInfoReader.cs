namespace A4ImagePrintTool.Utilities;

/// <summary>
/// Lightweight binary header reader to extract width and height of PNG and JPEG images
/// without introducing external imaging libraries.
/// </summary>
public static class ImageInfoReader
{
    public record ImageDimensions(int WidthPx, int HeightPx);

    /// <summary>
    /// Reads the pixel dimensions from a PNG or JPEG file.
    /// </summary>
    public static ImageDimensions ReadDimensions(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Image file not found: {filePath}", filePath);
        }

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream);

        byte b0 = reader.ReadByte();
        byte b1 = reader.ReadByte();

        // 1. Check PNG (Magic: 0x89 'P' 'N' 'G' 0x0D 0x0A 0x1A 0x0A)
        if (b0 == 0x89 && b1 == 0x50)
        {
            byte b2 = reader.ReadByte();
            byte b3 = reader.ReadByte();
            if (b2 == 0x4E && b3 == 0x47)
            {
                // In PNG, the IHDR chunk starts at byte offset 12.
                // Bytes 16-19: Width (32-bit big endian integer)
                // Bytes 20-23: Height (32-bit big endian integer)
                stream.Seek(16, SeekOrigin.Begin);
                int width = ReadBigEndianInt32(reader);
                int height = ReadBigEndianInt32(reader);
                return new ImageDimensions(width, height);
            }
        }

        // 2. Check JPEG (Magic: 0xFF 0xD8)
        if (b0 == 0xFF && b1 == 0xD8)
        {
            return ReadJpegDimensions(stream, reader);
        }

        throw new NotSupportedException($"Unrecognized image format for file: '{filePath}'. Only PNG and JPEG are supported.");
    }

    private static ImageDimensions ReadJpegDimensions(Stream stream, BinaryReader reader)
    {
        while (stream.Position < stream.Length)
        {
            byte markerPrefix = reader.ReadByte();
            if (markerPrefix != 0xFF)
            {
                continue;
            }

            byte marker = reader.ReadByte();
            // Skip stuffing bytes (0xFF)
            while (marker == 0xFF && stream.Position < stream.Length)
            {
                marker = reader.ReadByte();
            }

            // SOF0 (Baseline), SOF1 (Extended), SOF2 (Progressive)
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                _ = ReadBigEndianUInt16(reader); // chunk length
                _ = reader.ReadByte();            // data precision
                int height = ReadBigEndianUInt16(reader);
                int width = ReadBigEndianUInt16(reader);
                return new ImageDimensions(width, height);
            }

            // SOS (Start of Scan) or EOI (End of Image) -> dimensions not found before entropy data
            if (marker is 0xDA or 0xD9)
            {
                break;
            }

            // Variable length markers have a 16-bit big-endian length following
            if ((marker >= 0xE0 && marker <= 0xEF) || marker == 0xDB || marker == 0xC4 || marker == 0xFE || marker == 0xDD)
            {
                ushort length = ReadBigEndianUInt16(reader);
                if (length > 2)
                {
                    stream.Seek(length - 2, SeekOrigin.Current);
                }
            }
        }

        throw new InvalidDataException("Could not locate SOF marker in JPEG stream.");
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    private static ushort ReadBigEndianUInt16(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }
}
