using PharmacyApp.Api.Middleware;
using PharmacyApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service registration
// ---------------------------------------------------------------------------

builder.Services.AddControllers();

// Swagger/OpenAPI - handy for exercising the API directly while developing.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ABC Pharmacy API",
        Version = "v1",
        Description = "Tracks medicines and their sale records for ABC Pharmacy."
    });
});

// Services are registered as Scoped: cheap to create, and each holds a
// reference to the JSON file path resolved once per request.
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<ISaleService, SaleService>();

// Allow the front-end (which may be served from a different origin/port during
// development, e.g. a live-reload dev server) to call the API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------

// Must be registered first so it can catch exceptions thrown by everything after it.
app.UseAppExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ABC Pharmacy API v1");
    });
}

app.UseCors("AllowFrontend");

// Serves the SPA (index.html, css, js) from wwwroot/.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// Any route that isn't an API call or a static file falls back to index.html,
// so client-side routing (if added later) keeps working on refresh/deep-link.
app.MapFallbackToFile("index.html");

app.Run();
