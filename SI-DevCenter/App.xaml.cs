using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SI_DevCenter.Business;
using SI_DevCenter.Helpers;
using SI_DevCenter.ViewModels;
using System.Text;
using System.Windows;

namespace SI_DevCenter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Ioc.Default.ConfigureServices(
                new ServiceCollection()

                //Services
                .AddSingleton<IAppRegistry>(new AppRegistry("teranum"))
                .AddSingleton<IAppLogic>(new AppLogic())
                .AddSingleton<MainViewModel>()

                .BuildServiceProvider()
                );

            Startup += (s, e) =>
            {
                var View = new MainView();
                View.DataContext = Ioc.Default.GetService<MainViewModel>();

                IAppRegistry appRegistry = Ioc.Default.GetRequiredService<IAppRegistry>();
                int Left = appRegistry.GetValue("InitData", "Left", 0);
                int Top = appRegistry.GetValue("InitData", "Top", 0);
                int Width = appRegistry.GetValue("InitData", "Width", 0);
                int Height = appRegistry.GetValue("InitData", "Height", 0);

                if (Left != 0) View.Left = Left;
                if (Top != 0) View.Top = Top;
                if (Width != 0) View.Width = Width;
                if (Height != 0) View.Height = Height;

                View.Closed += (s, e) =>
                {
                    appRegistry.SetValue("InitData", "Left", (int)View.Left);
                    appRegistry.SetValue("InitData", "Top", (int)View.Top);
                    appRegistry.SetValue("InitData", "Width", (int)View.Width);
                    appRegistry.SetValue("InitData", "Height", (int)View.Height);
                };
                View.Show();
            };
        }
    }
}