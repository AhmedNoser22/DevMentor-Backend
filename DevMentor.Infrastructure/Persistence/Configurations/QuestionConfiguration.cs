namespace DevMentor.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Text).IsRequired().HasMaxLength(2000);
        builder.HasIndex(x => new { x.Domain, x.Level, x.Status });

        builder.Navigation(x => x.Options).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}