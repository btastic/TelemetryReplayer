using System.Windows;
using Autofac;
using Caliburn.Micro.Autofac;
using TelemetryReplayer.ViewModels;

namespace TelemetryReplayer
{
    public class AppBootstrapper : AutofacBootstrapper<ShellViewModel>
    {
        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }
    }
}