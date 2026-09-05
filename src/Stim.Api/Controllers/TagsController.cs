using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Filters;
using Stim.Api.Models.Common;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Concurrency;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Representation_Context;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("tags")]
[ApiController]
[ApiVersion(1.0)]
public class TagsController(ApplicationDbContext context, IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet(Name = "GetTags")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(DataCollectionResponse<TagDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<DataCollectionResponse<TagDto>>> GetTags([FromQuery] TagQueryParameters queries,
    [FromServices] SortMappingProvider sortMappingProvider,
    [FromServices] DataShapingService dataShapingService,
    CancellationToken cancellationToken)
    {
        var validationResult = ValidateQueryParameters(queries, sortMappingProvider, dataShapingService);

        if (validationResult is not null)
        {
            return validationResult;
        }
        var tagsQuery = BuildTagQuery(queries, sortMappingProvider);

        var pageSize = queries.PageSize ?? TagQueryParameters.TagQueryDefaults.PageSize;

        DataCollectionResponse<TagDto> dataCollectionResponse; if (queries.PaginationType == PaginationType.Cursor)
        {
            var result = await tagsQuery.ToCursorPaginationResult(queries.Cursor, pageSize, cancellationToken);

            dataCollectionResponse = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };
        }
        else
        {
            var page = queries.Page ?? TagQueryParameters.TagQueryDefaults.Page;

            var result = await tagsQuery.ToPaginationResultAsync(page, pageSize, cancellationToken);

            queries = queries with { Page = page };

            dataCollectionResponse = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };
        }

        AddHateoasLinks(dataCollectionResponse, queries);

        var shapedData = dataShapingService.ShapeCollectionData(dataCollectionResponse.Data, queries.Fields);

        var response = new DataCollectionResponse<ExpandoObject>
        {
            Data = [.. shapedData],
            Pagination = dataCollectionResponse.Pagination,
            Links = dataCollectionResponse.Links
        };

        return Ok(response);
    }
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet("{tagId}", Name = "GetTag")]
    [ETagConcurrencyFilterAttribute]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TagDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Tag With Provided Id Could Not Be Found")]
    public async Task<ActionResult<TagDto>> GetTag(string tagId, [FromServices] DataShapingService dataShapingService, string? fields)
    {
        if (!dataShapingService.Validate<TagDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {fields}");
        }
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        HttpContext.Items[HttpContextItemKeys.ResourceVersion] = tag.RowVersion;

        var tagDto = tag.ToDto();

        if (representationContext.IncludeHateoasLinks)
        {
            tagDto.Links = tagLinkBuilder.CreateLinksForResource(HttpContext, tagDto.Id, fields);
        }

        return Ok(tagDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPost(Name = "CreateTag")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TagDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto createTagDto, [FromServices] IValidator<CreateTagDto> validator)
    {
        await validator.ValidateAndThrowAsync(createTagDto);

        if (await context.Tags.AnyAsync(t => t.Name.Equals(createTagDto.Name)))
        {
            return BadRequest("The tag with the provided name already exists");
        }

        var tag = createTagDto.ToEntity();

        await context.Tags.AddAsync(tag);

        await context.SaveChangesAsync();

        var tagDto = tag.ToDto();

        if (representationContext.IncludeHateoasLinks)
        {
            tagDto.Links = tagLinkBuilder.CreateLinksForResource(HttpContext, tagDto.Id, null);
        }

        return CreatedAtRoute("GetTag", new { tagId = tag.Id }, tagDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{tagId}", Name = "UpdateTag")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateTag(string tagId, [FromBody] UpdateTagDto updateTagDto, [FromServices] IValidator<UpdateTagDto> validator)
    {

        await validator.ValidateAndThrowAsync(updateTagDto);

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, tag, expectedVersion);

        tag.UpdateTag(updateTagDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{tagId}", Name = "DeleteTag")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]

    public async Task<ActionResult> DeleteTag(string tagId)
    {
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, tag, expectedVersion);

        context.Tags.Remove(tag);

        await context.SaveChangesAsync();

        return NoContent();
    }
    private IQueryable<Tag> BuildTagQuery(TagQueryParameters queries, SortMappingProvider sortMappingProvider)
    {
        var search = queries.Search?.Trim().ToLower();

        var sortMappings = sortMappingProvider.GetMappings<TagDto, Tag>();

        return context.Tags.Where(tag => search == null || tag.Name.ToLower().Contains(search)).ApplySort(queries.Sort, sortMappings);
    }
    private ObjectResult? ValidateQueryParameters(
        TagQueryParameters queries,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShapingService)
    {
        if (queries.Page is not null && queries.Cursor is not null)
        {
            return Problem("The 'page' and 'cursor' query parameters cannot be used together.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.Page is not null && queries.Page < 1)
        {
            return Problem("The 'page' query parameter must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.PageSize is not null && queries.PageSize < 1)
        {
            return Problem("The 'pageSize' query parameter must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!sortMappingProvider.ValidateMappings<TagDto, Tag>(
            queries.Sort))
        {
            return Problem($"The supplied sort parameter is invalid: '{queries.Sort}'.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!dataShapingService.Validate<TagDto>(
            queries.Fields))
        {
            return Problem($"The supplied fields parameter is invalid: '{queries.Fields}'.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.PaginationType == PaginationType.Cursor &&
            !string.IsNullOrWhiteSpace(queries.Sort))
        {
            return Problem("Custom sorting is not supported with cursor pagination. " + "Cursor pagination uses CreatedAtUtc descending and Id ascending.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.PaginationType == PaginationType.Cursor &&
            queries.Page is not null)
        {
            return Problem("'page' cannot be used with cursor pagination.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.PaginationType == PaginationType.Offset &&
            !string.IsNullOrWhiteSpace(queries.Cursor))
        {
            return Problem("'Cursor' cannot be used with offset pagination. " + "Review your parameters.", statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }
    private void AddHateoasLinks(DataCollectionResponse<TagDto> response, TagQueryParameters queries)
    {
        if (!representationContext.IncludeHateoasLinks)
        {
            return;
        }

        foreach (var tag in response.Data)
        {
            tag.Links = tagLinkBuilder.CreateLinksForResource(HttpContext, tag.Id, queries.Fields);
        }

        if (response.Pagination?.PaginationType == PaginationType.Cursor)
        {
            response.Links = tagLinkBuilder.CreateCursorCollectionLinks(HttpContext, queries, response.Pagination.NextCursor, response.Pagination.PreviousCursor);

            return;
        }

        response.Links = tagLinkBuilder.CreateLinksForCollection(HttpContext, queries, response.Pagination?.HasNextPage ?? false, response.Pagination?.HasPreviousPage ?? false);
    }

}
