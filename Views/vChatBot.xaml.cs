using System.Collections.ObjectModel;
using addFloreriaG5.Models;
using addFloreriaG5.Services;

namespace addFloreriaG5.Views;

public partial class vChatBot : ContentPage
{
    private readonly bool esAdministrador;
    private readonly ObservableCollection<MensajeChat> mensajes = new();
    private readonly ChatBotService chatBotService = new();
    private bool enviando;

    public vChatBot(bool esAdministrador = false)
    {
        InitializeComponent();

        this.esAdministrador = esAdministrador;

        cvMensajes.ItemsSource = mensajes;

        ConfigurarPantalla();
        AgregarMensajeBienvenida();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (SesionAuth.TieneSesion)
            return;

        await MensajeModal.MostrarAsync(
            this,
            "Sesión vencida",
            "Vuelve a iniciar sesión para usar el ChatBot.",
            "advertencia");

        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }

    private void ConfigurarPantalla()
    {
        lblTituloChat.Text = "ChatBot";

        lblSubtituloChat.Text = esAdministrador
            ? "Consultas de administración"
            : "Asistente floral con IA";

        string[] sugerencias = esAdministrador
            ? new[]
            {
                "¿Cuánto vendí este mes?",
                "¿Qué pedidos están pendientes?",
                "¿Qué arreglos tienen poco stock?",
                "¿Qué productos están agotados?"
            }
            : new[]
            {
                "Tengo $30, ¿qué me recomiendas?",
                "Busco rosas y bombones",
                "¿Qué hay para un cumpleaños?",
                "¿Cómo va mi pedido?"
            };

        foreach (string texto in sugerencias)
        {
            var boton = new Button
            {
                Text = texto,
                CommandParameter = texto,
                BackgroundColor = Color.FromArgb("#F7E8ED"),
                TextColor = Color.FromArgb("#752B43"),
                BorderColor = Color.FromArgb("#EBD1DA"),
                BorderWidth = 1,
                CornerRadius = 16,
                FontSize = 12,
                Padding = new Thickness(12, 8),
                MinimumHeightRequest = 40
            };

            boton.Clicked += Sugerencia_Clicked;
            panelSugerencias.Children.Add(boton);
        }
    }

    private void AgregarMensajeBienvenida()
    {
        string texto = esAdministrador
            ? "¡Hola! 🌸 Soy Nova, tu asistente de administración. " +
              "Puedo consultar ventas, pedidos e inventario del sistema " +
              "de la florería. ¿Qué deseas saber hoy?"
            : "¡Hola! 🌸 Soy Nova, tu asistente floral. " +
              "¿En qué te puedo ayudar hoy? Puedo recomendarte arreglos " +
              "según tu presupuesto o lo que estés buscando.";

        AgregarMensaje(new MensajeChat
        {
            Texto = texto
        });
    }

    private async void Sugerencia_Clicked(
        object? sender,
        EventArgs e)
    {
        if (enviando || sender is not Button boton)
            return;

        await EnviarMensajeAsync(
            boton.CommandParameter?.ToString() ?? boton.Text);
    }

    private async void Enviar_Tapped(
        object? sender,
        TappedEventArgs e)
    {
        await EnviarMensajeAsync(txtMensaje.Text);
    }

    private async void Enviar_Completed(
        object? sender,
        EventArgs e)
    {
        await EnviarMensajeAsync(txtMensaje.Text);
    }

    private async Task EnviarMensajeAsync(string? texto)
    {
        if (enviando)
            return;

        string mensaje = texto?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        if (!SesionAuth.TieneSesion)
        {
            AgregarMensaje(new MensajeChat
            {
                Texto = "Tu sesión ha vencido. Cierra sesión y vuelve a ingresar.",
                EsError = true
            });

            return;
        }

        enviando = true;
        CambiarEstadoEnvio(true);
        scrollSugerencias.IsVisible = false;
        txtMensaje.Text = string.Empty;

        // El mensaje actual se envía aparte; el historial solo lleva los anteriores.
        var historial = mensajes
            .Where(m =>
                !m.EsCargando &&
                !m.EsError &&
                !string.IsNullOrWhiteSpace(m.Texto))
            .TakeLast(10)
            .ToList();

        AgregarMensaje(new MensajeChat
        {
            Texto = mensaje,
            EsUsuario = true
        });

        var cargando = new MensajeChat
        {
            Texto = "Escribiendo…",
            EsCargando = true
        };

        AgregarMensaje(cargando);

        try
        {
            string respuesta = await chatBotService.EnviarAsync(
                mensaje,
                historial);

            mensajes.Remove(cargando);

            AgregarMensaje(new MensajeChat
            {
                Texto = respuesta
            });
        }
        catch (ChatBotException ex)
        {
            mensajes.Remove(cargando);

            AgregarMensaje(new MensajeChat
            {
                Texto = ex.Message,
                EsError = true
            });

            scrollSugerencias.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            mensajes.Remove(cargando);

            AgregarMensaje(new MensajeChat
            {
                Texto = "No pude responder en este momento. " +
                        "Revisa tu conexión e inténtalo de nuevo.",
                EsError = true
            });

            scrollSugerencias.IsVisible = true;
        }
        finally
        {
            enviando = false;
            CambiarEstadoEnvio(false);
        }
    }

    private void AgregarMensaje(MensajeChat mensaje)
    {
        mensajes.Add(mensaje);
        DesplazarAlFinal();
    }

    private void DesplazarAlFinal()
    {
        if (mensajes.Count == 0)
            return;

        MensajeChat ultimo = mensajes[^1];

        Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(120),
            () =>
            {
                try
                {
                    cvMensajes.ScrollTo(
                        ultimo,
                        position: ScrollToPosition.End,
                        animate: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            });
    }

    private void CambiarEstadoEnvio(bool activo)
    {
        btnEnviar.IsEnabled = !activo;
        btnEnviar.Opacity = activo ? 0.55 : 1;
        txtMensaje.IsEnabled = !activo;
    }

    private async void Volver_Tapped(
        object? sender,
        TappedEventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }
}
