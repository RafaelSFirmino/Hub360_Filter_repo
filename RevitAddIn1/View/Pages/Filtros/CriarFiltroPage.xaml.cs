using Autodesk.Revit.UI;
using RevitAddIn1.ViewModel;
using RevitAddIn1.ViewModel.Filtros;
using System.Windows.Controls;

namespace RevitAddIn1.View.Pages.Filtros
{
    public partial class CriarFiltroPage : Page
    {
        public CriarFiltroPage(UIApplication uiapp)
        {
            InitializeComponent();
            DataContext = new CriarFiltroViewModel(uiapp);
        }
    }
}
