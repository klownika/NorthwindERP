using NorthwindERP.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "NorthwindERP API");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
