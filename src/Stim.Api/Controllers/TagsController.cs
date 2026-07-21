using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.Common;
using Stim.Api.Models.Tag;

namespace Stim.Api.Controllers;

[Route("tags")]
[ApiController]
public class TagsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DataCollectionResponse<TagDto>>> GetTags()
    {
        var tags = await context.Tags.Select(TagQueries.ProjectToDto()).ToListAsync();

        var result = new DataCollectionResponse<TagDto>
        {
            Data = tags
        };
        return Ok(result);
    }
    [HttpGet("{tagId}", Name = "GetTag")]
    public async Task<ActionResult<TagDto>> GetTag(string tagId)
    {
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        return Ok(tag);
    }
    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto createTagDto)
    {

        if (await context.Tags.AnyAsync(t => t.Name.Equals(createTagDto.Name)))
        {
            return BadRequest("The tag with the provided name already exists");
        }
        var tag = createTagDto.ToEntity();

        await context.Tags.AddAsync(tag);

        await context.SaveChangesAsync();

        var result = tag.ToDto();

        return CreatedAtRoute("GetTag", new { tagId = tag.Id }, result);
    }
    [HttpPut("{tagId}")]
    public async Task<ActionResult> UpdateTag(string tagId, [FromBody] UpdateTagDto updateTagDto)
    {
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateTag(updateTagDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpDelete("{tagId}")]
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
