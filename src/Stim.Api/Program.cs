using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Middleware;
using Stim.Api.Models.Developer;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Sorting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddTransient<SortMappingProvider>();

builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<DeveloperDto, Developer>>(_ => DeveloperMappings.SortMapping);

builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<GameDto, Game>>(_ => GameMappings.SortMapping);

builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<GenreDto, Genre>>(_ => GenreMappings.SortMapping);

builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<TagDto, Tag>>(_ => TagMappings.SortMapping);

builder.Services.AddProblemDetails();

builder.Services.AddSwaggerGen();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgresConnection"), npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application));
});

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
