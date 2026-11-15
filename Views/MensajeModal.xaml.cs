
namespace addFloreriaG5.Views;

public partial class MensajeModal : ContentPage
{
    private readonly TaskCompletionSource<bool> _cerrado = new();

    private bool? _respuestaElegida;

    public MensajeModal(string titulo, string mensaje, string tipo = "exito", bool esConfirmacion = false)
    {
        InitializeComponent();

        lblTitulo.Text = titulo;
        lblMensaje.Text = mensaje;

        // Configurar botones
        btnCancelar.IsVisible = esConfirmacion;

        if (!esConfirmacion)
        {
            Grid.SetColumn(btnAceptar, 0);
            Grid.SetColumnSpan(btnAceptar, 2);
        }

        // Configurar estilo del mensaje
        switch (tipo.ToLowerInvariant())
        {
            case "error":
                lblIcono.Text = "!";
                lblIcono.TextColor =
                    Color.FromArgb("#B44353");

                btnAceptar.Text = esConfirmacion
                    ? "Sí"
                    : "Entendido";
                break;

            case "advertencia":
                lblIcono.Text = "!";
                lblIcono.TextColor =
                    Color.FromArgb("#C18C44");

                btnAceptar.Text = esConfirmacion
                    ? "Sí"
                    : "Entendido";
                break;

            default:
                lblIcono.Text = "✓";
                lblIcono.TextColor =
                    Color.FromArgb("#9B3E5C");

                btnAceptar.Text = esConfirmacion
                    ? "Sí"
                    : "Continuar";
                break;
        }
    }

    // MENSAJES INFORMATIVOS
    public static async Task MostrarAsync(ContentPage pagina, string titulo, string mensaje, string tipo = "exito")
    {
        var modal = new MensajeModal(
            titulo,
            mensaje,
            tipo);

        await pagina.Navigation.PushModalAsync(modal);

        await modal._cerrado.Task;
    }

    // MENSAJES DE CONFIRMACIÓN
    public static async Task<bool> ConfirmarAsync( ContentPage pagina, string titulo, string mensaje)
    {
        var modal = new MensajeModal(titulo, mensaje, "advertencia", true);

        await pagina.Navigation.PushModalAsync(modal);

        return await modal._cerrado.Task;
    }

    // BOTÓN ACEPTAR / SÍ
    private async void Cerrar_Clicked( object sender, EventArgs e)
    {
        btnAceptar.IsEnabled = false;
        btnCancelar.IsEnabled = false;

        _respuestaElegida = true;

        await Navigation.PopModalAsync();

        _cerrado.TrySetResult(true);
    }

    // BOTÓN CANCELAR / NO
    private async void Cancelar_Clicked(object sender, EventArgs e)
    {
        btnAceptar.IsEnabled = false;
        btnCancelar.IsEnabled = false;

        _respuestaElegida = false;

        await Navigation.PopModalAsync();

        _cerrado.TrySetResult(false);
    }

    // SI SE CIERRA EL MODAL SIN ELEGIR
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _cerrado.TrySetResult(
            _respuestaElegida ?? false);
    }
}
