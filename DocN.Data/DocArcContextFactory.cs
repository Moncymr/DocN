using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocN.Data;

public class DocArcContextFactory : IDesignTimeDbContextFactory<DocArcContext>
{
    public DocArcContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocArcContext>();
        optionsBuilder.UseSqlServer("Server=NTSPJ-060-02\\SQL2025;Database=DocumentArchive;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new DocArcContext(optionsBuilder.Options);
    }
}
