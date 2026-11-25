namespace addFloreriaG5.Services;

public static class ValidadorPassword
{
    public const string Requisitos = "Usa al menos 8 caracteres, una mayúscula, una minúscula, un número y un símbolo.";

    public static bool EsValida(string? clave) =>
        !string.IsNullOrWhiteSpace(clave) && clave.Length >= 8 &&
        clave.Any(char.IsUpper) && clave.Any(char.IsLower) &&
        clave.Any(char.IsDigit) && clave.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
}
