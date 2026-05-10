using FluentAssertions;
using MovieNavigator.Core.FileOperations;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Tests.FileOperations;

public sealed class FileOperationPlannerTests
{
    [Fact]
    public void Move_plan_requires_confirmation_and_blocks_offline_source()
    {
        var plan = FileOperationPlan.Create(
            FileOperationType.Move,
            MediaLibraryType.Normal,
            @"D:\Movies\film.mkv",
            @"E:\Sorted\film.mkv",
            sourceIsOnline: false);

        plan.RequiresConfirmation.Should().BeTrue();
        plan.CanExecute.Should().BeFalse();
        plan.BlockReason.Should().Be("Source file is offline.");
    }

    [Fact]
    public void Adult_to_normal_transfer_requires_extra_confirmation()
    {
        var plan = FileOperationPlan.CreateLibraryTransfer(
            MediaLibraryType.Adult,
            MediaLibraryType.Normal,
            @"X:\Adult\film.mkv",
            @"D:\Movies\film.mkv",
            sourceIsOnline: true);

        plan.RequiresConfirmation.Should().BeTrue();
        plan.RequiresVisibilityBoundaryConfirmation.Should().BeTrue();
    }
}
