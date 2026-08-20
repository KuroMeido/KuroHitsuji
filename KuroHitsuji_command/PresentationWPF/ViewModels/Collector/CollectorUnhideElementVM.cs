#region Namespaces

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.Input;
using PropertyChanged;
using KuroHitsuji.Lib;
using KuroHitsuji.ModelItems;
using KuroHitsuji.PresentationWPF.Views.Collector;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KuroHitsuji.Lib.Helper;

#endregion Namespaces

namespace KuroHitsuji.PresentationWPF
{
    [AddINotifyPropertyChangedInterface]
    public class CollectorUnhideElementVM
    {
        #region Properties

        public UIApplication UIApp { get; set; }
        public UIDocument UIDoc { get; set; }
        public Document Doc { get; set; }
        public string ToolName { get; set; }

        public CollectorUnhideElementView MainWindow { get; set; }

        public Element SelectedElement { get; set; }
        public View TargetView { get; set; }

        public string Title { get; set; } = "Unhide Element Results";
        public string Explanation { get; set; } = string.Empty;
        public string RecommendAction { get; set; } = string.Empty;

        public string ElementId { get; set; } = string.Empty;
        public string ElementCategory { get; set; } = string.Empty;
        public string TargetViewName { get; set; } = string.Empty;
        public string ElementPhase { get; set; } = string.Empty;
        public string SummaryCheck { get; set; } = string.Empty;

        public ObservableCollection<DiagnosticItem> DiagnosticItems { get; set; } = new ObservableCollection<DiagnosticItem>();

        #endregion

        #region Commands

        public ICommand CloseCmd { get; }
        public ICommand FixCmd { get; }

        #endregion

        #region Constructor

        public CollectorUnhideElementVM(
            UIApplication uiApp,
            string toolName,
            Element selectedElement,
            View targetView,
            CheckResult[] results)
        {
            UIApp = uiApp;
            UIDoc = uiApp.ActiveUIDocument;
            Doc = UIDoc.Document;
            ToolName = toolName;

            SelectedElement = selectedElement;
            TargetView = targetView;

            ElementId = selectedElement?.Id?.ToString() ?? string.Empty;
            ElementCategory = selectedElement?.Category?.Name ?? string.Empty;
            TargetViewName = targetView?.Name ?? string.Empty;
            ElementPhase = targetView?.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsValueString() ?? string.Empty;

            CloseCmd = new RelayCommand<Window>((p) => { CloseWindow(); });
            FixCmd = new RelayCommand<DiagnosticItem>((item) => { FixIssue(item); });

            LoadResults(results);
        }

        #endregion

        #region Methods

        public void LoadResults(CheckResult[] results)
        {
            DiagnosticItems.Clear();

            if (results == null || results.Length == 0)
            {
                Title = "No issues found";
                Explanation = "No diagnostic results available.";
                RecommendAction = string.Empty;
                SummaryCheck = "No checks available.";
                return;
            }

            foreach (CheckResult result in results)
            {
                DiagnosticItems.Add(new DiagnosticItem(result));
            }

            CheckResult primaryIssue = results.FirstOrDefault(x => x.Group == "confirmed");

            if (primaryIssue != null)
            {
                Title = primaryIssue.Title ?? "Issue found";
                Explanation = primaryIssue.Explanation ?? string.Empty;
                RecommendAction = primaryIssue.Recommendation ?? string.Empty;
            }
            else
            {
                Title = "No issues found";
                Explanation = "The selected element does not appear to have a confirmed visibility blocker in this view.";
                RecommendAction = "No action needed.";
            }

            int passed = results.Count(x => x.Group == "passed");
            int issues = results.Count(x => x.Group == "confirmed");
            int notApplicable = results.Count(x => x.Group == "not_applicable");

            SummaryCheck = $"{passed} checks passed, {issues} issue(s) found, {notApplicable} not applicable.";
        }

        public void FixIssue(DiagnosticItem item)
        {
            if (item == null || item.SourceResult == null)
                return;

            CheckResult result = item.SourceResult;

            try
            {
                using (Transaction trans = new Transaction(Doc, "Unhide Element"))
                {
                    trans.Start();

                    bool fixedOk = false;
                    string message = string.Empty;

                    switch (result.ActionName)
                    {
                        case "unhide_element":
                        case "unhide_element_in_view":
                            fixedOk = UnhideElementInView(Doc, SelectedElement, TargetView);
                            message = fixedOk ? "Element was unhidden in the target view." : "Could not unhide element.";
                            break;

                        case "unhide_category":
                        case "unhide_category_in_view":
                            fixedOk = UnhideCategoryInView(Doc, SelectedElement, TargetView);
                            message = fixedOk ? "Category was unhidden in the target view." : "Could not unhide category.";
                            break;

                        case "show_filter":
                        case "show_filter_in_view":
                            {
                                FilterElement filterElement = null;
                                ElementId filterId = null;

                                if (result.ActionData is ElementId id && CollectorUnhideElementHelper.IdIsValid(id))
                                {
                                    filterId = id;
                                }
#if R2026
                                else if (result.ActionData is int idInt &&
                                         idInt != Autodesk.Revit.DB.ElementId.InvalidElementId.Value)
                                {
                                    filterId = new ElementId(idInt);
                                }
                                else if (result.ActionData is string idText &&
         int.TryParse(idText, out int parsedId) &&
         parsedId != Autodesk.Revit.DB.ElementId.InvalidElementId.Value)
                                {
                                    filterId = new ElementId(parsedId);
                                }
#else
                                else if (result.ActionData is int idInt &&
                                         idInt != Autodesk.Revit.DB.ElementId.InvalidElementId.IntegerValue)
                                {
                                    filterId = new ElementId(idInt);
                                }
                                else if (result.ActionData is string idText &&
                                         int.TryParse(idText, out int parsedId) &&
                                         parsedId != Autodesk.Revit.DB.ElementId.InvalidElementId.IntegerValue)
                                {
                                    filterId = new ElementId(parsedId);
                                }
#endif



                                if (CollectorUnhideElementHelper.IdIsValid(filterId))
                                {
                                    filterElement = Doc.GetElement(filterId) as FilterElement;
                                }

                                fixedOk = UnhideFilterInView(Doc, filterElement, TargetView);
                                message = fixedOk ? "Filter was made visible in the target view." : "Could not show filter.";
                                break;
                            }


                        case "adjust_view_range":
                        case "raise_top":
                        case "lower_range":
                            message = AdjustViewRangeToElement(Doc, SelectedElement, TargetView, result);
                            fixedOk = true;
                            break;

                        default:
                            message = $"No fix handler for action: {result.ActionName}";
                            break;
                    }

                    if (fixedOk)
                        trans.Commit();
                    else
                        trans.RollBack();
                    CloseWindow();
                    MessageBox.Show(
                        message,
                        "Collector Unhide Element",
                        MessageBoxButton.OK,
                        fixedOk ? MessageBoxImage.Information : MessageBoxImage.Warning);

                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Fix Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                CloseWindow();
            }
        }

        public void CloseWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Close();
            }
        }

#endregion

        #region Fix action

        private bool UnhideElementInView(Document doc, Element element, View targetView)
        {
            List<ElementId> elementIds = CollectorUnhideElementHelper.GetElementAndDependenceIds(element);
            targetView.UnhideElements(elementIds);

            foreach (ElementId id in elementIds)
            {
                try
                {
                    targetView.SetElementOverrides(id, new OverrideGraphicSettings());
                }
                catch { }
            }
            doc.Regenerate();
            return true;
        }

        private bool UnhideCategoryInView(Document doc, Element element, View targetView)
        {
            Category category = element.Category;
            if (category == null)
            {
                return false;
            }

            View visibilityOwnerView = targetView;
            if (targetView.ViewTemplateId != Autodesk.Revit.DB.ElementId.InvalidElementId)
            {
                View templateView = doc.GetElement(targetView.ViewTemplateId) as View;
                if (templateView != null)
                {
                    visibilityOwnerView = templateView;
                }
            }

            if (!visibilityOwnerView.CanCategoryBeHidden(category.Id))
            {
                return false;
            }

            if (visibilityOwnerView.GetCategoryHidden(category.Id))
            {
                visibilityOwnerView.SetCategoryHidden(category.Id, false);
                doc.Regenerate();
            }

            return true;
        }

        private bool UnhideFilterInView(Document doc, FilterElement filterElement, View targetView)
        {
            if (filterElement == null || targetView == null)
            {
                return false;
            }
            ElementId filterId = filterElement.Id;
            if (!targetView.GetFilters().Contains(filterId))
            {
                return false;
            }
            try
            {
                targetView.SetFilterVisibility(filterId, true);
                doc.Regenerate();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string AdjustViewRangeToElement(Document revitDoc, Element element, View targetView, CheckResult result)
        {
            if (!(targetView is ViewPlan viewPlan))
                throw new Exception("Target view is not a plan view.");

            string direction = string.Empty;
            if (result != null && result.ActionData is Dictionary<string, object> actionData)
            {
                if (actionData.ContainsKey("direction") && actionData["direction"] != null)
                    direction = actionData["direction"].ToString();
            }

            BoundingBoxXYZ elementBox;
            try
            {
                elementBox = element.get_BoundingBox(null);
            }
            catch
            {
                elementBox = null;
            }

            if (elementBox == null)
                throw new Exception("Revit did not return a model bounding box for this element.");

            var undoData = new Dictionary<string, object>();
            string message;



            PlanViewRange viewRange = viewPlan.GetViewRange();
            Dictionary<string, object> planeInfo;
            double requiredElevation;

            if (direction == "raise_top")
            {
                planeInfo = CollectorUnhideElementHelper.GetViewPlaneInfo(revitDoc, PlanViewPlane.TopClipPlane, viewRange);
                if (planeInfo == null)
                    throw new Exception("Could not read the target view top plane.");

                requiredElevation = elementBox.Max.Z + CollectorUnhideElementHelper.VIEW_RANGE_CLEARANCE_FEET;
                double currentElevation = Convert.ToDouble(planeInfo["elevation"]);
                if (requiredElevation <= currentElevation)
                {
                    return "View top already includes the element.";
                }
            }
            else if (direction == "lower_range")
            {
                planeInfo = CollectorUnhideElementHelper.GetLowerViewRangePlaneInfo(revitDoc, viewRange);
                if (planeInfo == null)
                    throw new Exception("Could not read the target view lower or depth plane.");

                requiredElevation = elementBox.Min.Z - CollectorUnhideElementHelper.VIEW_RANGE_CLEARANCE_FEET;
                double currentElevation = Convert.ToDouble(planeInfo["elevation"]);
                if (requiredElevation >= currentElevation)
                {

                    return "View lower range already includes the element.";
                }
            }
            else
            {
                throw new Exception("Unknown view range adjustment direction.");
            }

            double previousOffset = Convert.ToDouble(planeInfo["offset"]);
            double levelElevation = Convert.ToDouble(planeInfo["levelElevation"]);
            double previousElevation = Convert.ToDouble(planeInfo["elevation"]);
            double newOffset = requiredElevation - levelElevation;

            viewRange.SetOffset((PlanViewPlane)planeInfo["plane"], newOffset);
            viewPlan.SetViewRange(viewRange);

            foreach (var item in CollectorUnhideElementHelper.MakeViewRangeActionData(direction, planeInfo, previousOffset, newOffset))
                undoData[item.Key] = item.Value;

            message = $"Changed view range from {CollectorUnhideElementHelper.FormatMm(previousElevation)} to {CollectorUnhideElementHelper.FormatMm(requiredElevation)}.";

            if (undoData.Count > 0 && result != null)
                result.ActionData = undoData;

            revitDoc.Regenerate();
            return message;
        }
        #endregion


    }
}
