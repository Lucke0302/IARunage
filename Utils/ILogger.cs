namespace Runage.Utils;

public interface ILogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogSuccess(string message);
    void LogInfo(string message);
}