namespace addFloreriaG5.Views;

using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;
using addFloreriaG5.Views;

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
        // Mostramos los elementos en el CollectionView
        cvCarrito.ItemsSource = null;
        cvCarrito.ItemsSource = CarritoCompras.Items;

        // Autocompletamos automáticamente con los datos del usuario que inició sesión
        if (string.IsNullOrWhiteSpace(txtNombreCliente.Text))
        {
            txtNombreCliente.Text = SesionUsuario.Nombre;   // Carga el nombre del login
            txtTelefono.Text = SesionUsuario.Telefono;     // Carga el teléfono del login
        }

        CalcularTotalesGenerales();
    }

    private void CalcularTotalesGenerales()
    {
        decimal subtotalGeneral = 0.00m;
        foreach (var item in CarritoCompras.Items)
        {
            subtotalGeneral += item.Subtotal;
        }

        decimal ivaGeneral = subtotalGeneral * 0.15m; // 15% IVA
        decimal totalGeneral = subtotalGeneral + ivaGeneral;

        lblSubtotal.Text = $"${subtotalGeneral:F2}";
        lblIva.Text = $"${ivaGeneral:F2}";
        lblTotal.Text = $"${totalGeneral:F2}";
    }

    // Botón: Seguir Comprando (Devuelve al catálogo para elegir más)
    private async void OnSeguirComprandoClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // Método para actualizar la cantidad cuando el usuario la edita en la lista
    private void OnCantidadEntryChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is ItemCarrito itemSeleccionado)
        {
            if (int.TryParse(e.NewTextValue, out int nuevaCantidad) && nuevaCantidad > 0)
            {
                itemSeleccionado.Cantidad = nuevaCantidad;
                CalcularTotalesGenerales();
            }
            else if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                itemSeleccionado.Cantidad = 1;
                CalcularTotalesGenerales();
            }
        }
    }



    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (CarritoCompras.Items.Count == 0)
        {
            await DisplayAlert("Carrito vacío", "No tienes ningún arreglo seleccionado.", "Aceptar");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtNombreCliente.Text) ||
            string.IsNullOrWhiteSpace(txtTelefono.Text) ||
            string.IsNullOrWhiteSpace(txtDireccion.Text))
        {
            await DisplayAlert("Campos obligatorios", "La dirección de entrega, el nombre y el teléfono son obligatorios.", "Aceptar");
            return;
        }

        try
        {
            SupabaseService servicio = new SupabaseService();
            var client = servicio.GetClient();

            // Calculamos el total final a guardar
            decimal totalFinal = CarritoCompras.Items.Sum(i => i.Subtotal) * 1.15m;

            // PASO 1: Guardar la cabecera en la tabla 'pedidos' (¡INCLUYENDO LA DIRECCIÓN DE ENVÍO!)
            var nuevoPedido = new
            {
                id_cliente = 1,
                fecha_pedido = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                total = totalFinal,
                estado = "Pendiente",
                direccion_envio = txtDireccion.Text.Trim(),
                telefono_envio = txtTelefono.Text.Trim() // <-- ¡AQUÍ ESTABA EL DETALLE! Guardamos el teléfono real
            };

            string jsonPedido = JsonSerializer.Serialize(nuevoPedido);
            StringContent contenidoPedido = new StringContent(jsonPedido, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Prefer", "return=representation");

            string endpointPedido = $"{servicio.GetUrl()}/rest/v1/pedidos";
            var respuestaPedido = await client.PostAsync(endpointPedido, contenidoPedido);

            if (!respuestaPedido.IsSuccessStatusCode)
            {
                string error = await respuestaPedido.Content.ReadAsStringAsync();
                await DisplayAlert("Error", "No se pudo registrar el pedido: " + error, "Aceptar");
                return;
            }

            // Obtenemos el id_pedido recién creado
            string resultadoJson = await respuestaPedido.Content.ReadAsStringAsync();
            var listaPedidosCreados = JsonSerializer.Deserialize<List<PedidoCreadoModel>>(resultadoJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            int idPedidoGenerado = listaPedidosCreados.FirstOrDefault()?.id_pedido ?? 0;

            if (idPedidoGenerado > 0)
            {
                // PASO 2: Recorrer CADA producto del carrito, guardarlo en 'detalle_pedido' y actualizar su stock
                bool errorDetalleOcurrido = false;

                foreach (var item in CarritoCompras.Items)
                {
                    // A. Guardar el detalle del pedido
                    var detallePedido = new
                    {
                        id_pedido = idPedidoGenerado,
                        id_arreglo = item.Arreglo.id_arreglo,
                        cantidad = item.Cantidad,
                        precio_unitario = item.Arreglo.precio,
                        subtotal = item.Subtotal
                    };

                    string jsonDetalle = JsonSerializer.Serialize(detallePedido);
                    StringContent contenidoDetalle = new StringContent(jsonDetalle, Encoding.UTF8, "application/json");
                    string endpointDetalle = $"{servicio.GetUrl()}/rest/v1/detalle_pedido";
                    var respuestaDetalle = await client.PostAsync(endpointDetalle, contenidoDetalle);

                    if (respuestaDetalle.IsSuccessStatusCode)
                    {
                        // B. DISMINUIR EL STOCK EN LA TABLA 'arreglos'
                        int nuevoStock = item.Arreglo.stock - item.Cantidad;
                        if (nuevoStock < 0) nuevoStock = 0; // Evita stock negativo

                        var actualizarStock = new
                        {
                            stock = nuevoStock
                        };

                        string jsonStock = JsonSerializer.Serialize(actualizarStock);
                        StringContent contenidoStock = new StringContent(jsonStock, Encoding.UTF8, "application/json");

                        string endpointStock = $"{servicio.GetUrl()}/rest/v1/arreglos?id_arreglo=eq.{item.Arreglo.id_arreglo}";

                        var requestStock = new HttpRequestMessage(new HttpMethod("PATCH"), endpointStock)
                        {
                            Content = contenidoStock
                        };
                        requestStock.Headers.Add("Prefer", "return=minimal");

                        await client.SendAsync(requestStock);

                        // Actualizamos el objeto local en memoria por si el usuario sigue navegando
                        item.Arreglo.stock = nuevoStock;
                    }
                    else
                    {
                        errorDetalleOcurrido = true;
                    }
                }

                if (!errorDetalleOcurrido)
                {
                    await DisplayAlert("¡Éxito!", "¡Tu pedido múltiple ha sido registrado con éxito y el stock se ha actualizado!", "Aceptar");

                    // Limpiamos el carrito tras la compra exitosa
                    CarritoCompras.Limpiar();

                    // Obtenemos únicamente el texto del nombre digitado en el Entry
                    string nombreCliente = txtNombreCliente.Text;

                    // Enviamos solo el nombre al menú administrador
                    Navigation.InsertPageBefore(new vMenuAdministrador(nombreCliente), Navigation.NavigationStack.First());
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Advertencia", "El pedido se creó, pero hubo problemas registrando algunos detalles.", "Aceptar");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }

    }
}

public class PedidoCreadoModel
{
    public int id_pedido { get; set; }
}