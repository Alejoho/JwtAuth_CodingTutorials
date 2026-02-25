using Microsoft.EntityFrameworkCore;
using WebApiAuthentication.DataAccess.Entities;

namespace WebApiAuthentication.DataAccess.Context
{
    public class ReviewContext : DbContext
    {
        public ReviewContext(DbContextOptions<ReviewContext> options)
            : base(options)
        {
        }

        public DbSet<BookReview> BookReviews { get; set; }
    }
}
