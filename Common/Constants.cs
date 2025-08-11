namespace ShipmentFinishGood.Common;

public static class ValidationConstants
{
    public const int UsernameMaxLength = 50;
    public const int NameMaxLength = 100;
    public const int PasswordMinLength = 6;
    public const int PasswordMaxLength = 100;
}

public static class AuthConstants
{
    public const string CookieScheme = "app_cookie";
    public const string LoginPath = "/Auth/Login";
    public const string AccessDeniedPath = "/Auth/Denied";
}

public static class PolicyNames
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireScanner = "RequireScanner";
    public const string RequireInputer = "RequireInputer";
    public const string RequireManajemen = "RequireManajemen";
}

public static class ErrorMessages
{
    public const string UserNotFound = "User not found";
    public const string UsernameAlreadyExists = "Username already exists";
    public const string InvalidCredentials = "Username atau password salah";
    public const string UsernamePasswordRequired = "Username & password wajib";
}
