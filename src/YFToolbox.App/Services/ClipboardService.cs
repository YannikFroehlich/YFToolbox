using System.Windows;
using YFToolbox.Application.Contracts;

namespace YFToolbox.App.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text) => Clipboard.SetText(text);
}
