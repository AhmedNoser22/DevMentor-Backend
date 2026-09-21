namespace DevMentor.Infrastructure.Documents;

public class QuestPdfCertificateGenerator : ICertificatePdfGenerator
{
    public byte[] Generate(Certificate certificate, string recipientName)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var qrPngBytes = GenerateQrCode($"https://devmentor.app/verify/{certificate.CertificateCode}");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial"));

                page.Content().Border(2).BorderColor(Colors.Amber.Medium).Padding(30).Column(column =>
                {
                    column.Item().AlignCenter().Text("Certificate of Completion").FontSize(28).Bold();
                    column.Item().AlignCenter().PaddingTop(6).Text($"{certificate.Domain} — {certificate.Level}").FontSize(16);
                    column.Item().AlignCenter().PaddingTop(20).Text(recipientName).FontSize(22).Italic();
                    column.Item().AlignCenter().PaddingTop(10).Text(
                        $"Awarded for a passing score of {certificate.ScorePercentage}% on the DevMentor assessment.").FontSize(12);

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text($"Certificate ID: {certificate.CertificateCode}").FontSize(10);
                            inner.Item().Text($"Issued: {certificate.IssuedAtUtc:yyyy-MM-dd}").FontSize(10);
                        });
                        row.ConstantItem(90).Image(qrPngBytes);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] GenerateQrCode(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        return pngQr.GetGraphic(10);
    }
}