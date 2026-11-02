using Microsoft.Extensions.DependencyInjection;

namespace addFloreriaG5
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new Views.vLogin()));
        }
    }
}