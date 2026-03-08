using Microsoft.EntityFrameworkCore;
using TripTracker.API.DbContexts;
using TripTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Register repositories
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripStopRepository, TripStopRepository>();
// Register AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Register DbContext
builder.Services.AddDbContext<TripTrackerContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TripTrackerDBConnectionString")));
// Register Controllers
builder.Services.AddControllers();
// Swagger for API testing
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// CORS for MAUI app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// UITGESCHAKELD voor Ngrok - HTTP redirect veroorzaakt problemen
// app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();

