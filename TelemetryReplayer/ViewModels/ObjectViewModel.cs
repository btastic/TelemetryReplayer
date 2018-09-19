using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TelemetryReplayer.ViewModels
{
    public class ObjectViewModel : INotifyPropertyChanged
    {
        private ReadOnlyCollection<ObjectViewModel> _children;
        private readonly ObjectViewModel _parent;
        private readonly object _object;
        private readonly FieldInfo _info;
        private readonly Type _type;
        private readonly int? _index;
        private bool _isExpanded;
        private bool _isSelected;

        public ObjectViewModel(object obj)
            : this(obj, null, null)
        {
        }

        private ObjectViewModel(object obj, FieldInfo info, ObjectViewModel parent, int? index = null)
        {
            _object = obj;
            _info = info;
            _index = index;

            if (_object != null)
            {
                _type = obj.GetType();
                if (!IsPrintableType(_type))
                {
                    // load the _children object with an empty collection to allow the + expander to be shown
                    _children = new ReadOnlyCollection<ObjectViewModel>(new ObjectViewModel[] { new ObjectViewModel(null) });
                }
            }
            _parent = parent;
        }

        public void LoadChildren()
        {
            if (_object != null)
            {
                // exclude value types and strings from listing child members
                if (!IsPrintableType(_type))
                {
                    // the public properties of this object are its children
                    var children = _type.GetFields()
                        .Select(p => new ObjectViewModel(p.GetValue(_object), p, this))
                        .ToList();

                    // if this is a collection type, add the contained items to the children
                    var collection = _object as IEnumerable;
                    if (collection != null)
                    {
                        var i = 0;
                        foreach (var item in collection)
                        {
                            children.Add(new ObjectViewModel(item, null, this, i));
                            i++;
                        }
                    }

                    _children = new ReadOnlyCollection<ObjectViewModel>(children);
                    this.OnPropertyChanged("Children");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating if the object graph can display this type without enumerating its children
        /// </summary>
        private static bool IsPrintableType(Type type)
        {
            return type != null && (
                type.IsPrimitive ||
                type.IsAssignableFrom(typeof(string)) ||
                type.IsEnum);
        }

        public int? Index
        {
            get { return _index; }
        }

        public ObjectViewModel Parent
        {
            get { return _parent; }
        }

        public FieldInfo Info
        {
            get { return _info; }
        }

        public ReadOnlyCollection<ObjectViewModel> Children
        {
            get { return _children; }
        }

        public string Type
        {
            get
            {
                var type = string.Empty;
                if (_object != null)
                {
                    if (_index != null)
                    {
                        type = string.Format("({0}[{1}])", _type.Name, _index);
                    }
                    else
                    {
                        type = string.Format("({0})", _type.Name);
                    }
                }
                else
                {
                    if (_info != null)
                    {
                        if (_index != null)
                        {
                            type = string.Format("({0}[{1}])", _info.FieldType.Name, _index);
                        }
                        else
                        {
                            type = string.Format("({0})", _info.FieldType.Name);
                        }
                    }
                }

                if (_info == null)
                {
                    return type;
                }

                if (_info.FieldType.IsArray)
                {
                    var array = _object as Array;
                    type = type.Replace("[]", $"[{array.Length}]");
                }

                return type;
            }
        }

        public string Name
        {
            get
            {
                var name = string.Empty;
                if (_info != null)
                {
                    name = _info.Name;
                }
                return name;
            }
        }

        public string Value
        {
            get
            {
                var value = string.Empty;
                if (_object != null)
                {
                    if (IsPrintableType(_type))
                    {
                        value = _object.ToString();
                    }
                }
                else
                {
                    value = "<null>";
                }
                return value;
            }
        }

        #region Presentation Members

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    if (_isExpanded)
                    {
                        LoadChildren();
                    }
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                {
                    _parent.IsExpanded = true;
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
