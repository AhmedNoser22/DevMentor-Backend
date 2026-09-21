namespace DevMentor.Application.Features.Interviews;

public class InterviewService : IInterviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiClient _aiClient;
    private readonly InterviewSettings _settings;

    public InterviewService(IUnitOfWork unitOfWork, IAiClient aiClient, InterviewSettings settings)
    {
        _unitOfWork = unitOfWork;
        _aiClient = aiClient;
        _settings = settings;
    }

    public async Task<InterviewSessionDto> StartAsync(Guid userId, StartInterviewRequest request, CancellationToken ct = default)
    {
        var todayCount = await _unitOfWork.InterviewSessions.CountTodayAsync(userId, ct);
        if (todayCount >= _settings.DailyLimitPerUser)
        {
            throw new ConflictException("Daily interview limit reached, please try again tomorrow");
        }

        var firstQuestion = await _aiClient.GetFirstInterviewQuestionAsync(request.Domain, ct);
        var session = InterviewSession.Start(userId, request.Domain, firstQuestion);

        await _unitOfWork.InterviewSessions.AddAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Map(session);
    }

    public async Task<InterviewSessionDto> AnswerAsync(Guid userId, AnswerTurnRequest request, CancellationToken ct = default)
    {
        var session = await _unitOfWork.InterviewSessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Interview session not found");

        if (session.UserId != userId)
        {
            throw new UnauthorizedAppException("This session does not belong to the current user");
        }

        session.RecordAnswer(request.TurnId, request.Answer);

        _unitOfWork.InterviewSessions.Update(session);
        await _unitOfWork.SaveChangesAsync(ct);

        var answeredTurn = session.Turns.First(t => t.Id == request.TurnId);
        var history = session.Turns
            .Where(t => t.Answer is not null)
            .OrderBy(t => t.Order)
            .Select(t => new InterviewTurnContext(t.Question, t.Answer!))
            .ToList();

        var evaluation = await _aiClient.EvaluateInterviewTurnAsync(
            session.Domain, history, answeredTurn.Question, request.Answer, answeredTurn.Order, _settings.MaxTurns, ct);

        session.ApplyEvaluationAndAdvance(
            request.TurnId,
            new InterviewTurnEvaluation(
                evaluation.TechnicalAccuracy,
                evaluation.MissingConcepts,
                evaluation.Communication,
                evaluation.Score,
                evaluation.FollowUpQuestion,
                evaluation.IsFinalTurn),
            _settings.MaxTurns);

        _unitOfWork.InterviewSessions.Update(session);
        await _unitOfWork.SaveChangesAsync(ct);

        return Map(session);
    }

    public async Task<InterviewSessionDto> GetAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _unitOfWork.InterviewSessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Interview session not found");

        if (session.UserId != userId)
        {
            throw new UnauthorizedAppException("This session does not belong to the current user");
        }

        return Map(session);
    }

    private static InterviewSessionDto Map(InterviewSession session)
    {
        return new InterviewSessionDto(
            session.Id,
            session.Domain,
            session.EndedAtUtc is not null,
            session.FinalScore,
            session.SummaryText,
            session.Turns.OrderBy(t => t.Order).Select(t => new InterviewTurnDto(
                t.Id, t.Order, t.Question, t.Answer,
                t.TechnicalAccuracyFeedback, t.MissingConceptsFeedback, t.CommunicationFeedback, t.Score)).ToList());
    }
}