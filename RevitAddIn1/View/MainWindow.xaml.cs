using Autodesk.Revit.UI;
using System.Windows;
using RevitAddIn1.View;
using RevitAddIn1.View.Pages.Filtros;

namespace RevitAddIn1.View
{
    public partial class MainWindow : Window
    {
        private static MainWindow _instance;

        private UIApplication _uiApplication;

        public MainWindow(UIApplication uiApplication)
        {
            InitializeComponent();
            _uiApplication = uiApplication;
            LoadTabs();
        }

        public static MainWindow GetInstance(UIApplication uiApplication)
        {
            if (_instance == null)
            {
                _instance = new MainWindow(uiApplication);
                _instance.Closed += (sender, args) => _instance = null;
            }
            return _instance;
        }

        private void LoadTabs()
        {
            var doc = _uiApplication.ActiveUIDocument.Document;
            var activeView = doc.ActiveView;
            var createFilterPage = new CriarFiltroPage(_uiApplication);
            CriarFiltroFrame.Content = createFilterPage;
        }
    }
}
