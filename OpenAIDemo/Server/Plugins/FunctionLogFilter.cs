using Microsoft.SemanticKernel;
using System.Diagnostics;
using System.Text.Json;

namespace OpenAIDemo.Server.Plugins
{
    public class FunctionLogFilter : IFunctionInvocationFilter
    {
        string NL = Environment.NewLine; // shortcut
        string NORMAL = Console.IsOutputRedirected ? "" : "\x1b[39m";
        string RED = Console.IsOutputRedirected ? "" : "\x1b[91m";
        string GREEN = Console.IsOutputRedirected ? "" : "\x1b[92m";
        string YELLOW = Console.IsOutputRedirected ? "" : "\x1b[93m";
        string BLUE = Console.IsOutputRedirected ? "" : "\x1b[94m";
        string MAGENTA = Console.IsOutputRedirected ? "" : "\x1b[95m";
        string CYAN = Console.IsOutputRedirected ? "" : "\x1b[96m";
        string GREY = Console.IsOutputRedirected ? "" : "\x1b[97m";
        string BOLD = Console.IsOutputRedirected ? "" : "\x1b[1m";
        string NOBOLD = Console.IsOutputRedirected ? "" : "\x1b[22m";
        string UNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[4m";
        string NOUNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[24m";
        string REVERSE = Console.IsOutputRedirected ? "" : "\x1b[7m";
        string NOREVERSE = Console.IsOutputRedirected ? "" : "\x1b[27m";

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            Console.WriteLine($"Function {YELLOW}{BOLD}{context.Function.Name}{NOBOLD}{NORMAL} requested with parameters:");
            foreach (var parameter in context.Arguments)
            {
                Console.WriteLine($"{BOLD}{parameter.Key}{NOBOLD}: {parameter.Value}");
            }

            var watch = Stopwatch.StartNew();

            await next(context);

            watch.Stop();

            Console.WriteLine($"Function {YELLOW}{BOLD}{context.Function.Name}{NOBOLD}{NORMAL} completed in {watch.ElapsedMilliseconds}ms with result:");

            var resultSerialized = JsonSerializer.Serialize(context.Result.GetValue<object>(), new JsonSerializerOptions() { WriteIndented = true });
            Console.WriteLine(resultSerialized);
        }
    }
}
