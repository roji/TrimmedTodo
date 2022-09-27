using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TrimmedTodo.WebApi.EfCore.Sqlite.Data;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5079));
}

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtConfigHelper.ConfigureJwtBearer(builder));

builder.Services.AddAuthorization();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Validate<IHostEnvironment, ILoggerFactory>(JwtConfigHelper.ValidateJwtOptions,
        "JWT options are not configured. Run 'dotnet user-jwts create' in project directory to configure JWT.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("TodoDb") ?? "Data Source=todos.db;Cache=Shared";
builder.Services.AddSqlite<TodoDb>(connectionString)
                .AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureDb(connectionString, app.Services, app.Logger);

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.MapControllers();

await app.StartApp("/api/todos");

static async Task EnsureDb(string cs, IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", cs);

    using var db = services.CreateScope().ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.MigrateAsync();
}
