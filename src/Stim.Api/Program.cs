using Stim.Api;
using Stim.Api.Extensions;

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
    await app.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
