using Stim.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddAPICoreServices()
        .AddDatabase()
        .AddErrorHandling()
        .AddApplicationServices()
        .AddAuthenticationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
