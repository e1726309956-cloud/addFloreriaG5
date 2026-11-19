
namespace addFloreriaG5.Views;

public partial class vMetodoPago : ContentPage
{
    private readonly TaskCompletionSource<string?>
        resultadoSeleccion = new();

    public vMetodoPago()
    {
        InitializeComponent();
    }

    public Task<string?> EsperarSeleccionAsync()
    {
        return resultadoSeleccion.Task;
    }

    private async Task SeleccionarAsync(string? metodo)
    {
        if (resultadoSeleccion.Task.IsCompleted)
            return;

        await Navigation.PopModalAsync();

        resultadoSeleccion.TrySetResult(metodo);
    }

    private async void Transferencia_Clicked(
        object sender, EventArgs e)
    {
        await SeleccionarAsync("Transferencia");
    }

    private async void Efectivo_Clicked(
        object sender, EventArgs e)
    {
        await SeleccionarAsync("Efectivo");
    }

    private async void Cancelar_Clicked(
        object sender, EventArgs e)
    {
        await SeleccionarAsync(null);
    }

    protected override bool OnBackButtonPressed()
    {
        resultadoSeleccion.TrySetResult(null);
        return base.OnBackButtonPressed();
    }
}
