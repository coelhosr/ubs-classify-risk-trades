using WebApi.Extensions;


var builder = WebApplication.CreateBuilder(args);

// MVC controllers
builder.Services.AddControllers();

// Swagger (Swashbuckle)
builder.Services.AddSwaggerGen();

// DI RiskDomain - ClientSectors - Validators
builder.Services
    .AddDI()
    .AddClientSectorConfig(builder.Configuration)
    .AddValidators();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UBS - Risk API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();

// Controller-based routing
app.MapControllers();

app.Run();
