namespace DevMentor.Infrastructure.Persistence.Configurations;

public class InterviewSessionConfiguration : IEntityTypeConfiguration<InterviewSession>
{
    public void Configure(EntityTypeBuilder<InterviewSession> builder)
    {
        builder.ToTable("InterviewSessions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.StartedAtUtc });
        builder.Property(x => x.SummaryText).HasMaxLength(1000);

        builder.Navigation(x => x.Turns).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Turns)
            .WithOne(t => t.InterviewSession)
            .HasForeignKey(t => t.InterviewSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}