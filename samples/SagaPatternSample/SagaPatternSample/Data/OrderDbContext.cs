using Microsoft.EntityFrameworkCore;

namespace SagaPatternSample.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    // Add your domain entities here if needed
    // For saga state persistence, use ISagaPersistence<TSagaData>
}
