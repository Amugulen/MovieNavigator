using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Tests.TestDoubles;

public sealed class FakeVideoInspector : IVideoInspector
{
    private readonly IReadOnlyDictionary<string, VideoInspectionResult> _results;

    public FakeVideoInspector(IReadOnlyDictionary<string, VideoInspectionResult> results)
    {
        _results = results;
    }

    public Task<VideoInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken)
    {
        return Task.FromResult(_results[filePath]);
    }
}
