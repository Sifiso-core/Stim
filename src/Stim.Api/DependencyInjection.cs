using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Middleware;
using Stim.Api.Models.Developer;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Hateoas.Developer;
using Stim.Api.Services.Hateoas.Game;
using Stim.Api.Services.Sorting;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {

        builder.Services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });
        builder.Services.AddProblemDetails();

        builder.Services.AddSwaggerGen();

        builder.Services.AddOpenApi();

        return builder;
    }
    public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
    {

        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        return builder;
    }
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<SortMappingProvider>();

        builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<DeveloperDto, Developer>>(_ => DeveloperMappings.SortMapping);

        builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<GameDto, Game>>(_ => GameMappings.SortMapping);

        builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<GenreDto, Genre>>(_ => GenreMappings.SortMapping);

        builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<TagDto, Tag>>(_ => TagMappings.SortMapping);

        builder.Services.AddTransient<DataShapingService>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IHateoasLinkBuilder<DeveloperDto, DeveloperQueryParameters>, DeveloperLinkBuilder>();
        builder.Services.AddScoped<IHateoasLinkBuilder<GameDto, GameQueryParameters>, GameLinkBuilder>();

        return builder;
    }
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("postgresConnection"), npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application));
        });
        return builder;
    }
}