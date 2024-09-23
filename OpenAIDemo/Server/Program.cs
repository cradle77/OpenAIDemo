#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure;
using Azure.Search.Documents;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Server.Plugins;

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

            builder.Services.AddSingleton<ShoppingListPlugin>();
            builder.Services.AddSingleton<WeatherPlugin>();
            builder.Services.AddTransient<HotelPlugin>();
            builder.Services.AddTransient<DataAnalysisPlugin>();

            builder.Services.AddTransient<ITextEmbeddingGenerationService>((serviceProvider) =>
            {
                AzureConfig options = serviceProvider.GetRequiredService<IOptions<AzureConfig>>().Value;

                return new AzureOpenAITextEmbeddingGenerationService(
                    options.OpenAi.EmbedEngine,
                    options.OpenAi.OpenAiEndpoint,
                    options.OpenAi.OpenAiKey);
            });

            builder.Services.AddTransient<SearchClient>((ServiceProvider) =>
            {
                AzureConfig options = ServiceProvider.GetRequiredService<IOptions<AzureConfig>>().Value;

                return new SearchClient(
                    new Uri(options.Search.SearchUrl),
                    options.Search.IndexName,
                    new AzureKeyCredential(options.Search.SearchKey));
            });

            builder.Services.AddTransient<KernelPluginCollection>((serviceProvider) =>
                new KernelPluginCollection()
                {
                    KernelPluginFactory.CreateFromType<ShoppingListPlugin>("ShoppingList", serviceProvider),
                    KernelPluginFactory.CreateFromType<WeatherPlugin>("Weather", serviceProvider),
                    KernelPluginFactory.CreateFromType<HotelPlugin>("Hotel", serviceProvider)
                });

            builder.Services.AddTransient<Kernel>();

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

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}