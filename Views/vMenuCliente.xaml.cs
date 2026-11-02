namespace addFloreriaG5.Views;

public partial class vMenuCliente : ContentPage
{
	public vMenuCliente(string nombre)
	{
		InitializeComponent();
        lblUsuario.Text = "Bienvenido: " + nombre;
    }
}