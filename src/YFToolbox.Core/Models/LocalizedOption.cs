namespace YFToolbox.Core.Models;

public sealed record LocalizedOption<T>(T Value, string Label) where T : struct, Enum;
