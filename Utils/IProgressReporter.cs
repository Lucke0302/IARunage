namespace Runage.Utils;

public interface IProgressReporter
{
    void ReportProgress(float percent, string message);
}