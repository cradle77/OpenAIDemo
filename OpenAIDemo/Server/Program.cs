using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Server.Queuing;
using System.Text;

namespace OpenAIDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.Configure<AzureConfig>(builder.Configuration.GetSection("Azure"));

            builder.Services.AddSingleton<IChatCompletionService>(sp =>
            {
                AzureConfig options = sp.GetRequiredService<IOptions<AzureConfig>>().Value;

                // A custom HttpClient can be provided to this constructor
                return new AzureOpenAIChatCompletionService(
                    options.OpenAi.ChatEngine,
                    options.OpenAi.OpenAiEndpoint,
                    options.OpenAi.OpenAiKey);
            });

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddHttpClient();
            builder.Services.AddQueueProcessor();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            Console.OutputEncoding = Encoding.UTF8;

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}