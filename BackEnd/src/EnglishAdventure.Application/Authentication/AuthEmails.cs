using EnglishAdventure.Application.Common.Email;

namespace EnglishAdventure.Application.Authentication;

/// <summary>The emails of the registration flow, in the language the Parent used on the site.</summary>
public static class AuthEmails
{
    public const string DefaultLanguage = "vi";
    public static readonly string[] Languages = ["vi", "en"];

    public static EmailMessage ConfirmEmail(string to, string language, string link, TimeSpan validFor) =>
        language == "en"
            ? new EmailMessage(
                to,
                "Confirm your English Adventure account",
                $"""
                Hello,

                Thank you for registering with English Adventure. Open this link to confirm your email address:

                {link}

                The link works for {validFor.TotalHours:0} hours and only once. If you did not register, ignore this email.
                """)
            : new EmailMessage(
                to,
                "Xác nhận tài khoản English Adventure",
                $"""
                Xin chào,

                Cảm ơn bạn đã đăng ký English Adventure. Hãy mở liên kết sau để xác nhận địa chỉ email:

                {link}

                Liên kết có hiệu lực trong {validFor.TotalHours:0} giờ và chỉ dùng được một lần. Nếu bạn không đăng ký, hãy bỏ qua email này.
                """);

    public static EmailMessage AlreadyRegistered(string to, string language, string signInLink) =>
        language == "en"
            ? new EmailMessage(
                to,
                "You already have an English Adventure account",
                $"""
                Hello,

                Someone tried to register with this email address, but it already has an account. You can sign in here:

                {signInLink}

                If this was not you, no action is needed.
                """)
            : new EmailMessage(
                to,
                "Bạn đã có tài khoản English Adventure",
                $"""
                Xin chào,

                Có người vừa đăng ký bằng địa chỉ email này, nhưng email đã có tài khoản. Bạn có thể đăng nhập tại:

                {signInLink}

                Nếu không phải bạn, bạn không cần làm gì thêm.
                """);
}
