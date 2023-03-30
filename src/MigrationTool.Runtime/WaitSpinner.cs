using System.ComponentModel;

namespace MigrationTool;

public class WaitSpinner : IDisposable
{
    private readonly BackgroundWorker worker;
    private bool showSpinner;

    public WaitSpinner()
    {
        this.worker = new BackgroundWorker();
        this.worker.WorkerSupportsCancellation = true;
        this.worker.DoWork += this.BackgroundJob;
        this.showSpinner = false;
        this.worker.RunWorkerAsync();
    }

    public void Start() => this.showSpinner = true;

    public void Stop() => this.showSpinner = false;

    void BackgroundJob(object? _, DoWorkEventArgs __)
    {
        var i = 0;
        while (true)
        {
            if (this.worker.CancellationPending) break;

            if (this.showSpinner)
            {
                i++;
                Console.CursorVisible = false;
                Console.Write((i % 4) switch
                {
                    0 => "|",
                    1 => "/",
                    2 => "-",
                    3 => "\\",
                    _ => throw new ArgumentOutOfRangeException()
                });

                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }

            Thread.Sleep(200);
        }
    }

    public void Dispose()
    {
        this.worker.Dispose();
        GC.SuppressFinalize(this);
    }
}