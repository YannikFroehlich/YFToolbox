using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;

namespace YFToolbox.App.Services;

public sealed class OutputConflictPrompt : IOutputConflictPrompt
{
    private static readonly CompositeFormat CollisionPromptMessage =
        CompositeFormat.Parse(AppStrings.CollisionPromptFormat);

    public OutputConflictPolicy Resolve(string targetPath) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CollisionPromptMessage,
                Path.GetFileName(targetPath));
            var result = System.Windows.MessageBox.Show(
                message,
                AppStrings.CollisionPromptTitle,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            return result switch
            {
                MessageBoxResult.Yes => OutputConflictPolicy.Overwrite,
                MessageBoxResult.No => OutputConflictPolicy.CreateUnique,
                _ => OutputConflictPolicy.Skip
            };
        });
}
