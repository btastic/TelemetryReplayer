using System.Windows;
using Autofac;
using Caliburn.Micro.Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TelemetryReplayer.ViewModels;

namespace TelemetryReplayer
{
    public class AppBootstrapper : AutofacBootstrapper<ShellViewModel>
    {
        public AppBootstrapper()
        {
            Initialize();
            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                return settings;
            });
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