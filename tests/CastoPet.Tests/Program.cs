namespace CastoPet.Tests;

internal static class Program
{
    private static int Main()
    {
        return TestRunner.Run(TestSuite.Tests, Console.Out);
    }
}
