using Scalar.AspNetCore;
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
    app.MapOpenApi().WithDocumentPerVersion();

    app.MapScalarApiReference(options =>
    {
        var descriptions = app.DescribeApiVersions();

        for (var i = 0; i < descriptions.Count; i++)
        {
            var description = descriptions[i];

            options.AddDocument(description.GroupName, description.GroupName, isDefault: i == descriptions.Count - 1);
        }
        options.WithTheme(ScalarTheme.DeepSpace);

        options.Title = "Stim Api Reference";

    });
    await app.ApplyMigrationsAsync();
    await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
