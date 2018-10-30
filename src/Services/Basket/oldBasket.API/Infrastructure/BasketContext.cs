namespace Basket.API.Infrastructure
{
    using Microsoft.EntityFrameworkCore;
    using Basket.API.Infrastructure;
    using Basket.API.Model;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;
    using JetBrains.Annotations;
    using Microsoft.eShopOnContainers.Services.Basket.API.Model;
    using Microsoft.eShopOnContainers.Services.Basket.API.Infrastructure.EntityConfigurations;

    public class BasketContext : DbContext
    {
        public BasketContext(DbContextOptions<BasketContext> options) : base(options)
        {
        }

        //public BasketContext([NotNullAttribute] DbContextOptions options) : base(options)
        //{
        //}
        public DbSet<BasketItem> BasketItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.ApplyConfiguration(new BasketItemEntityTypeConfiguration());
        }
    }


    public class BasketContextDesignFactory : IDesignTimeDbContextFactory<BasketContext>
    {
        private IConfiguration configuration;
        public BasketContextDesignFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public BasketContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BasketContext>()
                .UseSqlServer(configuration["ConnectionString"]);
            //.UseSqlServer("Server=.;Initial Catalog=Microsoft.eShopOnContainers.Services.BasketDb;Integrated Security=true");

            return new BasketContext(optionsBuilder.Options);
        }
    }
}
