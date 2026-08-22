namespace CastoPet.Tests;

internal static class Program
{
    private static int Main()
    {
        var failures = 0;
        foreach (var test in TestSuite.Tests)
        {
            try
            {
                test.Test();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
            }
        }

        return failures;
    }
}
