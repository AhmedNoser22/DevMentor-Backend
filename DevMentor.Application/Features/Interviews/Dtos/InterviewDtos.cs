namespace DevMentor.Application.Features.Interviews.Dtos;

public record StartInterviewRequest(TechDomain Domain);

public record InterviewTurnDto(
    Guid TurnId,
    int Order,
    string Question,
    string? Answer,
    string? TechnicalAccuracyFeedback,
    string? MissingConceptsFeedback,
    string? CommunicationFeedback,
    int? Score);

public record InterviewSessionDto(
    Guid SessionId,
    TechDomain Domain,
    bool Ended,
    int? FinalScore,
    string? SummaryText,
    List<InterviewTurnDto> Turns);

public record AnswerTurnRequest(Guid SessionId, Guid TurnId, string Answer);