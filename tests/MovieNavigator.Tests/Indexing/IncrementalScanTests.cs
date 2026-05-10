using FluentAssertions;
using MovieNavigator.Core.Indexing;
using MovieNavigator.Core.Media;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Indexing;

public sealed class IncrementalScanTests
{
    [Fact]
    public async Task Scan_root_repository_persists_enabled_roots()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var repository = new SqliteScanRootRepository(factory);
        var root = new ScanRoot(@"D:\Movies", MediaLibraryType.Normal, IsEnabled: true, LastScanAt: null);

        await repository.UpsertAsync(root, CancellationToken.None);
        await repository.UpdateLastScanAtAsync(root.Path, DateTimeOffset.Parse("2026-05-10T10:00:00+00:00"), CancellationToken.None);

        var reloaded = await new SqliteScanRootRepository(factory).GetEnabledAsync(MediaLibraryType.Normal, CancellationToken.None);

        reloaded.Should().ContainSingle();
        reloaded[0].Path.Should().Be(root.Path);
        reloaded[0].LastScanAt.Should().Be(DateTimeOffset.Parse("2026-05-10T10:00:00+00:00"));
    }
}
