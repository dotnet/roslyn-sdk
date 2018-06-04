using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSBuildWorkspaceTester.Logging;
using MSBuildWorkspaceTester.Services;
using MSBuildWorkspaceTester.ViewModels;

namespace MSBuildWorkspaceTester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var msbuildService = serviceProvider.GetRequiredService<MSBuildService>();
            msbuildService.Initialize();

            var mainWindowViewModel = new MainWindowViewModel(serviceProvider);

            this.MainWindow = mainWindowViewModel.CreateView();
            this.MainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            var outputService = new OutputService();
            var loggerFactory = new LoggerFactory()
                .AddOutput(outputService);

            serviceCollection.AddSingleton(loggerFactory);
            serviceCollection.AddSingleton(outputService);
            serviceCollection.AddSingleton<MSBuildService>();
            serviceCollection.AddSingleton<WorkspaceService>();
            serviceCollection.AddSingleton<OpenDocumentService>();
        }
    }
}
