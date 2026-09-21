namespace DevMentor.Application.Abstractions;

public interface ICertificatePdfGenerator
{
    byte[] Generate(Certificate certificate, string recipientName);
}