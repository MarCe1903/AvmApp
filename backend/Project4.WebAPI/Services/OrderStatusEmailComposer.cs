using System;

namespace Project4.WebAPI.Services;

public static class OrderStatusEmailComposer
{
    public static OrderStatusEmail Create(int orderId, string productName, string statusDescription, string statusCode)
    {
        return new OrderStatusEmail(
            GetSubject(statusCode),
            BuildBody(orderId, productName, statusDescription, statusCode));
    }

    private static string GetSubject(string statusCode)
    {
        return statusCode switch
        {
            "on_road" => "Siparisiniz yola cikti",
            "delivered" => "Siparisiniz teslim edildi",
            _ => "Siparis durumu guncellendi"
        };
    }

    private static string BuildBody(int orderId, string productName, string statusDescription, string statusCode)
    {
        var accentColor = statusCode switch
        {
            "on_road" => "#2563eb",
            "delivered" => "#059669",
            _ => "#7c3aed"
        };

        var statusBadge = statusCode switch
        {
            "on_road" => "Yolda",
            "delivered" => "Teslim Edildi",
            _ => "Guncellendi"
        };

        var title = statusCode switch
        {
            "on_road" => "Siparisiniz yola cikti",
            "delivered" => "Siparisiniz basariyla teslim edildi",
            _ => "Siparis durumunuz guncellendi"
        };

        var summaryText = statusCode switch
        {
            "on_road" => "Hazirlanan siparisiniz kargoya verildi. Kisa sure icinde size ulasacak.",
            "delivered" => "Siparisiniz teslim edildi olarak isaretlendi. Umariz alisveris deneyiminizden memnun kalmissinizdir.",
            _ => "Siparisiniz icin yeni bir durum guncellemesi bulunuyor."
        };

        return $"""
            <!DOCTYPE html>
            <html lang=\"tr\">
            <head>
                <meta charset=\"utf-8\" />
                <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />
                <title>{title}</title>
            </head>
            <body style=\"margin:0;padding:0;background-color:#f4f7fb;font-family:Arial,sans-serif;color:#0f172a;\">
                <table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background-color:#f4f7fb;padding:32px 12px;\">
                    <tr>
                        <td align=\"center\">
                            <table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:640px;background:#ffffff;border-radius:24px;overflow:hidden;box-shadow:0 20px 45px rgba(15,23,42,0.10);\">
                                <tr>
                                    <td style=\"padding:28px 32px;background:linear-gradient(135deg,#0f172a 0%,#1e293b 100%);\">
                                        <div style=\"font-size:13px;letter-spacing:2px;text-transform:uppercase;color:#93c5fd;font-weight:700;\">AVM App</div>
                                        <div style=\"margin-top:14px;font-size:30px;line-height:1.2;font-weight:700;color:#ffffff;\">{title}</div>
                                        <div style=\"margin-top:10px;font-size:15px;line-height:1.7;color:#cbd5e1;\">{summaryText}</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=\"padding:32px;\">
                                        <div style=\"display:inline-block;padding:8px 14px;border-radius:999px;background:{accentColor};color:#ffffff;font-size:13px;font-weight:700;letter-spacing:0.4px;\">
                                            {statusBadge}
                                        </div>

                                        <table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin-top:24px;border-collapse:separate;border-spacing:0 12px;\">
                                            <tr>
                                                <td style=\"width:180px;font-size:13px;color:#64748b;\">Siparis Numarasi</td>
                                                <td style=\"font-size:15px;font-weight:700;color:#0f172a;\">#{orderId}</td>
                                            </tr>
                                            <tr>
                                                <td style=\"font-size:13px;color:#64748b;\">Urun</td>
                                                <td style=\"font-size:15px;font-weight:700;color:#0f172a;\">{productName}</td>
                                            </tr>
                                            <tr>
                                                <td style=\"font-size:13px;color:#64748b;\">Guncel Durum</td>
                                                <td style=\"font-size:15px;font-weight:700;color:{accentColor};\">{statusDescription}</td>
                                            </tr>
                                            <tr>
                                                <td style=\"font-size:13px;color:#64748b;\">Guncelleme Zamani</td>
                                                <td style=\"font-size:15px;font-weight:700;color:#0f172a;\">{DateTime.Now:dd.MM.yyyy HH:mm}</td>
                                            </tr>
                                        </table>

                                        <div style=\"margin-top:28px;padding:20px;border-radius:18px;background:#f8fafc;border:1px solid #e2e8f0;\">
                                            <div style=\"font-size:14px;line-height:1.8;color:#334155;\">
                                                Hesabinizdaki <strong>Siparislerim</strong> sayfasindan tum siparis gecmisinizi ve yeni durum guncellemelerini takip edebilirsiniz.
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=\"padding:0 32px 32px 32px;\">
                                        <div style=\"height:1px;background:#e2e8f0;\"></div>
                                        <div style=\"padding-top:18px;font-size:12px;line-height:1.8;color:#94a3b8;\">
                                            Bu e-posta AVM App tarafindan siparis bilgilendirmesi amaciyla gonderilmistir.
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}

public readonly record struct OrderStatusEmail(string Subject, string Body);
