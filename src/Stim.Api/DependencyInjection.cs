using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Stim.Api;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Middleware;
using Stim.Api.Models.Developer;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Hateoas.Developer;
using Stim.Api.Services.Hateoas.Game;
using Stim.Api.Services.Hateoas.Genre;
using Stim.Api.Services.Hateoas.Tag;
using Stim.Api.Services.Sorting;

namespace Stim.Api
{
    public static class DependencyInjection
    {
        public static WebApplicationBuilder AddAPICoreServices(this WebApplicationBuilder builder)
        {

            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            builder.Services.Configure<MvcOptions>(options =>
            {
                var formatter = options.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>().First();
                formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonMediaType);
                formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonMediaTypeV1);
                formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonMediaTypeV2);
                formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.JsonV1);
                formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.JsonV2);
            });

            builder.Services.AddProblemDetails();

            builder.Services.AddSwaggerGen();

            builder.Services.AddOpenApi();

            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(new MediaTypeApiVersionReader(),
                new MediaTypeApiVersionReaderBuilder().Template("application/vnd.stim.hateoas.{version}+json").Build());
            }).AddMvc();

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

            builder.Services.AddScoped<IHateoasLinkBuilder<GenreDto, GenreQueryParameters>, GenreLinkBuilder>();

            builder.Services.AddScoped<IHateoasLinkBuilder<TagDto, TagQueryParameters>, TagLinkBuilder>();

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
}