using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace addFloreriaG5.Models;

// Mensaje que se muestra en la conversación del ChatBot.
public class MensajeChat
{
    public string Texto { get; set; } = string.Empty;

    public bool EsUsuario { get; init; }

    public bool EsError { get; init; }

    public bool EsCargando { get; init; }

    public DateTimeOffset Hora { get; init; } = DateTimeOffset.Now;

    public string HoraTexto => Hora.ToString("HH:mm");

    public bool MostrarHora => !EsCargando;

    public LayoutOptions Alineacion =>
        EsUsuario ? LayoutOptions.End : LayoutOptions.Start;

    public LayoutOptions AlineacionHora =>
        EsUsuario ? LayoutOptions.End : LayoutOptions.Start;

    public Color ColorFondo =>
        EsError
            ? Color.FromArgb("#FDE8E8")
            : EsUsuario
                ? Color.FromArgb("#9B3E5C")
                : Colors.White;

    public Color ColorTexto =>
        EsError
            ? Color.FromArgb("#9B2C2C")
            : EsUsuario
                ? Colors.White
                : Color.FromArgb("#68454D");

    public Color ColorBorde =>
        EsError
            ? Color.FromArgb("#F3C6C6")
            : EsUsuario
                ? Color.FromArgb("#9B3E5C")
                : Color.FromArgb("#EBDCE0");

    public Color ColorHora =>
        EsUsuario
            ? Color.FromArgb("#F6DCE4")
            : Color.FromArgb("#A89297");
}
