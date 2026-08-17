using Tedwren.Abstractions.Contracts.DemoData;

namespace Tedwren.Application.DemoData;

/// <summary>
/// Holds the live progress of the current (or last) demo seed/delete operation so the Product Admin portal can
/// poll it and drive a determinate progress indicator. Registered as a singleton because the seed runs in one
/// request while the dialog polls in another; access is guarded by a lock as those requests run concurrently.
/// </summary>
public sealed class DemoDataProgressState
{
    private readonly object _gate = new();
    private bool _running;
    private string? _operation;
    private string? _stage;
    private int _completed;
    private int _total;
    private string? _error;

    /// <summary>Marks the start of an operation with a known number of stages.</summary>
    public void Begin(string operation, int totalStages)
    {
        lock (_gate)
        {
            _running = true;
            _operation = operation;
            _stage = "Starting";
            _completed = 0;
            _total = Math.Max(1, totalStages);
            _error = null;
        }
    }

    /// <summary>Advances to the next stage, recording its label and marking the previous one complete.</summary>
    public void Advance(string stage)
    {
        lock (_gate)
        {
            if (_completed < _total)
            {
                _completed++;
            }
            _stage = stage;
        }
    }

    /// <summary>Marks the operation finished (all stages complete).</summary>
    public void Complete()
    {
        lock (_gate)
        {
            _running = false;
            _completed = _total;
            _stage = "Done";
        }
    }

    /// <summary>Records a failure and stops the operation.</summary>
    public void Fail(string error)
    {
        lock (_gate)
        {
            _running = false;
            _error = error;
        }
    }

    /// <summary>Returns an immutable snapshot of the current progress.</summary>
    public DemoDataProgress Snapshot()
    {
        lock (_gate)
        {
            var percent = _total == 0 ? 0 : (int)Math.Round(100.0 * _completed / _total);
            return new DemoDataProgress(_running, _operation, _stage, _completed, _total, Math.Clamp(percent, 0, 100), _error);
        }
    }
}
