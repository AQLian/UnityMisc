
public class NormalReport : IDisposable
{
    public static int MaxFieldCount { get; private set; } = 10;
    protected List<string> reserveFields = new List<string>();
    protected bool _disposed;

    public NormalReport(List<string> reserves)
    {
        reserves.ForEach(AddReserveField);
        OnStartRecord();
    }

    public void AddReserveField(string field)
    {
        if (_disposed)
        {
            return;
        }
        if (reserveFields.Count > MaxFieldCount)
        {
            return;
        }
        reserveFields.Add(field);
    }

    protected virtual void OnStartRecord() { }

    protected virtual void OnStopRecord() { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
    }
}

public class ReportWithElapsedTime : NormalReport
{
    private StopWatch stopWatch;

    public ReportWithElapsedTime(List<string> reserves):base(reserves){}

    protected override void OnStartRecord()
    {
        base.OnStartRecord();
        stopWatch = StopWatch.StartNew();
    }

    protected override void OnStopRecord()
    {
        stopWatch.Stop();
        var seconds = stopWatch.Elapsed.TotalSeconds;
        AddReserveField(seconds.ToString());
        base.OnStopRecord();
    }
}
