namespace addFloreriaG5.Views;

using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text.Json;
using System.Text;


public partial class vVerPedidos : ContentPage
{
	public vVerPedidos()
	{
		InitializeComponent();
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPedidos();
    }

    private async Task CargarPedidos()
    {
        try
        {
            SupabaseService servicio = new SupabaseService();

            // Pedimos los pedidos y unimos la tabla pagos con 'pagos(*)'
            string endpoint = $"{servicio.GetUrl()}/rest/v1/pedidos?select=*,pagos(*)&order=id_pedido.desc";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);
            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();
                var listaPedidos = JsonSerializer.Deserialize<List<PedidoModel>>(contenido, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                cvPedidos.ItemsSource = listaPedidos;
            }
            else
            {
                string error = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error", "No se pudo cargar la lista: " + error, "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Excepción al cargar pedidos: " + ex.Message, "Aceptar");
        }
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

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PedidoModel pedido)
        {
            // Ventana para que el admin ingrese cuánto abonó el cliente
            string resultadoMonto = await DisplayPromptAsync(
                "Registrar Abono",
                $"Pedido #{pedido.id_pedido}\nSaldo actual: ${pedido.total:F2}\n\nIngrese el monto del abono recibido:",
                "Guardar",
                "Cancelar",
                placeholder: "Ej. 10.00",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(resultadoMonto))
                return;

            if (!decimal.TryParse(resultadoMonto, out decimal montoAbono) || montoAbono <= 0)
            {
                await DisplayAlert("Error", "Ingrese un monto válido.", "Aceptar");
                return;
            }

            if (montoAbono > pedido.total)
            {
                await DisplayAlert("Aviso", "El abono no puede ser mayor al saldo restante del pedido.", "Aceptar");
                return;
            }

            try
            {
                SupabaseService servicio = new SupabaseService();
                var client = servicio.GetClient();

                // 1. Insertar el registro en la tabla 'pagos'
                var nuevoPago = new
                {
                    id_pedido = pedido.id_pedido,
                    monto = montoAbono,
                    metodo_pago = "Transferencia WhatsApp",
                    fecha_pago = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    estado = "Completado"
                };

                string jsonPago = JsonSerializer.Serialize(nuevoPago);
                StringContent contenidoPago = new StringContent(jsonPago, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                string endpointPago = $"{servicio.GetUrl()}/rest/v1/pagos";
                var respuestaPago = await client.PostAsync(endpointPago, contenidoPago);

                if (respuestaPago.IsSuccessStatusCode)
                {
                    // 2. Calcular el nuevo total restante
                    decimal nuevoTotal = pedido.total - montoAbono;

                    // 3. Evaluar reglas de estado automáticas (40% o Pagado Completo)
                    string nuevoEstado = "Pendiente";

                    if (nuevoTotal == 0)
                    {
                        nuevoEstado = "Pagado Completo";
                    }
                    else
                    {
                        // Sumamos todos los pagos anteriores + este nuevo abono para calcular el total abonado
                        decimal totalAbonado = 0;
                        if (pedido.pagos != null)
                        {
                            foreach (var p in pedido.pagos)
                            {
                                totalAbonado += p.monto;
                            }
                        }
                        totalAbonado += montoAbono;

                        // Estimamos el total original sumando el saldo actual y los abonos previos
                        decimal totalOriginalEstimado = pedido.total;
                        foreach (var p in pedido.pagos) { totalOriginalEstimado += p.monto; }

                        // Si el abonado cubre al menos el 40% del total estimado
                        if (totalAbonado >= (totalOriginalEstimado * 0.40m))
                        {
                            nuevoEstado = "Pago Minimo";
                        }
                        else
                        {
                            nuevoEstado = "Pago Parcial";
                        }
                    }

                    // 4. Actualizar el pedido en la tabla 'pedidos'
                    var actualizarPedido = new
                    {
                        total = nuevoTotal,
                        estado = nuevoEstado
                    };

                    string jsonPedido = JsonSerializer.Serialize(actualizarPedido);
                    StringContent contenidoPedido = new StringContent(jsonPedido, Encoding.UTF8, "application/json");

                    string endpointPedido = $"{servicio.GetUrl()}/rest/v1/pedidos?id_pedido=eq.{pedido.id_pedido}";
                    var requestPedido = new HttpRequestMessage(new HttpMethod("PATCH"), endpointPedido)
                    {
                        Content = contenidoPedido
                    };
                    requestPedido.Headers.Add("Prefer", "return=minimal");

                    await client.SendAsync(requestPedido);

                    // 5. NUEVO: Si el pedido ya cumple con Pago Minimo o Pagado Completo, 
                    // generamos automáticamente su registro en la tabla 'entregas' usando la dirección y teléfono reales del pedido

                    if (nuevoEstado == "Pago Minimo" || nuevoEstado == "Pagado Completo")
                    {
                        var nuevaEntrega = new
                        {
                            id_pedido = pedido.id_pedido,
                            direccion = string.IsNullOrEmpty(pedido.direccion_envio) ? "Sin dirección registrada" : pedido.direccion_envio,
                            destinatario = "Destinatario Pedido #" + pedido.id_pedido,
                            telefono_destinatario = string.IsNullOrEmpty(pedido.telefono_envio) ? "0999999999" : pedido.telefono_envio, // <-- ¡Leemos el teléfono real guardado en el pedido!
                            fecha_entrega = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"),
                            observaciones = $"Saldo pendiente a cobrar: ${nuevoTotal:F2}",
                            estado = "Pendiente"
                        };

                        string jsonEntrega = JsonSerializer.Serialize(nuevaEntrega);
                        StringContent contenidoEntrega = new StringContent(jsonEntrega, Encoding.UTF8, "application/json");

                        string endpointEntrega = $"{servicio.GetUrl()}/rest/v1/entregas";
                        await client.PostAsync(endpointEntrega, contenidoEntrega);
                    }


                    await DisplayAlert("¡Éxito!", $"Se registró el abono de ${montoAbono:F2}.\nSaldo restante: ${nuevoTotal:F2}\nEstado: {nuevoEstado}", "Aceptar");

                    await CargarPedidos();
                }
                else
                {
                    string error = await respuestaPago.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", "No se pudo registrar el pago: " + error, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }
    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PedidoModel pedido)
        {
            string accion = await DisplayActionSheet(
                $"Pedido #{pedido.id_pedido}",
                "Cancelar",
                null,
                "Cambiar a: Pago Minimo",
                "Cambiar a: Pagado Completo",
                "Cambiar a: Pendiente");

            if (string.IsNullOrEmpty(accion) || accion == "Cancelar")
                return;

            string nuevoEstado = "Pendiente";
            if (accion.Contains("Pago Minimo")) nuevoEstado = "Pago Minimo";
            else if (accion.Contains("Pagado Completo")) nuevoEstado = "Pagado Completo";

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

                string endpoint = $"{servicio.GetUrl()}/rest/v1/pedidos?id_pedido=eq.{pedido.id_pedido}";
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                {
                    Content = contenido
                };
                request.Headers.Add("Prefer", "return=minimal");

                var respuesta = await client.SendAsync(request);

                if (respuesta.IsSuccessStatusCode)
                {
                    // NUEVO: Si se cambió manualmente a Pago Minimo o Pagado Completo, 
                    // generamos automáticamente su registro en la tabla 'entregas' con la dirección y teléfono reales
                    if (nuevoEstado == "Pago Minimo" || nuevoEstado == "Pagado Completo")
                    {
                        var nuevaEntrega = new
                        {
                            id_pedido = pedido.id_pedido,
                            direccion = string.IsNullOrEmpty(pedido.direccion_envio) ? "Sin dirección especificada" : pedido.direccion_envio,
                            destinatario = "Destinatario Pedido #" + pedido.id_pedido,
                            telefono_destinatario = string.IsNullOrEmpty(pedido.telefono_envio) ? "0999999999" : pedido.telefono_envio, // <-- ¡Leemos el teléfono real guardado en el pedido!
                            fecha_entrega = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"),
                            observaciones = "Generado automáticamente por cambio manual de estado",
                            estado = "Pendiente"
                        };

                        string jsonEntrega = JsonSerializer.Serialize(nuevaEntrega);
                        StringContent contenidoEntrega = new StringContent(jsonEntrega, Encoding.UTF8, "application/json");

                        string endpointEntrega = $"{servicio.GetUrl()}/rest/v1/entregas";
                        await client.PostAsync(endpointEntrega, contenidoEntrega);
                    }

                    await DisplayAlert("Éxito", $"El estado del pedido cambió a: {nuevoEstado}", "Aceptar");
                    await CargarPedidos();
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
}