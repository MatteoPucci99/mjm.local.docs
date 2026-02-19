using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mjm.LocalDocs.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating <see cref="LocalDocsDbContext"/> instances.
/// Used by EF Core CLI tools (dotnet ef migrations add, dotnet ef database update).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LocalDocsDbContext>
{
    /// <inheritdoc />
    public LocalDocsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LocalDocsDbContext>();
        optionsBuilder.UseSqlite("Data Source=localdocs.db");

        return new LocalDocsDbContext(optionsBuilder.Options);
    }
}
