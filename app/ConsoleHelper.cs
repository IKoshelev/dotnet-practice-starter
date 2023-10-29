namespace Practice.Csharp;

public static class ConsoleHelper
{
    // https://stackoverflow.com/questions/2743260/is-it-possible-to-write-to-the-console-in-colour-in-net
    public static string NORMAL = Console.IsOutputRedirected ? "" : "\x1b[39m";
    public static string RED = Console.IsOutputRedirected ? "" : "\x1b[91m";
    public static string GREEN = Console.IsOutputRedirected ? "" : "\x1b[92m";
    public static string BLUE = Console.IsOutputRedirected ? "" : "\x1b[94m";

    public static string Color(string color, string text) => $"{color}{text}{NORMAL}";

    public static T WithConsoleNotification<T>(
        string actionName,
        Func<T> actionFn)
    {
        var actionNameBlue = Color(BLUE, actionName);
        Console.WriteLine($"{actionNameBlue} start");
        var result = actionFn();
        Console.WriteLine($"{actionNameBlue} done");
        return result;
    }

    public static async Task RunCheck(
        string sectionName,
        Func<Task<bool>> checkFn)
    {
        var sectionNameBlue = Color(BLUE, sectionName);
        try
        {
            Console.WriteLine($"Running connection check for {sectionNameBlue}");
            var result = await checkFn();

            if (result)
            {
                Console.WriteLine($"{sectionNameBlue} connection check {Color(GREEN, "success")}");
            }
            else
            {
                Console.WriteLine($"{sectionNameBlue} connection check {Color(RED, "failed")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{sectionNameBlue} connection check threw {Color(RED, "exception")}. \r\n Exception message: {ex.Message}");
        }
    }
}
