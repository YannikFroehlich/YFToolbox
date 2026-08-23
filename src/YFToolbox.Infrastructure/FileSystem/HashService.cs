using System.Security.Cryptography;
using System.Diagnostics;
using YFToolbox.Application.Contracts;

namespace YFToolbox.Infrastructure.FileSystem;

public sealed class HashService : IHashService
{
    public Task<string> ComputeSha256Async(
        string path,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        ComputeAsync(path, HashAlgorithmName.SHA256, progress, cancellationToken);

    public Task<string> ComputeMd5Async(
        string path,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        ComputeAsync(path, HashAlgorithmName.MD5, progress, cancellationToken);

    private static async Task<string> ComputeAsync(
        string path,
        HashAlgorithmName algorithm,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var incrementalHash = IncrementalHash.CreateHash(algorithm);
        var buffer = new byte[1024 * 1024];
        long processed = 0;
        var progressClock = Stopwatch.StartNew();
        while (true)
        {
            var count = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                break;
            }

            incrementalHash.AppendData(buffer, 0, count);
            processed += count;
            if (progressClock.ElapsedMilliseconds >= 100 || processed == info.Length)
            {
                progress?.Report(info.Length == 0 ? 1 : (double)processed / info.Length);
                progressClock.Restart();
            }
        }

        progress?.Report(1);

        return Convert.ToHexString(incrementalHash.GetHashAndReset()).ToLowerInvariant();
    }
}
