namespace CastoPet.Tests;

internal static class TestRunner
{
    public static int Run(IEnumerable<TestCase> tests, TextWriter output)
    {
        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                output.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                output.WriteLine($"FAIL {test.Name}: {ex.Message}");
            }
        }

        return failures;
    }
}
