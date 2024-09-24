using Autodesk.Revit.Attributes;
using RevitAddIn1.View;
using Nice3point.Revit.Toolkit.External;

namespace RevitAddIn1.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        public override void Execute()
        {
            var mainWindow = new MainWindow(UiApplication);
            mainWindow.Show();
        }
    }
}