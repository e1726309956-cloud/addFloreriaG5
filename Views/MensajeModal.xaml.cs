
namespace addFloreriaG5.Views;

public partial class MensajeModal : ContentPage
{
    private readonly TaskCompletionSource<bool> _cerrado = new();

    public MensajeModal(
        string titulo,
        string mensaje,
        string tipo = "exito")
    {
        InitializeComponent();

        lblTitulo.Text = titulo;
        lblMensaje.Text = mensaje;

        switch (tipo.ToLowerInvariant())
        {
            case "error":
                lblIcono.Text = "!";
                lblIcono.TextColor = Color.FromArgb("#B44353");
                btnAceptar.Text = "Entendido";
                break;

            case "advertencia":
                lblIcono.Text = "!";
                lblIcono.TextColor = Color.FromArgb("#C18C44");
                btnAceptar.Text = "Entendido";
                break;

            default:
                lblIcono.Text = "✓";
                lblIcono.TextColor = Color.FromArgb("#9B3E5C");
                btnAceptar.Text = "Continuar";
                break;
        }
    }

    public static async Task MostrarAsync(
        ContentPage pagina,
        string titulo,
        string mensaje,
        string tipo = "exito")
    {
        var modal = new MensajeModal(titulo, mensaje, tipo);

        await pagina.Navigation.PushModalAsync(modal);
        await modal._cerrado.Task;
    }

    private async void Cerrar_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cerrado.TrySetResult(true);
    }
}
