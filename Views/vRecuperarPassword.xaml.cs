using System.Net.Mail;
using addFloreriaG5.Services;
namespace addFloreriaG5.Views;
public partial class vRecuperarPassword : ContentPage
{
    private readonly Entry[] digitos = new Entry[8];
    private bool actualizandoCodigo;
    private readonly AuthService auth = new();
    private string? correoSolicitado;
    private string? token;
    public vRecuperarPassword(string correoInicial)
    {
        InitializeComponent();
        correo.Text = correoInicial;
        requisitos.Text = ValidadorPassword.Requisitos;
        contenedorCodigo.Children.Add(CrearCasillasCodigo());
    }

    private Grid CrearCasillasCodigo()
    {
        var fila = new Grid { ColumnSpacing = 4 };
        for (int i = 0; i < digitos.Length; i++)
        {
            fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var indice = i;
            var entrada = new Entry { Keyboard = Keyboard.Numeric, HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill, TextColor = Color.FromArgb("#35232A"),
                BackgroundColor = Colors.Transparent, HeightRequest = 50, FontSize = 18 };
#if WINDOWS
            // El TextBox de Windows tiene un ancho mínimo y márgenes internos
            // pensados para campos largos, que desplazan el texto en estas casillas.
            entrada.HandlerChanged += (_, _) =>
            {
                if (entrada.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox caja)
                {
                    caja.MinWidth = 0;
                    caja.Padding = new Microsoft.UI.Xaml.Thickness(0);
                    caja.TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
                    caja.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                }
            };
#endif
            SemanticProperties.SetDescription(entrada, $"Dígito {i + 1} de 8 del código de verificación");
            digitos[i] = entrada;
            entrada.TextChanged += (_, e) => CodigoCambiado(indice, e.OldTextValue, e.NewTextValue);
            var borde = Campo(entrada);
            borde.Padding = 0;
            Grid.SetColumn(borde, i);
            fila.Children.Add(borde);
        }
        return fila;
    }

    private void CodigoCambiado(int indice, string? anterior, string? texto)
    {
        if (actualizandoCodigo) return;
        var numeros = new string((texto ?? "").Where(c => c >= '0' && c <= '9').ToArray());
        if (anterior?.Length == 1 && numeros.Length == 2 && texto?.Length == 2 && texto.StartsWith(anterior, StringComparison.Ordinal))
            numeros = numeros[1..];
        actualizandoCodigo = true;
        try
        {
            if (numeros.Length == 0) digitos[indice].Text = "";
            else
            {
                // Al pegar varios dígitos se distribuyen desde la casilla actual.
                for (int i = 0; i < numeros.Length && indice + i < digitos.Length; i++)
                    digitos[indice + i].Text = numeros[i].ToString();
            }
        }
        finally { actualizandoCodigo = false; }
        if (numeros.Length > 0 && indice + numeros.Length < digitos.Length)
            digitos[indice + numeros.Length].Focus();
    }

    private void LimpiarCodigo()
    {
        actualizandoCodigo = true;
        try { foreach (var digito in digitos) digito.Text = ""; }
        finally { actualizandoCodigo = false; }
    }

    private static Border Campo(Entry entrada) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#9B6574"),
        StrokeThickness = 1.5,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
        Padding = new Thickness(12, 0),
        Content = entrada
    };
    private async void Enviar_Clicked(object? sender, EventArgs e)
    {
        var direccion = correo.Text?.Trim() ?? "";
        if (!MailAddress.TryCreate(direccion, out var valida) || valida.Address != direccion)
        {
            await DisplayAlertAsync("Correo inválido", "Ingresa un correo electrónico válido.", "Aceptar"); return;
        }
        enviar.IsEnabled = false;
        verificar.IsEnabled = cambiar.IsEnabled = clave.IsEnabled = confirmar.IsEnabled = false;
        correoSolicitado = null;
        token = null;
        correo.IsEnabled = true;
        foreach (var digito in digitos) digito.IsEnabled = true;
        clave.Text = confirmar.Text = string.Empty;
        LimpiarCodigo();
        try {
            await auth.SolicitarRecuperacionAsync(direccion);
            correoSolicitado = direccion;
            verificar.IsEnabled = true;
            await DisplayAlertAsync("Revisa tu correo", "Si existe una cuenta con ese correo, recibirás un código de recuperación.", "Aceptar");
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("Recuperación no disponible", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            await DisplayAlertAsync("No se pudo enviar", "Comprueba tu conexión o espera antes de volver a intentarlo.", "Aceptar");
        }
        finally { enviar.IsEnabled = true; }
    }

    private async void Verificar_Clicked(object? sender, EventArgs e)
    {
        if (correoSolicitado == null || !string.Equals(correo.Text?.Trim(), correoSolicitado, StringComparison.OrdinalIgnoreCase))
        { await DisplayAlertAsync("Correo modificado", "Solicita un código para este correo.", "Aceptar"); return; }
        if (digitos.Any(d => string.IsNullOrEmpty(d.Text)))
        { await DisplayAlertAsync("Código incompleto", "Ingresa los 8 dígitos recibidos por correo.", "Aceptar"); return; }
        verificar.IsEnabled = enviar.IsEnabled = false;
        try
        {
            token = await auth.VerificarRecuperacionAsync(correoSolicitado, string.Concat(digitos.Select(d => d.Text)));
            clave.IsEnabled = confirmar.IsEnabled = cambiar.IsEnabled = true;
            foreach (var digito in digitos) digito.IsEnabled = false;
            correo.IsEnabled = false;
            await DisplayAlertAsync("Código verificado", "Ingresa y confirma tu nueva contraseña.", "Aceptar");
            clave.Focus();
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("Código no válido", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { await DisplayAlertAsync("Error de conexión", "Comprueba tu conexión e inténtalo nuevamente.", "Aceptar"); }
        finally { enviar.IsEnabled = true; verificar.IsEnabled = token == null; }
    }

    private async void Cambiar_Clicked(object? sender, EventArgs e)
    {
        if (correoSolicitado == null || !string.Equals(correo.Text?.Trim(), correoSolicitado, StringComparison.OrdinalIgnoreCase)) {
            await DisplayAlertAsync("Correo modificado", "Solicita un código para este correo.", "Aceptar"); return;
        }
        if (token == null) {
            await DisplayAlertAsync("Código pendiente", "Verifica primero el código recibido por correo.", "Aceptar"); return;
        }
        if (!ValidadorPassword.EsValida(clave.Text)) {
            await DisplayAlertAsync("Contraseña inválida", ValidadorPassword.Requisitos, "Aceptar"); return;
        }
        if (clave.Text != confirmar.Text) {
            await DisplayAlertAsync("Contraseñas diferentes", "Las contraseñas deben coincidir.", "Aceptar"); return;
        }
        enviar.IsEnabled = cambiar.IsEnabled = false;
        try {
            await auth.CambiarPasswordAsync(token, clave.Text);
            token = null; LimpiarCodigo(); clave.Text = confirmar.Text = string.Empty;
            await DisplayAlertAsync("Contraseña actualizada", "Ya puedes iniciar sesión con tu nueva contraseña.", "Aceptar");
            await Navigation.PopAsync();
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("No se pudo continuar", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            await DisplayAlertAsync("Error de conexión", "Comprueba tu conexión e inténtalo nuevamente.", "Aceptar");
        }
        finally { enviar.IsEnabled = cambiar.IsEnabled = true; }
    }
}
