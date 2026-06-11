using TodoApi;
using TodoApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddTodoPersistence(builder.Configuration)
    .AddTodoAuth(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Apply migrations + seed the demo account before serving requests.
await app.UseDatabaseSeedingAsync();

app.MapAuthEndpoints();
app.MapTaskEndpoints();

app.Run();

public partial class Program;
