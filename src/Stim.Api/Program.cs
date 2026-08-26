using Stim.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddAPICoreServices()
        .AddDatabase()
        .AddErrorHandling()
        .AddApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
