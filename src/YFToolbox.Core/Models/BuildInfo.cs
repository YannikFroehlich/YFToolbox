namespace YFToolbox.Core.Models;

public sealed record BuildInfo(
    string SemanticVersion,
    string CommitSha,
    string Channel,
    DateTimeOffset? BuildTime)
{
    public string ShortCommit => CommitSha.Length > 7 ? CommitSha[..7] : CommitSha;
}
