using System.Text.Json;
using DotNetLabTutor.Core;
using DotNetLabTutor.Rag;
using DotNetLabTutor.Tools;
using DotNetLabTutor.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services
    .AddDotNetLabTutorCore()
    .AddDotNetLabTutorRag()
    .AddDotNetLabTutorTools();

// 配置 JSON 序列化选项（使用 PascalCase）
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null
};

// 确保 HttpClient 使用相同的 JSON 选项
builder.Services.AddSingleton(jsonOptions);

// 配置 HttpClient
builder.Services.AddHttpClient<ChatService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5203/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<SessionStateService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5203/api/");
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

var scope = app.Services.CreateScope();
var ragService = scope.ServiceProvider.GetRequiredService<DotNetLabTutor.Core.Abstractions.IRagService>();
await ragService.InitializeAsync();

app.Run();