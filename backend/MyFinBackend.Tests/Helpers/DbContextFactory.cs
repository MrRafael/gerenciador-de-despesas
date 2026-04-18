using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;

namespace MyFinBackend.Tests.Helpers
{
    public static class DbContextFactory
    {
        public static FinanceContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new FinanceContext(options);
        }
    }
}
