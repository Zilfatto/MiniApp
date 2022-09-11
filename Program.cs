using MiniApp.Common.Extensions;
using MiniApp.Core.Repositories;
using MiniApp.Persistence;
using MiniApp.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<IPairRepository, PairRepository>();
builder.Services.AddTransient<IRequestLogRepository, RequestLogRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// It logs request/response data to the DB
app.UseRequestLogger();

app.UseAuthorization();

app.MapControllers();

SqlServer.Setup(app.Configuration);
await SqlServer.InitializeDatabaseAsync();

app.Run();