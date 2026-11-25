namespace addFloreriaG5.Views;
using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

public partial class vCatalogoArreglos : ContentPage
{
    // Lista de arreglos obtenidos desde Supabase
    private List<ArregloModel> listaCompleta = new();

    // Filtro seleccionado
    private string filtroActual = "Todos";

    // Determina el modo visual del catálogo
    private readonly bool esAdministrador;

    public vCatalogoArreglos(bool esAdministrador = true)
    {
        InitializeComponent();

        this.esAdministrador = esAdministrador;

        // Los clientes no necesitan filtros administrativos
        panelFiltros.IsVisible = esAdministrador;

        // Solo el administrador agrega productos
        btnNuevoArreglo.IsVisible = esAdministrador;

        // Botón del carrito exclusivo para clientes
        btnVerCarrito.IsVisible = !esAdministrador;

        // Cambiar el título según el rol
        lblTituloCatalogo.Text = esAdministrador
            ? "Catálogo de Arreglos"
            : "Nuestros Arreglos";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarArreglos();
    }


    private async Task CargarArreglos()
    {
        try
        {
            SupabaseService servicio = new SupabaseService();

            // Administrador: todos los arreglos.
            // Cliente: únicamente arreglos activos.
            string endpoint = esAdministrador
                ? $"{servicio.GetUrl()}/rest/v1/arreglos?select=*&order=id_arreglo.desc"
                : $"{servicio.GetUrl()}/rest/v1/arreglos?select=*&estado=eq.true&order=id_arreglo.desc";

            var respuesta = await servicio
                .GetClient()
                .GetAsync(endpoint);

            if (respuesta.IsSuccessStatusCode)
            {
                string contenido =
                    await respuesta.Content.ReadAsStringAsync();

                listaCompleta =
                    JsonSerializer.Deserialize<List<ArregloModel>>(
                        contenido,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<ArregloModel>();

                // VISIBILIDAD SEGÚN EL MODO DEL CATÁLOGO
                foreach (var arreglo in listaCompleta)
                {
                    arreglo.MostrarAccionesAdmin =
                        esAdministrador;

                    arreglo.MostrarAccionesCliente =
                        !esAdministrador &&
                        arreglo.estado &&
                        arreglo.stock > 0;
                }

                // Actualizar controles
                if (esAdministrador)
                {
                    ActualizarContadores();
                    ActualizarEstiloFiltros();
                }

                AplicarFiltros();
            }
            else
            {
                string error =
                    await respuesta.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Error de catálogo: {error}");

                await MensajeModal.MostrarAsync(
                    this,
                    "Error",
                    "No se pudieron cargar los arreglos.",
                    "error");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error",
                "Ocurrió un problema al cargar el catálogo.",
                "error");
        }
    }



    private void AplicarFiltros()
    {
        IEnumerable<ArregloModel> consulta = esAdministrador
            ? listaCompleta
            : listaCompleta.Where(x => x.estado);

        // Filtros solo para administrador
        if (esAdministrador)
        {
            if (filtroActual == "Activos")
            {
                consulta = consulta.Where(x => x.estado);
            }
            else if (filtroActual == "Inactivos")
            {
                consulta = consulta.Where(x => !x.estado);
            }
        }

        // Búsqueda compartida
        string texto = txtBuscar.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(texto))
        {
            consulta = consulta.Where(x =>
                (x.nombre?.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.descripcion?.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        cvArreglos.ItemsSource = consulta.ToList();
    }

    private void ActualizarContadores()
    {
        int total = listaCompleta.Count;
        int activos = listaCompleta.Count(x => x.estado == true);
        int inactivos = listaCompleta.Count(x => x.estado == false);

        btnTodos.Text = $"Todos ({total})";
        btnActivos.Text = $"Activos ({activos})";
        btnInactivos.Text = $"Inactivos ({inactivos})";
    }

    private void ActualizarEstiloFiltros()
    {
        ResetearBotonFiltro(btnTodos);
        ResetearBotonFiltro(btnActivos);
        ResetearBotonFiltro(btnInactivos);

        switch (filtroActual)
        {
            case "Todos":
                ActivarBotonFiltro(btnTodos);
                break;
            case "Activos":
                ActivarBotonFiltro(btnActivos);
                break;
            case "Inactivos":
                ActivarBotonFiltro(btnInactivos);
                break;
        }
    }

    private void ResetearBotonFiltro(Button boton)
    {
        boton.BackgroundColor = Colors.White;
        boton.TextColor = Color.FromArgb("#7D273E");
        boton.BorderColor = Color.FromArgb("#CDB6BD");
        boton.BorderWidth = 1.2;
    }

    private void ActivarBotonFiltro(Button boton)
    {
        boton.BackgroundColor = Color.FromArgb("#9B3E5C");
        boton.TextColor = Colors.White;
        boton.BorderColor = Color.FromArgb("#9B3E5C");
        boton.BorderWidth = 1.2;
    }

    private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void btnTodos_Clicked(object sender, EventArgs e)
    {
        filtroActual = "Todos";
        ActualizarEstiloFiltros();
        AplicarFiltros();
    }

    private void btnActivos_Clicked(object sender, EventArgs e)
    {
        filtroActual = "Activos";
        ActualizarEstiloFiltros();
        AplicarFiltros();
    }

    private void btnInactivos_Clicked(object sender, EventArgs e)
    {
        filtroActual = "Inactivos";
        ActualizarEstiloFiltros();
        AplicarFiltros();
    }

    private async void Volver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void btnNuevoArreglo_Clicked(object sender, EventArgs e)
    {
        if (!esAdministrador)
            return;
        await Navigation.PushAsync(new vRegistroArreglo());
    }


    private void btnDetalles(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is ArregloModel arreglo)
        {
            lblDetalleNombre.Text = arreglo.nombre;

            lblDetalleDescripcion.Text = arreglo.descripcion;

            lblDetallePrecio.Text = $"${arreglo.precio:F2}";

            lblDetalleStock.Text = $"{arreglo.stock} unidades";

            lblDetalleEstado.Text =
                arreglo.estado ? "● Activo" : "● Inactivo";

            lblDetalleEstado.TextColor = Color.FromArgb(
                arreglo.estado ? "#698356" : "#A04B64");

            // Imagen del arreglo
            imgDetalle.Source = null;

            if (!string.IsNullOrWhiteSpace(arreglo.imagen))
            {
                if (Uri.TryCreate(
                    arreglo.imagen,
                    UriKind.Absolute,
                    out var uri) &&
                    (uri.Scheme == "https" || uri.Scheme == "http"))
                {
                    imgDetalle.Source = ImageSource.FromUri(uri);
                }
                else
                {
                    imgDetalle.Source =
                        ImageSource.FromFile(arreglo.imagen);
                }
            }

            panelDetalles.IsVisible = true;
        }
    }

    private void CerrarDetalles_Clicked(object sender, EventArgs e)
    {
        panelDetalles.IsVisible = false;
    }


    private async void btnEditar(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            await Navigation.PushAsync(new vEditarArreglo(arreglo));
        }
    }

    private async void btnCambiarEstadoArreglo_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            bool nuevoEstado = !arreglo.estado;

            string accion = nuevoEstado ? "activar" : "desactivar";
            bool confirmar = await MensajeModal.ConfirmarAsync(
                    this,
                    nuevoEstado ? "Activar arreglo" : "Desactivar arreglo",
                    $"¿Confirmas el cambio de estado de '{arreglo.nombre}'?"
            );
            if (!confirmar)
                return;

            try
            {
                SupabaseService servicio = new SupabaseService();
                var client = servicio.GetClient();

                var actualizarEstado = new
                {
                    estado = nuevoEstado
                };

                string json = JsonSerializer.Serialize(actualizarEstado);
                StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

                string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?id_arreglo=eq.{arreglo.id_arreglo}";

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                {
                    Content = contenido
                };

                request.Headers.Add("Prefer", "return=minimal");

                var respuesta = await client.SendAsync(request);

                if (respuesta.IsSuccessStatusCode)
                {
                    string mensaje = nuevoEstado
                        ? "El arreglo ha sido activado correctamente."
                        : "El arreglo ha sido desactivado correctamente.";

                    await DisplayAlert("Éxito", mensaje, "Aceptar");
                    await CargarArreglos();
                }
                else
                {
                    string error = await respuesta.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", "No se pudo actualizar el estado: " + error, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }
    }

    // AGREGAR ARREGLO AL CARRITO
    private async void AgregarCarrito_Clicked(
        object sender,
        EventArgs e)
    {
        if (esAdministrador)
            return;

        if (sender is not Button boton ||
            boton.CommandParameter is not ArregloModel arreglo)
            return;

        if (!arreglo.estado || arreglo.stock <= 0)
        {
            await MensajeModal.MostrarAsync(
                this,
                "No disponible",
                "Este arreglo no se encuentra disponible.",
                "advertencia");
            return;
        }

        // Validar cantidad que ya tiene en el carrito
        int cantidadActual = CarritoCompras.Items
            .Where(i => i.Arreglo.id_arreglo == arreglo.id_arreglo)
            .Sum(i => i.Cantidad);

        if (cantidadActual >= arreglo.stock)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Stock insuficiente",
                "Ya agregaste la cantidad disponible.",
                "advertencia");
            return;
        }

        CarritoCompras.AgregarItem(arreglo, 1);

        await MensajeModal.MostrarAsync(
            this,
            "Agregado correctamente",
            $"«{arreglo.nombre}» fue agregado al carrito.",
            "exito");
    }

    // ABRIR CARRITO
    private async void VerCarrito_Clicked(
        object sender,
        EventArgs e)
    {
        if (esAdministrador)
            return;

        if (CarritoCompras.Items.Count == 0)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Carrito vacío",
                "Selecciona al menos un arreglo.",
                "advertencia");
            return;
        }

        await Navigation.PushAsync(
            new vProcesoCompra());
    }

}