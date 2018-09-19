using System.Collections.ObjectModel;

namespace TelemetryReplayer.ViewModels
{
    public class ObjectViewModelHierarchy
    {
        private readonly ReadOnlyCollection<ObjectViewModel> _firstGeneration;
        private readonly ObjectViewModel _rootObject;

        public ObjectViewModelHierarchy(object rootObject)
        {
            _rootObject = new ObjectViewModel(rootObject);
            _firstGeneration = new ReadOnlyCollection<ObjectViewModel>(new ObjectViewModel[] { _rootObject });
        }

        public ReadOnlyCollection<ObjectViewModel> FirstGeneration
        {
            get { return _firstGeneration; }
        }
    }
}
