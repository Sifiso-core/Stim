namespace Stim.Api.Models.Cursor;

public record Cursor(string Id, DateTime CreatedAtUtc, CursorDirection Direction = CursorDirection.Next);
