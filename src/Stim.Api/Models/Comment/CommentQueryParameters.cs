namespace Stim.Api.Models.Commnet;

public record CommentQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}