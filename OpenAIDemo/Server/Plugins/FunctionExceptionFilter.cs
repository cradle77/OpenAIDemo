#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.SemanticKernel;

namespace OpenAIDemo.Server.Plugins
{
    public class FunctionExceptionFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
			try
			{
				await next(context);
			}
			catch (Exception ex)
			{
                context.Result = new FunctionResult(context.Function, ex.Message);
            }
        }
    }
}
