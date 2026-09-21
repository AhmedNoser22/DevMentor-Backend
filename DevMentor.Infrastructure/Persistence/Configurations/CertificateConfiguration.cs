namespace DevMentor.Infrastructure.Persistence.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CertificateCode).IsRequired().HasMaxLength(60);
        builder.HasIndex(x => x.CertificateCode).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Domain, x.Level }).IsUnique();
    }
}