using YFToolbox.App.Tests.Fakes;
using YFToolbox.App.ViewModels;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;
using YFToolbox.Features.Files.Views;
using YFToolbox.Features.Images.Views;

namespace YFToolbox.App.Tests;

public sealed class HomeViewModelTests
{
    private readonly FakeFileInspectionService _inspection = new();
    private readonly FakeActionSuggestionService _suggestions = new();
    private readonly FakeNavigationService _navigation = new();

    private HomeViewModel CreateSut() => new(_inspection, _suggestions, _navigation);

    [Fact]
    public async Task AddFilesAsyncAddsFileWithSuggestedActionsAndUpdatesStatus()
    {
        var descriptor = CreateDescriptor();
        _inspection.Result = [descriptor];
        _suggestions.Suggestions = [new ToolDescriptor("convert", nameof(AppStrings.Converter), FileCategory.Image, new HashSet<string>(), 0)];
        var sut = CreateSut();

        await sut.AddFilesAsync(["photo.png"], TestContext.Current.CancellationToken);

        var item = Assert.Single(sut.Files);
        Assert.Equal(descriptor, item.File);
        Assert.Equal(AppStrings.Converter, item.Actions);
        var expectedStatus = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            System.Text.CompositeFormat.Parse(AppStrings.FilesInspectedFormat),
            1);
        Assert.Equal(expectedStatus, sut.Status);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task AddFilesAsyncMarksUnsupportedFileWhenNoActionsAreSuggested()
    {
        _inspection.Result = [CreateDescriptor()];
        _suggestions.Suggestions = [];
        var sut = CreateSut();

        await sut.AddFilesAsync(["file.bin"], TestContext.Current.CancellationToken);

        Assert.Equal(AppStrings.UnsupportedFile, Assert.Single(sut.Files).Actions);
    }

    [Fact]
    public async Task AddFilesAsyncFlagsCorruptInspectionErrorAsWarning()
    {
        _inspection.Result = [CreateDescriptor(inspectionError: AppErrorCode.CorruptInput)];
        var sut = CreateSut();

        await sut.AddFilesAsync(["broken.png"], TestContext.Current.CancellationToken);

        Assert.Equal(AppStrings.CorruptImage, Assert.Single(sut.Files).Warning);
    }

    [Fact]
    public async Task AddFilesAsyncFlagsExtensionMismatchAsWarning()
    {
        _inspection.Result = [CreateDescriptor(extensionMatchesContent: false)];
        var sut = CreateSut();

        await sut.AddFilesAsync(["renamed.png"], TestContext.Current.CancellationToken);

        Assert.Equal(AppStrings.ExtensionMismatch, Assert.Single(sut.Files).Warning);
    }

    [Fact]
    public async Task AddFilesAsyncCatchesIoExceptionAndSurfacesMessageAsStatus()
    {
        _inspection.ThrowOnInspectMany = new IOException("disk unavailable");
        var sut = CreateSut();

        await sut.AddFilesAsync(["file.png"], TestContext.Current.CancellationToken);

        Assert.Equal("disk unavailable", sut.Status);
        Assert.False(sut.IsBusy);
        Assert.Empty(sut.Files);
    }

    [Fact]
    public void ClearCommandEmptiesFilesAndResetsStatus()
    {
        var sut = CreateSut();
        sut.Files.Add(new DashboardFileItem(CreateDescriptor(), "actions", string.Empty));

        sut.ClearCommand.Execute(null);

        Assert.Empty(sut.Files);
        Assert.Equal(AppStrings.Ready, sut.Status);
    }

    [Fact]
    public void OpenConverterCommandNavigatesToImageConverterView()
    {
        var sut = CreateSut();

        sut.OpenConverterCommand.Execute(null);

        Assert.Equal(typeof(ImageConverterView), _navigation.NavigatedType);
    }

    [Fact]
    public void OpenRenameCommandNavigatesToBatchRenameView()
    {
        var sut = CreateSut();

        sut.OpenRenameCommand.Execute(null);

        Assert.Equal(typeof(BatchRenameView), _navigation.NavigatedType);
    }

    [Fact]
    public void OpenUtilitiesCommandNavigatesToFileUtilitiesView()
    {
        var sut = CreateSut();

        sut.OpenUtilitiesCommand.Execute(null);

        Assert.Equal(typeof(FileUtilitiesView), _navigation.NavigatedType);
    }

    private static FileDescriptor CreateDescriptor(
        AppErrorCode? inspectionError = null,
        bool extensionMatchesContent = true) => new(
        FullPath: @"C:\images\photo.png",
        FileName: "photo.png",
        Extension: ".png",
        Size: 1024,
        LastWriteTime: DateTimeOffset.UtcNow,
        MimeType: "image/png",
        Category: FileCategory.Image,
        Confidence: DetectionConfidence.Signature,
        ExtensionMatchesContent: extensionMatchesContent,
        InspectionError: inspectionError);
}
