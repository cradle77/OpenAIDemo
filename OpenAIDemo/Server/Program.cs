using OpenAIDemo.Server.FunctionAdapters;
using OpenAIDemo.Server.InferenceProviders;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Shared;

namespace OpenAIDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.Configure<AzureConfig>(builder.Configuration.GetSection("Azure"));

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<IFunctionAdapter, WeatherFunctionAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, ShoppingAddAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, ShoppingGetListAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, ShoppingModifyAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, HotelSearchAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, HotelBookingAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, CsvGetColumnsAdapter>();
            builder.Services.AddTransient<IFunctionAdapter, CsvQueryAdapter>();
            builder.Services.AddSingleton<IFunctionHandler, FunctionHandler>();
            builder.Services.AddTransient<IInferenceProvider<ReviewDetails>, ReviewInferenceProvider>();

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