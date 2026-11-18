using Microsoft.Identity.Web;
using SimpleAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();

// Add the authentication schemes defined in the Authentication section of the configuration (none in this sample),
// then add Microsoft Entra ID as an additional scheme. Default schemes and authorization policies are not changed.
builder.Services.AddSimpleAuthentication(builder.Configuration)
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"))
    // The following lines are needed only because this API calls another API using its own identity.
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Register the HTTP client that calls the Inventory API using an app-only access token.
builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"))
    .AddTypedClient<InventoryClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

// This endpoint requires an app-only access token that contains the 'Orders.Read' application role.
app.MapGet("api/orders", async (InventoryClient inventoryClient, CancellationToken cancellationToken) =>
{
    // The availability of the ordered products is provided by the Inventory API.
    var products = await inventoryClient.GetProductsAsync(cancellationToken);
    var orders = products.Select(product => new Order(product.Id, product.Name, Quantity: 1, IsAvailable: product.AvailableQuantity > 0));

    return TypedResults.Ok(orders);
})
.RequireEntraIdApplicationRole("Orders.Read");

app.Run();

public record class Order(int Id, string ProductName, int Quantity, bool IsAvailable);

public record class Product(int Id, string Name, int AvailableQuantity);

public class InventoryClient(HttpClient httpClient)
{
    public async Task<IEnumerable<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IEnumerable<Product>>("api/products", cancellationToken) ?? [];
}
