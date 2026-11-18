using SimpleAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();

// This API accepts Microsoft Entra ID tokens only and does not call other APIs,
// so the scheme is added to the standard authentication builder and no credentials are required.
builder.Services.AddAuthentication()
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

// This endpoint requires an app-only access token that contains the 'Inventory.Read' application role.
app.MapGet("api/products", () =>
{
    Product[] products = [new(1, "Keyboard", 12), new(2, "Monitor", 0)];
    return TypedResults.Ok(products);
})
.RequireEntraIdApplicationRole("Inventory.Read");

app.Run();

public record class Product(int Id, string Name, int AvailableQuantity);
