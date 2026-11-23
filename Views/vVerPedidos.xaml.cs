namespace addFloreriaG5.Views;

using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text.Json;
using System.Text;


public partial class vVerPedidos : ContentPage
{
    private readonly bool modoAdministradorSolicitado;
    private bool esAdministrador;
    private int idClienteActual;
    public vVerPedidos(bool esAdministrador = true)
    {
        InitializeComponent();
        modoAdministradorSolicitado = esAdministrador;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            bool autorizado = await ValidarAccesoAsync();

            if (!autorizado)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Acceso denegado",
                    "No tienes autorización para consultar esta sección.",
                    "error");

                await Navigation.PopAsync();
                return;
            }

            await CargarPedidos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error",
                "No se pudo validar tu sesión.",
                "error");

            await Navigation.PopAsync();
        }
    }




    private void WhatsApp_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (!esAdministrador)
            return;

        if (e.Parameter is not PedidoModel pedido)
            return;

        var boton = new Button
        {
            CommandParameter = pedido
        };

        Button_Clicked(boton, EventArgs.Empty);
    }


    private async Task CargarPedidos()
    {
        try
        {
            var servicio = new SupabaseService();
            var client = servicio.GetClient();
            string url = servicio.GetUrl();

            // 1. CONSULTAR PEDIDOS
            string endpoint = esAdministrador
                ? $"{url}/rest/v1/pedidos?select=*&order=id_pedido.desc"
                : $"{url}/rest/v1/pedidos?select=*&id_cliente=eq.{idClienteActual}&order=id_pedido.desc";

            using var respuesta = await client.GetAsync(endpoint);

            string json = await respuesta.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"PEDIDOS HTTP {(int)respuesta.StatusCode}: {json}");

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert(
                    "Error al consultar pedidos",
                    $"HTTP {(int)respuesta.StatusCode}\n\n{json}",
                    "Aceptar");
                return;
            }

            var pedidos = JsonSerializer.Deserialize<List<PedidoModel>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PedidoModel>();

            System.Diagnostics.Debug.WriteLine(
                $"Pedidos encontrados: {pedidos.Count}");

            foreach (var pedido in pedidos)
            {
                pedido.MostrarAccionesAdmin = esAdministrador;
                pedido.MostrarAccionesCliente = !esAdministrador;
            }

            // Mostrar inmediatamente los pedidos
            cvPedidos.ItemsSource = pedidos;

            if (pedidos.Count == 0)
                return;

            // 2. RECUPERAR DATOS ADICIONALES
            // Si fallan, conservar el listado principal.
            try
            {
                string ids = string.Join(
                    ",",
                    pedidos.Select(p => p.id_pedido));

                // PAGOS
                using var respuestaPagos = await client.GetAsync(
                    $"{url}/rest/v1/pagos?select=*&id_pedido=in.({ids})");

                if (respuestaPagos.IsSuccessStatusCode)
                {
                    string jsonPagos =
                        await respuestaPagos.Content.ReadAsStringAsync();

                    var pagos = JsonSerializer.Deserialize<List<PagoModel>>(
                        jsonPagos,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<PagoModel>();

                    foreach (var pedido in pedidos)
                    {
                        pedido.pagos = pagos
                            .Where(p => p.id_pedido == pedido.id_pedido)
                            .ToList();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "No se pudieron consultar los pagos: " +
                        await respuestaPagos.Content.ReadAsStringAsync());
                }

                // DETALLES DE PEDIDOS
                using var respuestaDetalles = await client.GetAsync(
                    $"{url}/rest/v1/detalle_pedido" +
                    $"?select=id_pedido,id_arreglo&id_pedido=in.({ids})");

                if (respuestaDetalles.IsSuccessStatusCode)
                {
                    string jsonDetalles =
                        await respuestaDetalles.Content.ReadAsStringAsync();

                    var detalles = JsonSerializer.Deserialize<
                        List<DetalleImagenPedido>>(
                            jsonDetalles,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            }) ?? new List<DetalleImagenPedido>();

                    if (detalles.Count > 0)
                    {
                        // ARREGLOS
                        using var respuestaArreglos =
                            await client.GetAsync(
                                $"{url}/rest/v1/arreglos" +
                                "?select=id_arreglo,nombre,imagen");

                        if (respuestaArreglos.IsSuccessStatusCode)
                        {
                            string jsonArreglos =
                                await respuestaArreglos.Content.ReadAsStringAsync();

                            var arreglos = JsonSerializer.Deserialize<
                                List<ArregloImagenPedido>>(
                                    jsonArreglos,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    }) ?? new List<ArregloImagenPedido>();

                            foreach (var pedido in pedidos)
                            {
                                var detalle = detalles.FirstOrDefault(
                                    d => d.id_pedido == pedido.id_pedido);

                                var arreglo = arreglos.FirstOrDefault(
                                    a => a.id_arreglo ==
                                         detalle?.id_arreglo);

                                if (arreglo != null)
                                {
                                    pedido.nombreArreglo = arreglo.nombre;

                                    pedido.imagenArreglo = arreglo.imagen?.Trim()
                                        ?? string.Empty;

                                    System.Diagnostics.Debug.WriteLine(
                                        $"PEDIDO #{pedido.id_pedido} | " +
                                        $"ARREGLO: {arreglo.nombre} | " +
                                        $"IMAGEN: {pedido.imagenArreglo}");
                                }
                            }
                        }
                    }
                }

                // Actualizar después de recuperar información adicional
                cvPedidos.ItemsSource = pedidos.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Datos adicionales no disponibles: {ex}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlert(
                "Error de carga",
                ex.Message,
                "Aceptar");
        }


    }


    // MODELOS AUXILIARES PARA LA CONSULTA
    private class DetalleImagenPedido
    {
        public int id_pedido { get; set; }
        public int id_arreglo { get; set; }
    }

    private class ArregloImagenPedido
    {
        public int id_arreglo { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string imagen { get; set; } = string.Empty;
    }


    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PedidoModel pedido)
        {
            try
            {
                // 1. Obtenemos el teléfono registrado o usamos uno por defecto si está vacío
                string telefonoCrudo = string.IsNullOrWhiteSpace(pedido.telefono_envio) ? "593959178595" : pedido.telefono_envio.Trim();

                // 2. Limpiamos el número: quitamos espacios, guiones y el símbolo '+'
                string telefonoLimpio = telefonoCrudo.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

                // 3. Si el número empieza con '0' (ej. 0997535512), se lo quitamos y le anteponemos el código de país '593'
                if (telefonoLimpio.StartsWith("0") && telefonoLimpio.Length > 1)
                {
                    telefonoLimpio = "593" + telefonoLimpio.Substring(1);
                }
                // Si el usuario olvidó poner el 593 y el 0, pero puso el 9 directamente, aseguramos el 593
                else if (!telefonoLimpio.StartsWith("593") && telefonoLimpio.Length == 9)
                {
                    telefonoLimpio = "593" + telefonoLimpio;
                }

                // 4. Armamos el mensaje y la URL correcta para WhatsApp
                string mensaje = Uri.EscapeDataString($"Hola, nos comunicamos de la florería respecto a su Pedido #{pedido.id_pedido} con un total de ${pedido.total:F2}. ¿Desea coordinar algún detalle?");
                string urlWhatsApp = $"https://wa.me/{telefonoLimpio}?text={mensaje}";

                await Launcher.OpenAsync(new Uri(urlWhatsApp));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir WhatsApp: " + ex.Message, "Aceptar");
            }
        }
    }


    private async void RegistrarAbono_Clicked(object sender, EventArgs e)
    {
        if (!esAdministrador || sender is not Button boton || boton.CommandParameter is not PedidoModel pedido)
            return;

        if (!SesionAuth.TieneSesion)
        {
            await MensajeModal.MostrarAsync(this, "Sesión vencida", "Inicia sesión nuevamente.", "advertencia");
            return;
        }

        if (pedido.total <= 0)
        {
            await MensajeModal.MostrarAsync(this, "Pedido pagado", "Este pedido no tiene saldo pendiente.", "advertencia");
            return;
        }

        // 1. SELECCIONAR MÉTODO DE PAGO
        var ventanaMetodo = new vMetodoPago();
        await Navigation.PushModalAsync(ventanaMetodo);

        string? metodo = await ventanaMetodo.EsperarSeleccionAsync();
        if (string.IsNullOrWhiteSpace(metodo)) return;

        // 2. INGRESAR MONTO DEL ABONO
        var ventanaMonto = new vMontoAbono(pedido.id_pedido, pedido.total);
        await Navigation.PushModalAsync(ventanaMonto);

        decimal? montoSeleccionado = await ventanaMonto.EsperarMontoAsync();
        if (!montoSeleccionado.HasValue) return;

        decimal montoAbono = montoSeleccionado.Value;

        // 3. CONFIRMAR EL PAGO
        bool confirmar = await MensajeModal.ConfirmarAsync(
            this, "Confirmar abono",
            $"Pedido #{pedido.id_pedido}\nMonto: ${montoAbono:F2}\nMétodo: {metodo}\n\n¿Confirmas que recibiste este pago?");

        if (!confirmar) return;

        boton.IsEnabled = false;

        try
        {
            // 4. REGISTRAR ABONO EN SUPABASE
            Guid solicitudId = Guid.NewGuid();
            var pagosService = new PagosService();

            var resultado = await pagosService.RegistrarAbonoAsync(
                pedido.id_pedido, montoAbono, metodo, solicitudId);

            // 5. MOSTRAR RESULTADO
            string mensaje = $"Abono registrado: ${montoAbono:F2}\n" +
                             $"Saldo restante: ${resultado.SaldoRestante:F2}\n" +
                             $"Estado: {resultado.Estado}";

            if (resultado.EntregaCreada)
                mensaje += "\n\nEl pedido alcanzó el 50 % y pasó automáticamente a Control de entregas.";

            await MensajeModal.MostrarAsync(this, "Pago registrado", mensaje, "exito");

            // 6. ACTUALIZAR PEDIDOS
            await CargarPedidos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this, "No se pudo confirmar",
                "No se pudo verificar el registro del abono. Revisa el historial antes de repetirlo.",
                "advertencia");
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }



    private async Task<bool> ValidarAccesoAsync()
    {
        if (!SesionAuth.TieneSesion)
            return false;

        var servicio = new SupabaseService();

        string url =
            $"{servicio.GetUrl()}/rest/v1/usuarios" +
            "?select=id_usuario,id_rol,estado";

        using var respuesta = await servicio
            .GetClient()
            .GetAsync(url);

        respuesta.EnsureSuccessStatusCode();

        string json =
            await respuesta.Content.ReadAsStringAsync();

        var perfiles = JsonSerializer.Deserialize<List<PerfilPedido>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        // La política RLS debe permitir consultar
        // únicamente el perfil del usuario autenticado.
        if (perfiles == null || perfiles.Count != 1)
            return false;

        var perfil = perfiles[0];

        if (!perfil.estado)
            return false;

        esAdministrador = perfil.id_rol == 1;
        idClienteActual = perfil.id_usuario;

        // No permitir abrir la vista administrativa
        // con un perfil que no sea Administrador.
        if (modoAdministradorSolicitado)
            return esAdministrador;

        return perfil.id_rol == 3;
    }

    private class PerfilPedido
    {
        public int id_usuario { get; set; }
        public int id_rol { get; set; }
        public bool estado { get; set; }
    }
    private async void Volver_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void Actualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarPedidos();
    }

}