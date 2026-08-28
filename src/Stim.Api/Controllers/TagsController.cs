using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Authorize(Roles = Roles.Member)]
[Route("tags")]
[ApiController]
[ApiVersion(1.0)]
public class TagsController(ApplicationDbContext context, IHateoasLinkBuilder<TagDto, TagQueryParameters> hateoasLinkBuilder) : ControllerBase
{
    private bool IncludeHateoasLinks => Request.Headers.Accept.Contains(CustomMediaTypeNames.Application.HateoasJsonMediaType);
    [HttpGet(Name = "GetTags")]
    public async Task<IActionResult> GetTags(TagQueryParameters queries, SortMappingProvider sortMappingProvider, DataShapingService dataShapingService)
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
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, IncludeHateoasLinks ? t => hateoasLinkBuilder.CreateLinksForResource(HttpContext, t.Id, queries.Fields) : null),

            Links = IncludeHateoasLinks ? hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage) : null
        };

        return Ok(result);
    }
    [HttpGet("{tagId}", Name = "GetTag")]
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
        var tagDto = tag.ToDto();

        if (IncludeHateoasLinks)
        {
            tagDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, tagDto.Id, fields);
        }

        return Ok(tagDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPost(Name = "CreateTag")]
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

        if (IncludeHateoasLinks)
        {
            tagDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, tagDto.Id, null);
        }

        return CreatedAtRoute("GetTag", new { tagId = tag.Id }, tagDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{tagId}", Name = "UpdateTag")]
    public async Task<ActionResult> UpdateTag(string tagId, [FromBody] UpdateTagDto updateTagDto, [FromServices] IValidator<UpdateTagDto> validator)
    {

        await validator.ValidateAndThrowAsync(updateTagDto);

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateTag(updateTagDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{tagId}", Name = "DeleteTag")]
    public async Task<ActionResult> DeleteTag(string tagId)
    {
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        context.Tags.Remove(tag);

        await context.SaveChangesAsync();

        return NoContent();
    }
}
