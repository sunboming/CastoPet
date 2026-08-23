namespace CastoPet.Application.Diagnostics;

public interface IApplicationLogger
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}
