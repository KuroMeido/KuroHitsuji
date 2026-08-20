using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KuroHitsuji.Lib;
using System.IO;
using System.Reflection;
using System.Windows;

namespace KuroHitsuji.Command
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCollectorUnhideElement : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // General info
            string toolName = ConstantsAndMessages.BUTTON_COLLECTOR_UNHIDE_ELEMENT_NAME;

            UIApplication uiApp = commandData.Application;

            string dllFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            AssemblyLoader.LoadAllRibbonAssemblies(dllFolder);

            // Starting
            try
            {
                CollectorUnhideElementUtils.Main(uiApp, toolName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Execute");
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}
