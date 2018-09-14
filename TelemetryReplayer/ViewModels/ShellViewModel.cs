using System.ComponentModel.Composition;
using Caliburn.Micro;

namespace TelemetryReplayer.ViewModels
{
    [Export(typeof(IShell))]
    public class ShellViewModel : PropertyChangedBase, IShell
    {
    }
}