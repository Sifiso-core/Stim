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
using Stim.Api.Services.User_Context;

namespace Stim.Api.Controllers;

[Route("tags")]
[ApiController]
[ApiVersion(1.0)]
public class TagsController(ApplicationDbContext context, IHateoasLinkBuilder<TagDto, TagQueryParameters> hateoasLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet(Name = "GetTags")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(DataCollectionResponse<TagDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<DataCollectionResponse<TagDto>>> GetTags(TagQueryParameters queries, SortMappingProvider sortMappingProvider, DataShapingService dataShapingService)
    {
        if (!sortMappingProvider.ValidateMappings<TagDto, Tag>(queries.Sort))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided sort parameters is invalid '{queries.Sort}'");
        }

        if (!dataShapingService.Validate<TagDto>(queries.Fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {queries.Fields}");
        }

        var sortMappings = sortMappingProvider.GetMappings<TagDto, Tag>();

        var search = queries.Search?.Trim().ToLower();

        var tagsQuaryable = context.Tags.Where(t => search == null || t.Name.ToLower().Contains(search))
        .ApplySort(queries.Sort, sortMappings)
        .Select(TagQueries.ProjectToDto());

        var paginationResult = await tagsQuaryable.ToPaginationResultAsync(queries.Page, queries.PageSize);

        var result = new DataCollectionResponse<ExpandoObject>()
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, representationContext.IncludeHateoasLinks ? t => hateoasLinkBuilder.CreateLinksForResource(HttpContext, t.Id, queries.Fields) : null),

            Links = representationContext.IncludeHateoasLinks ? hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage) : null
        };

        return Ok(result);
    }
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet("{tagId}", Name = "GetTag")]
    [ETagCache]
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
            tagDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, tagDto.Id, fields);
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
            tagDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, tagDto.Id, null);
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
}
