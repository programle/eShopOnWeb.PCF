using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.eShopOnContainers.Services.Basket.API.Model;

namespace Microsoft.eShopOnContainers.Services.Basket.API.Infrastructure.EntityConfigurations
{
    class BasketItemEntityTypeConfiguration
        : IEntityTypeConfiguration<BasketItem>
    {
        public void Configure(EntityTypeBuilder<BasketItem> builder)
        {
            builder.ToTable("BasketItem");

            builder.Property(ci => ci.Id)
               // .ForSqlServerUseSequenceHiLo("basket_hilo")
                .IsRequired();

            builder.Property(ci => ci.ProductId)
            .IsRequired();

            builder.Property(ci => ci.ProductName)
                .IsRequired(true)
                .HasMaxLength(50);

            builder.Property(ci => ci.UnitPrice)
                .IsRequired(true);

            builder.Property(ci => ci.OldUnitPrice)
                .IsRequired(false);

            builder.Property(ci => ci.Quantity)
               .IsRequired(true);

            builder.Ignore(ci => ci.PictureUrl);

        }
    }
}
