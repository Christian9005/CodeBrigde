using Microsoft.AspNetCore.Components;
using CodeBridge.Hosting;

namespace CodeBridge.Blazor;

/// <summary>Base of every CodeBridge component: gives access to the shared board and re-renders when its state changes.</summary>
public abstract class CodeBridgeComponentBase : ComponentBase, IAsyncDisposable
{
    private bool _subscribed;

    [Inject]
    protected BoardService Board { get; set; } = default!;

    /// <summary>The last error of this component, shown by the component and cleared on the next success.</summary>
    protected string? Error { get; set; }

    protected override void OnInitialized()
    {
        Board.Changed += OnBoardChanged;
        _subscribed = true;
    }

    private void OnBoardChanged() => _ = InvokeAsync(StateHasChanged);

    public virtual ValueTask DisposeAsync()
    {
        if (_subscribed)
            Board.Changed -= OnBoardChanged;

        _subscribed = false;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>A component that reads a value from the board on a timer and shows it. The timer stops when the component is removed.</summary>
public abstract class PollingComponentBase<T> : CodeBridgeComponentBase
{
    private readonly CancellationTokenSource _stop = new();
    private Task? _loop;

    /// <summary>Milliseconds between reads.</summary>
    [Parameter]
    public int Interval { get; set; } = 500;

    protected T? Value { get; private set; }

    protected bool HasValue { get; private set; }

    protected abstract Task<T> ReadAsync(CancellationToken ct);

    /// <summary>Called on the render thread after each successful read.</summary>
    protected virtual void OnValue(T value)
    {
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            _loop = Task.Run(() => PollAsync(_stop.Token));
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Clamp(Interval, 20, 60_000)));
        try
        {
            do
            {
                try
                {
                    var value = await ReadAsync(ct);
                    await InvokeAsync(() =>
                    {
                        Value = value;
                        HasValue = true;
                        Error = null;
                        OnValue(value);
                        StateHasChanged();
                    });
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    await InvokeAsync(() =>
                    {
                        Error = ex.Message;
                        StateHasChanged();
                    });
                }
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException)
        {
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        if (_loop is not null)
        {
            try { await _loop; } catch (Exception) { }
        }

        _stop.Dispose();
        await base.DisposeAsync();
    }
}
