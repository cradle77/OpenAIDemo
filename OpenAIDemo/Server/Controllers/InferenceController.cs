#pragma warning disable SKEXP0010 

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAIDemo.Shared;
using System.Text.Json;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InferenceController : ControllerBase
    {
        private Kernel _kernel;

        public InferenceController(Kernel kernel)
        {
            _kernel = kernel;
            _kernel.Plugins.Clear();
        }

        [HttpPost()] // <-- ReviewDetails
        public async Task<IActionResult> InferReviewAsync([FromBody] string reviewText)
        {
            string prompt = string.Format(
@"You are an AI which helps storing review data. You will be prompted with a review text, between triple backticks.
For example a prompt could be like:
This is an example of a review:
```Hotel Corinthia was simply stunning```

You need to invoke the hotel-review function to store the review details.

This is the actual review to analyze:

```{0}```", reviewText);

            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(ReviewDetails)
            };

            var result = await _kernel.InvokePromptAsync(reviewText, new KernelArguments(executionSettings));

            Console.WriteLine(result);

            var review = JsonSerializer.Deserialize<ReviewDetails>(result.ToString());

            return this.Ok(review);
        }
    }
}
