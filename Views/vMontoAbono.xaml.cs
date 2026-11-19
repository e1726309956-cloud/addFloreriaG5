
using System.Globalization;

namespace addFloreriaG5.Views;

public partial class vMontoAbono : ContentPage
{
    private readonly decimal saldoPendiente;

    private readonly TaskCompletionSource<decimal?>
        resultado = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

    private bool cerrando;

    public vMontoAbono(int idPedido, decimal saldo)
    {
        InitializeComponent();

        saldoPendiente = saldo;

        lblPedido.Text = $"Pedido #{idPedido}";
        lblSaldo.Text = $"${saldo:N2}";
    }

    // DEVOLVER EL MONTO SELECCIONADO
    public Task<decimal?> EsperarMontoAsync()
    {
        return resultado.Task;
    }

    // VALIDAR Y CONTINUAR
    private async void Continuar_Clicked(
        object sender,
        EventArgs e)
    {
        lblError.IsVisible = false;

        string texto = (txtMonto.Text ?? "")
            .Trim()
            .Replace(',', '.');

        bool esValido = decimal.TryParse(
            texto,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out decimal monto);

        if (!esValido || monto <= 0)
        {
            MostrarError(
                "Ingresa un monto válido mayor que cero.");
            return;
        }

        if (decimal.Round(monto, 2) != monto)
        {
            MostrarError(
                "El monto debe tener máximo dos decimales.");
            return;
        }

        if (monto > saldoPendiente)
        {
            MostrarError(
                $"El abono no puede superar " +
                $"el saldo pendiente (${saldoPendiente:N2}).");
            return;
        }

        cerrando = true;

        await Navigation.PopModalAsync();

        resultado.TrySetResult(monto);
    }

    // CANCELAR
    private async void Cancelar_Clicked(
        object sender,
        EventArgs e)
    {
        cerrando = true;

        await Navigation.PopModalAsync();

        resultado.TrySetResult(null);
    }

    // MOSTRAR MENSAJES DE VALIDACIÓN
    private void MostrarError(string mensaje)
    {
        lblError.Text = mensaje;
        lblError.IsVisible = true;
    }

    // CONTROLAR EL CIERRE CON EL BOTÓN ATRÁS
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!cerrando)
        {
            resultado.TrySetResult(null);
        }
    }
}
