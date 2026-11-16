namespace addFloreriaG5.Views;

using addFloreriaG5.Models;
using addFloreriaG5.Services;
using addFloreriaG5.Views;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public partial class vProcesoCompra : ContentPage
{

    public vProcesoCompra(ArregloModel arregloRecibido)
	{
		InitializeComponent();
        // Por defecto le asignamos una cantidad inicial de 1 al primer producto
        CarritoCompras.AgregarItem(arregloRecibido, 1);

        CargarDatosPantalla();
    }

    // Constructor vacío por si entra directamente desde el botón "Seguir Comprando" sin re-agregar nada
    public vProcesoCompra()
    {
        InitializeComponent();
        CargarDatosPantalla();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarDatosPantalla();
    }


    private void CargarDatosPantalla()
    {
        // Actualizar el listado
        cvCarrito.ItemsSource = null;
        cvCarrito.ItemsSource = CarritoCompras.Items;

        // Ajustar altura según la cantidad de productos
        int cantidadProductos = CarritoCompras.Items.Count;

        cvCarrito.HeightRequest = cantidadProductos == 0
            ? 80
            : Math.Min(cantidadProductos * 115, 350);

        // Completar datos del usuario
        if (string.IsNullOrWhiteSpace(txtNombreCliente.Text))
        {
            txtNombreCliente.Text = SesionUsuario.Nombre;
        }

        if (string.IsNullOrWhiteSpace(txtTelefono.Text))
        {
            txtTelefono.Text = SesionUsuario.Telefono;
        }

        // Actualizar totales
        CalcularTotalesGenerales();
    }
    private void CalcularTotalesGenerales()
    {
        decimal subtotalGeneral = 0.00m;

        foreach (var item in CarritoCompras.Items)
        {
            subtotalGeneral += item.Subtotal;
        }

        // El total mostrado no añade IVA
        decimal totalGeneral = subtotalGeneral;

        lblSubtotal.Text = $"${subtotalGeneral:F2}";
        lblTotal.Text = $"${totalGeneral:F2}";
    }


    // Botón: Seguir Comprando (Devuelve al catálogo para elegir más)
    private async void OnSeguirComprandoClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // Método para actualizar la cantidad cuando el usuario la edita en la lista


    private void OnCantidadEntryChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is not Entry entry ||
            entry.BindingContext is not ItemCarrito item)
            return;

        // Validar que se ingrese un número
        if (!int.TryParse(e.NewTextValue, out int cantidad))
            return;

        // Validar cantidad y stock disponible
        if (cantidad < 1 || cantidad > item.Arreglo.stock)
            return;

        // Actualizar la cantidad del producto
        item.Cantidad = cantidad;

        // Actualizar únicamente los totales generales
        CalcularTotalesGenerales();
    }



    private async void Button_Clicked(
        object sender,
        EventArgs e)
    {
        if (!btnConfirmarPedido.IsEnabled)
            return;

        // VALIDAR SESIÓN
        if (!SesionAuth.TieneSesion)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Sesión vencida",
                "Vuelve a iniciar sesión para realizar tu pedido.",
                "advertencia");
            return;
        }

        // VALIDAR CARRITO
        if (CarritoCompras.Items.Count == 0)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Carrito vacío",
                "Selecciona al menos un arreglo floral.",
                "advertencia");
            return;
        }

        // VALIDAR DATOS
        string nombre = txtNombreCliente.Text?.Trim() ?? "";
        string telefono = txtTelefono.Text?.Trim() ?? "";
        string direccion = txtDireccion.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(telefono) ||
            string.IsNullOrWhiteSpace(direccion))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Campos obligatorios",
                "Completa el nombre, teléfono y dirección de entrega.",
                "advertencia");
            return;
        }

        // VALIDAR CANTIDADES
        if (CarritoCompras.Items.Any(
            x => x.Cantidad <= 0 ||
                 x.Cantidad > x.Arreglo.stock))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Cantidad inválida",
                "Revisa las cantidades y existencias disponibles.",
                "advertencia");
            return;
        }

        bool confirmar = await MensajeModal.ConfirmarAsync(
            this,
            "Confirmar pedido",
            "¿Deseas registrar tu pedido con los arreglos seleccionados?");

        if (!confirmar)
            return;

        btnConfirmarPedido.IsEnabled = false;
        btnConfirmarPedido.Text = "Registrando pedido...";

        try
        {
            var servicio = new SupabaseService();

            // Enviar únicamente ID y cantidad.
            // Supabase calculará precios y totales.
            var productos = CarritoCompras.Items
                .Select(x => new
                {
                    id_arreglo = x.Arreglo.id_arreglo,
                    cantidad = x.Cantidad
                })
                .ToList();

            var solicitud = new
            {
                p_items = productos,
                p_direccion = direccion,
                p_telefono = telefono
            };

            string endpoint =
                $"{servicio.GetUrl()}/rest/v1/rpc/confirmar_pedido_nova";

            using var respuesta = await servicio
                .GetClient()
                .PostAsJsonAsync(endpoint, solicitud);

            if (!respuesta.IsSuccessStatusCode)
            {
                string error =
                    await respuesta.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Error al registrar pedido: {error}");

                await MensajeModal.MostrarAsync(
                    this,
                    "No se pudo registrar",
                    "Verifica las existencias de los arreglos e inténtalo nuevamente.",
                    "error");
                return;
            }

            // La función devuelve el ID del pedido
            int idPedido = await respuesta.Content
                .ReadFromJsonAsync<int>();

            if (idPedido <= 0)
                throw new InvalidOperationException(
                    "Supabase no devolvió un pedido válido.");

            // Limpiar carrito después del éxito
            CarritoCompras.Limpiar();

            await MensajeModal.MostrarAsync(
                this,
                "¡Pedido registrado!",
                $"Tu pedido #{idPedido} se registró correctamente.",
                "exito");

            // Regresar al catálogo del cliente
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "No se pudo confirmar",
                "No se pudo verificar el resultado del pedido. Consulta tus pedidos antes de volver a intentarlo.",
                "advertencia");
        }
        finally
        {
            btnConfirmarPedido.IsEnabled = true;
            btnConfirmarPedido.Text = "Confirmar pedido";
        }
    }


    private async void Volver_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class PedidoCreadoModel
{
    public int id_pedido { get; set; }
}