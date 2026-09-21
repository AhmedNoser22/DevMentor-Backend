namespace DevMentor.Infrastructure.Persistence.Configurations;

public class InterviewTurnConfiguration : IEntityTypeConfiguration<InterviewTurn>
{
    public void Configure(EntityTypeBuilder<InterviewTurn> builder)
    {
        builder.ToTable("InterviewTurns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Question).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Answer).HasMaxLength(4000);
        builder.Property(x => x.TechnicalAccuracyFeedback).HasMaxLength(1000);
        builder.Property(x => x.MissingConceptsFeedback).HasMaxLength(1000);
        builder.Property(x => x.CommunicationFeedback).HasMaxLength(1000);
        builder.Property(x => x.FollowUpQuestion).HasMaxLength(2000);
    }
}