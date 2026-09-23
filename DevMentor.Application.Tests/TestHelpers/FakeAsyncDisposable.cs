namespace DevMentor.Application.Tests.TestHelpers;

public class FakeAsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}