namespace StockFlow.Application.UseCases;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password baru wajib diisi.";

        if (password.Length < MinimumLength ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            return $"Password minimal {MinimumLength} karakter dan harus memiliki huruf besar, huruf kecil, angka, serta simbol.";
        }

        return null;
    }
}
