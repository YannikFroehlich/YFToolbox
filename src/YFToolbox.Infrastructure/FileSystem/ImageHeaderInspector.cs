using System.Buffers.Binary;
using System.Text;

namespace YFToolbox.Infrastructure.FileSystem;

internal readonly record struct ImageHeaderInfo(
    int Width,
    int Height,
    int FrameCount,
    string MimeType,
    string Extension);

internal static class ImageHeaderInspector
{
    public static ImageHeaderInfo Inspect(string path, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            16 * 1024,
            FileOptions.SequentialScan);
        Span<byte> header = stackalloc byte[32];
        var read = stream.Read(header);
        cancellationToken.ThrowIfCancellationRequested();

        if (read >= 24 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return Validate(
                BinaryPrimitives.ReadInt32BigEndian(header[16..20]),
                BinaryPrimitives.ReadInt32BigEndian(header[20..24]),
                1,
                "image/png",
                "png");
        }

        if (read >= 26 && header[0] == (byte)'B' && header[1] == (byte)'M')
        {
            return Validate(
                BinaryPrimitives.ReadInt32LittleEndian(header[18..22]),
                Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(header[22..26])),
                1,
                "image/bmp",
                "bmp");
        }

        if (read >= 6 && BinaryPrimitives.ReadUInt16LittleEndian(header[..2]) == 0 &&
            BinaryPrimitives.ReadUInt16LittleEndian(header[2..4]) == 1)
        {
            stream.Position = 0;
            return InspectIcon(stream, cancellationToken);
        }

        if (read >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            stream.Position = 12;
            return InspectWebp(stream, cancellationToken);
        }

        if (read >= 2 && header[0] == 0xff && header[1] == 0xd8)
        {
            stream.Position = 2;
            return InspectJpeg(stream, cancellationToken);
        }

        throw new NotSupportedException("The image header is not supported.");
    }

    private static ImageHeaderInfo InspectIcon(Stream stream, CancellationToken cancellationToken)
    {
        Span<byte> header = stackalloc byte[6];
        ReadExactly(stream, header);
        var count = BinaryPrimitives.ReadUInt16LittleEndian(header[4..6]);
        if (count == 0 || count > 512)
        {
            throw new InvalidDataException("The icon has an invalid frame count.");
        }

        var bestWidth = 0;
        var bestHeight = 0;
        Span<byte> entry = stackalloc byte[16];
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadExactly(stream, entry);
            var width = entry[0] == 0 ? 256 : entry[0];
            var height = entry[1] == 0 ? 256 : entry[1];
            var length = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..12]);
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(entry[12..16]);
            if (length == 0 || offset < 6 + count * 16 || (long)offset + length > stream.Length)
            {
                throw new InvalidDataException("The icon frame table is invalid.");
            }

            if ((long)width * height > (long)bestWidth * bestHeight)
            {
                bestWidth = width;
                bestHeight = height;
            }
        }

        return Validate(bestWidth, bestHeight, count, "image/x-icon", "ico");
    }

    private static ImageHeaderInfo InspectJpeg(Stream stream, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[2];
        var dimensions = new byte[5];
        while (stream.Position < stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var markerPrefix = stream.ReadByte();
            if (markerPrefix != 0xff)
            {
                continue;
            }

            int marker;
            do { marker = stream.ReadByte(); } while (marker == 0xff);
            if (marker < 0)
            {
                break;
            }

            if (marker is 0xd8 or 0xd9 || marker is >= 0xd0 and <= 0xd7)
            {
                continue;
            }

            ReadExactly(stream, lengthBytes);
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);
            if (segmentLength < 2)
            {
                throw new InvalidDataException("The JPEG segment length is invalid.");
            }

            if (marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or 0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf)
            {
                ReadExactly(stream, dimensions);
                return Validate(
                    BinaryPrimitives.ReadUInt16BigEndian(dimensions.AsSpan(3, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(dimensions.AsSpan(1, 2)),
                    1,
                    "image/jpeg",
                    "jpg");
            }

            stream.Seek(segmentLength - 2, SeekOrigin.Current);
        }

        throw new InvalidDataException("The JPEG does not contain a supported frame header.");
    }

    private static ImageHeaderInfo InspectWebp(Stream stream, CancellationToken cancellationToken)
    {
        var width = 0;
        var height = 0;
        var frameCount = 0;
        var animationFlag = false;
        Span<byte> chunkHeader = stackalloc byte[8];
        var data = new byte[10];

        while (stream.Position + 8 <= stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadExactly(stream, chunkHeader);
            var id = Encoding.ASCII.GetString(chunkHeader[..4]);
            var length = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[4..8]);
            var payloadStart = stream.Position;
            if (payloadStart + length > stream.Length)
            {
                throw new InvalidDataException("The WebP chunk table is invalid.");
            }

            if (id == "VP8X" && length >= 10)
            {
                ReadExactly(stream, data.AsSpan());
                animationFlag = (data[0] & 0x02) != 0;
                width = 1 + ReadUInt24LittleEndian(data.AsSpan(4, 3));
                height = 1 + ReadUInt24LittleEndian(data.AsSpan(7, 3));
            }
            else if (id == "VP8 " && length >= 10 && width == 0)
            {
                ReadExactly(stream, data.AsSpan());
                if (!data.AsSpan(3, 3).SequenceEqual(new byte[] { 0x9d, 0x01, 0x2a }))
                {
                    throw new InvalidDataException("The WebP VP8 frame header is invalid.");
                }
                width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6, 2)) & 0x3fff;
                height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(8, 2)) & 0x3fff;
            }
            else if (id == "VP8L" && length >= 5 && width == 0)
            {
                ReadExactly(stream, data.AsSpan(0, 5));
                if (data[0] != 0x2f)
                {
                    throw new InvalidDataException("The WebP lossless frame header is invalid.");
                }
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(1, 4));
                width = 1 + (int)(bits & 0x3fff);
                height = 1 + (int)((bits >> 14) & 0x3fff);
            }
            else if (id == "ANMF")
            {
                frameCount++;
            }

            var next = payloadStart + length + (length & 1);
            stream.Position = next;
        }

        return Validate(width, height, animationFlag ? Math.Max(2, frameCount) : 1, "image/webp", "webp");
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> value) =>
        value[0] | value[1] << 8 | value[2] << 16;

    private static ImageHeaderInfo Validate(
        int width,
        int height,
        int frameCount,
        string mimeType,
        string extension)
    {
        if (width <= 0 || height <= 0 || frameCount <= 0)
        {
            throw new InvalidDataException("The image dimensions are invalid.");
        }

        return new ImageHeaderInfo(width, height, frameCount, mimeType, extension);
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read == 0)
            {
                throw new InvalidDataException("The image header is truncated.");
            }
            total += read;
        }
    }
}
