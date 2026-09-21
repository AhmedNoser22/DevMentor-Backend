namespace DevMentor.Infrastructure.Persistence.Configurations;

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.ToTable("ExamAttempts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.Domain, x.Level, x.Status });

        builder.Navigation(x => x.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Answers)
            .WithOne(a => a.ExamAttempt)
            .HasForeignKey(a => a.ExamAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}