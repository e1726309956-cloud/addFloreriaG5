namespace addFloreriaG5.Views;
using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public partial class vControlEntregas : ContentPage
{
    private readonly Dictionary<int, Guid> solicitudesAbonoPendientes = new();
    public vControlEntregas()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEntregas();
    }

    private async Task CargarEntregas()
    {
        try
        {
            var servicio = new SupabaseService();

            string endpoint = $"{servicio.GetUrl()}/rest/v1/entregas" +
                  "?select=*,pedido:pedidos(*,pagos(*))" +
                  "&order=id_entrega.desc";

            using var respuesta = await servicio.GetClient().GetAsync(endpoint);
            string contenido = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Error", $"No se pudieron cargar las entregas:\n{contenido}", "Aceptar");
                return;
            }

            var entregas = JsonSerializer.Deserialize<List<EntregaModel>>(
                contenido,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<EntregaModel>();

            await CargarArreglosEntregas(entregas, servicio);


            // CARGAR LOS ARREGLOS DE CADA PEDIDO
            await CargarArreglosEntregas(entregas, servicio);

            // MOSTRAR ENTREGAS CON SUS PRODUCTOS
            cvEntregas.ItemsSource = entregas;

            System.Diagnostics.Debug.WriteLine(
                $"Entregas cargadas: {entregas.Count}");

            foreach (var entrega in entregas)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Pedido #{entrega.id_pedido}: " +
                    $"{entrega.Arreglos.Count} arreglos cargados");
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Error", $"No se pudieron cargar las entregas:\n{ex.Message}", "Aceptar");
        }
    }


    private async Task CargarArreglosEntregas(
        List<EntregaModel> entregas,
        SupabaseService servicio)
    {
        if (entregas.Count == 0) return;

        var cliente = servicio.GetClient();
        string url = servicio.GetUrl().TrimEnd('/');

        string idsPedidos = string.Join(",",
            entregas.Select(e => e.id_pedido).Distinct());

        // CONSULTAR DETALLES DE LOS PEDIDOS
        string urlDetalles =
            $"{url}/rest/v1/detalle_pedido" +
            $"?select=id_pedido,id_arreglo,cantidad,precio_unitario" +
            $"&id_pedido=in.({idsPedidos})";

        using var respuestaDetalles =
            await cliente.GetAsync(urlDetalles);

        respuestaDetalles.EnsureSuccessStatusCode();

        var detalles = await respuestaDetalles.Content
            .ReadFromJsonAsync<List<DetalleEntregaConsulta>>()
            ?? new();

        if (detalles.Count == 0) return;

        // CONSULTAR LOS ARREGLOS QUE SE VAN A ENTREGAR
        string idsArreglos = string.Join(",",
            detalles.Select(d => d.id_arreglo).Distinct());

        string urlArreglos =
            $"{url}/rest/v1/arreglos" +
            $"?select=id_arreglo,nombre,imagen" +
            $"&id_arreglo=in.({idsArreglos})";

        using var respuestaArreglos =
            await cliente.GetAsync(urlArreglos);

        respuestaArreglos.EnsureSuccessStatusCode();

        var arreglos = await respuestaArreglos.Content
            .ReadFromJsonAsync<List<ArregloEntregaConsulta>>()
            ?? new();

        var catalogo = arreglos.ToDictionary(a => a.id_arreglo);

        // ASIGNAR TODOS LOS ARREGLOS A CADA ENTREGA
        foreach (var entrega in entregas)
        {
            entrega.Arreglos = detalles
                .Where(d => d.id_pedido == entrega.id_pedido)
                .Select(d =>
                {
                    catalogo.TryGetValue(d.id_arreglo, out var arreglo);

                    return new ArregloEntregaModel
                    {
                        id_arreglo = d.id_arreglo,
                        nombre = arreglo?.nombre ?? "Arreglo no disponible",
                        imagen = arreglo?.imagen ?? "",
                        cantidad = d.cantidad,
                        precio_unitario = d.precio_unitario
                    };
                })
                .ToList();
        }
    }


    private async void btnUbicacion(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EntregaModel entrega)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entrega.direccion))
                {
                    await DisplayAlert("Aviso", "Este pedido no cuenta con una dirección registrada.", "Aceptar");
                    return;
                }

                // Codificamos la dirección para la URL de mapas
                string direccionCodificada = Uri.EscapeDataString(entrega.direccion);
                string urlMapa = $"https://www.google.com/maps/search/?api=1&query={direccionCodificada}";

                await Launcher.OpenAsync(new Uri(urlMapa));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir el mapa: " + ex.Message, "Aceptar");
            }
        }
    }


    private async void MarcarEntregado_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button boton || boton.CommandParameter is not EntregaModel entrega)
            return;

        if (!SesionAuth.TieneSesion)
        {
            await MensajeModal.MostrarAsync(this, "Sesión vencida", "Inicia sesión nuevamente.", "advertencia");
            return;
        }

        if (entrega.estado == "Entregado")
        {
            await MensajeModal.MostrarAsync(this, "Entrega completada", "Este pedido ya fue marcado como entregado.", "advertencia");
            return;
        }

        // CONFIRMACIÓN PERSONALIZADA
        bool confirmar = await MensajeModal.ConfirmarAsync(
            this,
            "Confirmar entrega",
            $"¿Estás seguro de marcar el pedido #{entrega.id_pedido} como ENTREGADO?");

        if (!confirmar) return;

        boton.IsEnabled = false;

        try
        {
            var servicio = new SupabaseService();
            var datos = new { estado = "Entregado" };
            string endpoint = $"{servicio.GetUrl()}/rest/v1/entregas?id_entrega=eq.{entrega.id_entrega}";

            using var solicitud = new HttpRequestMessage(HttpMethod.Patch, endpoint)
            {
                Content = JsonContent.Create(datos)
            };

            solicitud.Headers.TryAddWithoutValidation("Prefer", "return=representation");

            using var respuesta = await servicio.GetClient().SendAsync(solicitud);
            string contenido = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)respuesta.StatusCode}: {contenido}");

            // Verificar que realmente se haya actualizado una fila.
            using var documento = JsonDocument.Parse(contenido);

            if (documento.RootElement.ValueKind != JsonValueKind.Array ||
                documento.RootElement.GetArrayLength() == 0)
                throw new Exception("La entrega no se actualizó. Verifica los permisos de Supabase.");

            await MensajeModal.MostrarAsync(
                this, "Entrega completada",
                $"El pedido #{entrega.id_pedido} fue marcado como entregado correctamente.",
                "exito");

            await CargarEntregas();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR ENTREGA: {ex}");

            await MensajeModal.MostrarAsync(
                this, "No se pudo actualizar la entrega",
                ex.Message, "error");
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }

    private async void btnWhatsApp(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EntregaModel entrega)
        {
            try
            {
                // 1. Obtenemos el teléfono registrado o usamos uno por defecto si está vacío
                string telefonoCrudo = string.IsNullOrWhiteSpace(entrega.telefono_destinatario) ? "593959178595" : entrega.telefono_destinatario.Trim();

                // 2. Limpiamos el número: quitamos espacios, guiones y el símbolo '+'
                string telefonoLimpio = telefonoCrudo.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

                // 3. Si el número empieza con '0' (ej. 0997535512), se lo quitamos y le anteponemos el código de país '593'
                if (telefonoLimpio.StartsWith("0") && telefonoLimpio.Length > 1)
                {
                    telefonoLimpio = "593" + telefonoLimpio.Substring(1);
                }
                // Si el usuario olvidó poner el 0 pero puso los 9 dígitos directos, le agregamos el 593
                else if (!telefonoLimpio.StartsWith("593") && telefonoLimpio.Length == 9)
                {
                    telefonoLimpio = "593" + telefonoLimpio;
                }

                // 4. Armamos el mensaje y la URL limpia para WhatsApp
                string mensaje = Uri.EscapeDataString($"Hola, le escribimos de la florería respecto a la entrega de su Pedido #{entrega.id_pedido}. ¿Se encuentra en la dirección para recibir su arreglo?");
                string urlWhatsApp = $"https://wa.me/{telefonoLimpio}?text={mensaje}";

                await Launcher.OpenAsync(new Uri(urlWhatsApp));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir WhatsApp: " + ex.Message, "Aceptar");
            }
        }

    }


    private async void RegistrarAbonoEntrega_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button boton || boton.CommandParameter is not EntregaModel entrega)
            return;

        if (!SesionAuth.TieneSesion)
        {
            await MensajeModal.MostrarAsync(this, "Sesión vencida", "Inicia sesión nuevamente.", "advertencia");
            return;
        }

        if (entrega.pedido == null)
        {
            await MensajeModal.MostrarAsync(this, "Error", "No se encontró el pedido asociado.", "error");
            return;
        }

        decimal saldoPendiente = entrega.pedido.total;

        if (saldoPendiente <= 0)
        {
            await MensajeModal.MostrarAsync(this, "Pedido pagado", "Este pedido no tiene saldo pendiente.", "advertencia");
            return;
        }

        // 1. SELECCIONAR MÉTODO DE PAGO
        var ventanaMetodo = new vMetodoPago();
        await Navigation.PushModalAsync(ventanaMetodo);

        string? metodoPago = await ventanaMetodo.EsperarSeleccionAsync();
        if (string.IsNullOrWhiteSpace(metodoPago)) return;

        // 2. INGRESAR MONTO DEL ABONO
        var ventanaMonto = new vMontoAbono(entrega.id_pedido, saldoPendiente);
        await Navigation.PushModalAsync(ventanaMonto);

        decimal? montoSeleccionado = await ventanaMonto.EsperarMontoAsync();
        if (!montoSeleccionado.HasValue) return;

        decimal montoAbono = montoSeleccionado.Value;

        // 3. VALIDACIÓN ADICIONAL
        if (montoAbono <= 0 || montoAbono > saldoPendiente ||
            decimal.Round(montoAbono, 2) != montoAbono)
        {
            await MensajeModal.MostrarAsync(this, "Monto inválido", "El abono debe ser mayor que cero y no superar el saldo pendiente.", "advertencia");
            return;
        }

        // 4. CONFIRMAR PAGO
        bool confirmar = await MensajeModal.ConfirmarAsync(
            this, "Confirmar abono",
            $"Pedido #{entrega.id_pedido}\nMonto: ${montoAbono:F2}\nMétodo: {metodoPago}\n\n¿Confirmas que recibiste este dinero?");

        if (!confirmar) return;

        boton.IsEnabled = false;

        try
        {
            // 5. REGISTRAR PAGO EN SUPABASE
            if (!solicitudesAbonoPendientes.TryGetValue(entrega.id_pedido, out Guid solicitudId))
            {
                solicitudId = Guid.NewGuid();
                solicitudesAbonoPendientes[entrega.id_pedido] = solicitudId;
            }

            var pagosService = new PagosService();

            ResultadoPago resultado = await pagosService.RegistrarAbonoAsync(
                entrega.id_pedido, montoAbono, metodoPago, solicitudId);

            solicitudesAbonoPendientes.Remove(entrega.id_pedido);

            // 6. MOSTRAR RESULTADO
            string mensaje = $"Abono registrado: ${montoAbono:F2}\n" +
                             $"Saldo pendiente: ${resultado.SaldoRestante:F2}\n" +
                             $"Estado: {resultado.Estado}";

            if (resultado.SaldoRestante == 0)
                mensaje += "\n\n¡El pedido está pagado al 100 %!";

            await MensajeModal.MostrarAsync(this, "Pago registrado", mensaje, "exito");

            // 7. ACTUALIZAR ENTREGAS
            await CargarEntregas();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR AL REGISTRAR ABONO EN ENTREGA: {ex}");

            await MensajeModal.MostrarAsync(
                this, "No se pudo confirmar",
                "No se pudo verificar el registro del abono. Revisa el historial antes de repetirlo.\n\n" + ex.Message,
                "error");
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }


    private async void Volver_Tapped(object sender, TappedEventArgs e)
    {
        if (Navigation.ModalStack.Count > 0 &&
            Navigation.ModalStack[^1] == this)
        {
            await Navigation.PopModalAsync();
        }
        else if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
        else if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

}