using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SI_DevCenter.Business;
using SI_DevCenter.Helpers;
using SI_DevCenter.ViewModels;
using SI_DevCenter.Views;
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
                new MainView()
                {
                    DataContext = Ioc.Default.GetService<MainViewModel>()
                }.Show();
            };
        }
    }
}