using Microsoft.EntityFrameworkCore;
using PlanoraApp.API.Infrastructure;
using PlanoraApp.API.Services.TripService;
using PlanoraApp.API.Services.UserService;
using PlanoraApp.API.Services.BookingService;
using PlanoraApp.API.Services.VaultService;
using PlanoraApp.AI;
using PlanoraApp.AI.Agents;
using PlanoraApp.API.Workers;
using Azure.Messaging.ServiceBus;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(b => {
        b.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Cosmos DB & Trip Service
builder.Services.AddSingleton<CosmosDbClient>();
builder.Services.AddScoped<ITripService, TripService>();

// Service Bus integration
var serviceBusConn = builder.Configuration["AzureServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConn) && !serviceBusConn.Contains("PASTE_"))
{
    builder.Services.AddSingleton(new ServiceBusClient(serviceBusConn));
}


// 2. PostgreSQL EF Core & User Service
var pgsqlConn = builder.Configuration["PostgreSQL:ConnectionString"];
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(pgsqlConn) && pgsqlConn != "PASTE_HERE")
    {
        options.UseNpgsql(pgsqlConn);
    }
});
builder.Services.AddScoped<IUserService, UserService>();

// 3. AI & Background Workers
var aiKey = builder.Configuration["Google:GeminiApiKey"];
if (!string.IsNullOrEmpty(aiKey) && !aiKey.Contains("PASTE_"))
{
    builder.Services.AddSingleton(SemanticKernelSetup.BuildKernel(aiKey));
}
builder.Services.AddHttpClient<IResearchAgent, ResearchAgent>();
builder.Services.AddHttpClient<IRouteOptimiserAgent, RouteOptimiserAgent>();
builder.Services.AddScoped<ITourPackageInjector, TourPackageInjector>();
builder.Services.AddScoped<IBudgetAgent, BudgetAgent>();
// DayAssembly and Personalisation now call Gemini REST directly (no Semantic Kernel)
builder.Services.AddScoped<IDayAssemblyAgent>(_ => new DayAssemblyAgent(aiKey ?? ""));
builder.Services.AddScoped<IPersonalisationAgent>(_ => new PersonalisationAgent(aiKey ?? ""));
builder.Services.AddScoped<IOrchestratorAgent, OrchestratorAgent>();
builder.Services.AddHostedService<AiJobWorker>();

// 4. Booking Services
builder.Services.AddSingleton<IPriceCacheService, PriceCacheService>();
builder.Services.AddScoped<IAffiliateLinkBuilder, AffiliateLinkBuilder>();

// 5. Vault Services
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IOcrParserService, OcrParserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));
app.MapHub<PlanoraApp.API.Hubs.AgentHub>("/hubs/agent");

// Run DB creation in background to avoid blocking app startup on slow networks
_ = Task.Run(() => {
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            db.Database.EnsureCreated();
            Console.WriteLine("[Database] Schema check completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Schema check failed (usually due to network): {ex.Message}");
        }
    }
});

app.Run();
