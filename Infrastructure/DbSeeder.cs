using Microsoft.EntityFrameworkCore;
using UserDomain.Entities;

namespace Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!await context.Contexts.AnyAsync())
        {
            context.Contexts.AddRange(
                new Contexts
                {
                    RoomCount = 1,
                    ContextData = "I am designing a room: \"{{RoomName}}\" with dimensions {{Length}}x{{Width}}x{{Height}} {{Unit}}. Style: {{Style}}. Suggest a color palette, furniture layout, lighting plan, and decor items. Consider the room's proportions and natural light.",
                    SourceAI = "claude",
                    Type = "home-single"
                },
                new Contexts
                {
                    RoomCount = 2,
                    ContextData = "I have two rooms to decorate. Room 1: \"{{RoomName}}\" ({{Length}}x{{Width}}x{{Height}} {{Unit}}). Room 2: \"{{RoomName2}}\" ({{Length2}}x{{Width2}}x{{Height2}} {{Unit}}). Style: {{Style}}. Suggest a cohesive color palette, furniture layout, and decor for both rooms that flow together harmoniously.",
                    SourceAI = "claude",
                    Type = "home-double"
                },
                new Contexts
                {
                    RoomCount = 1,
                    ContextData = "I am designing an event marquee: \"{{RoomName}}\" with dimensions {{Length}}x{{Width}}x{{Height}} {{Unit}}. Ceiling type: {{CeilingType}}. Style: {{Style}}. Suggest layout, seating arrangement, lighting design, ceiling decor, and flow for guests.",
                    SourceAI = "claude",
                    Type = "marquee"
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
