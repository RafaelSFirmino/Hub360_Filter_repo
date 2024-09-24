using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace RevitAddIn1.ViewModel.Filtros
{
    public class CriarFiltroViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> _categories;
        private ObservableCollection<string> _parameters;
        private ObservableCollection<string> _values;
        private string _prefix;
        private readonly UIApplication _uiApp;
        private readonly CriarFiltroHandler _filtroHandler;
        private ExternalEvent _externalEvent;
        private string _selectedCategory;
        private string _selectedParameter;

        public CriarFiltroViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _categories = LoadCategories();
            _parameters = new ObservableCollection<string>();
            _values = new ObservableCollection<string>();

            _filtroHandler = new CriarFiltroHandler();
            _externalEvent = ExternalEvent.Create(_filtroHandler);
        }

        private ObservableCollection<string> LoadCategories()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var activeView = _uiApp.ActiveUIDocument.ActiveView;
           
            var collector = new FilteredElementCollector(doc, activeView.Id)
                            .WhereElementIsNotElementType()
                            .Select(e => e.Category)
                            .Where(c => c != null)
                            .Select(c => c.Name)
                            .Distinct()
                            .ToList();
     
            return new ObservableCollection<string>(collector);
        }

        private ObservableCollection<string> CategoryParameters(string categoryName)
        {
            var doc = _uiApp.ActiveUIDocument.Document;

            var collector = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .Where(e => e.Category != null && e.Category.Name == categoryName); 

            var parametersList = new HashSet<string>();

            foreach (var element in collector)
            {
                foreach (Parameter param in element.Parameters)
                {
                    if (param.Definition != null)
                    {
                        parametersList.Add(param.Definition.Name);
                    }
                }
            }

            return new ObservableCollection<string>(parametersList.ToList());
        }


        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged(nameof(SelectedCategory));

                    Parameters = CategoryParameters(_selectedCategory);
                    OnPropertyChanged(nameof(Parameters));
                }
            }
        }

        public string SelectedParameter
        {
            get => _selectedParameter;
            set
            {
                if (_selectedParameter != value)
                {
                    _selectedParameter = value;
                    OnPropertyChanged(nameof(SelectedParameter));

                    Values = LoadParameterValues(_selectedParameter);
                    OnPropertyChanged(nameof(Values));
                }
            }
        }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set
            {
                if (_categories != value)
                {
                    _categories = value;
                    OnPropertyChanged(nameof(Categories));
                }
            }
        }

        public ObservableCollection<string> Parameters
        {
            get => _parameters;
            set
            {
                if (_parameters != value)
                {
                    _parameters = value;
                    OnPropertyChanged(nameof(Parameters));
                }
            }
        }

        public ObservableCollection<string> Values
        {
            get => _values;
            set
            {
                if (_values != value)
                {
                    _values = value;
                    OnPropertyChanged(nameof(Values));
                }
            }
        }
        public string Prefix
        {
            get => _prefix;
            set
            {
                if (_prefix != value)
                {
                    _prefix = value;
                    OnPropertyChanged(nameof(Prefix));
                }
            }
        }
        private ObservableCollection<string> LoadParameterValues(string parameterName)
        {
            var doc = _uiApp.ActiveUIDocument.Document;

            var collector = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .Where(e => e.Category != null && e.Category.Name == _selectedCategory); 

            var valuesList = new List<string>();

            foreach (var element in collector)
            {
                foreach (Parameter param in element.Parameters)
                {
                    if (param.Definition != null && param.Definition.Name == parameterName)
                    {
                        string paramValue = null;

                        if (param.StorageType == StorageType.String)
                        {
                            paramValue = param.AsString();
                        }
                        else if (param.StorageType == StorageType.Double)
                        {
                            double valueInFeet = param.AsDouble();

                            ForgeTypeId specTypeId = param.Definition.GetDataType();

                            ForgeTypeId projectUnitType = GetProjectUnitType(doc, specTypeId);

                            paramValue = UnitUtils.ConvertFromInternalUnits(valueInFeet, projectUnitType).ToString();
                        }
                        else if (param.StorageType == StorageType.Integer)
                        {
                            paramValue = param.AsInteger().ToString();
                        }
                        else if (param.StorageType == StorageType.ElementId)
                        {
                            paramValue = param.AsElementId().Value.ToString();
                        }

                        if (!string.IsNullOrEmpty(paramValue) && !valuesList.Contains(paramValue))
                        {
                            valuesList.Add(paramValue);
                        }
                    }
                }
            }

            return new ObservableCollection<string>(valuesList.Distinct().ToList());
        }

        private ForgeTypeId GetProjectUnitType(Document doc, ForgeTypeId specTypeId)
        {
            FormatOptions formatOptions = doc.GetUnits().GetFormatOptions(specTypeId);
            return formatOptions.GetUnitTypeId(); 
        }

        public ICommand ApplyFilterCommand => new RelayCommand(ApplyFilter);

        private void ApplyFilter()
        {
            _filtroHandler.SelectedCategory = _selectedCategory;
            _filtroHandler.SelectedParameter = _selectedParameter;
            _filtroHandler.Values = _values.ToList();
            _filtroHandler.Prefix = _prefix;

            _externalEvent.Raise();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

            public void Execute(object parameter) => _execute();

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}