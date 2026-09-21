namespace DevMentor.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));
        services.AddScoped<IQuestionBankService, QuestionBankService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<IProfileService, ProfileService>();
        return services;
    }
}