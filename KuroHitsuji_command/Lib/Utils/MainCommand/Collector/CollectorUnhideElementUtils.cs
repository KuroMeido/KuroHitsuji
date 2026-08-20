using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KuroHitsuji.Lib.Helper;
using KuroHitsuji.PresentationWPF;
using KuroHitsuji.PresentationWPF.Views.Collector;
using System.IO;
using System.Reflection;
using System.Windows;

namespace KuroHitsuji.Lib
{
    public class CollectorUnhideElementUtils
    {
        public static void Main(UIApplication uiApp, string toolName)
        {

            string dllFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            AssemblyLoader.LoadAllRibbonAssemblies(dllFolder);

            try
            {
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                ElementId selectedElementId = GetSelectedElement(uiDoc);
                if (selectedElementId == null)
                {
                    MessageBox.Show("Please select exactly one element.", "Why Not Here");
                    return;
                }

                Element selectedElement = doc.GetElement(selectedElementId);
                if (selectedElement == null)
                {
                    MessageBox.Show("Could not get selected element.", "Why Not Here");
                    return;
                }

                View targetView = GetSeclectedView(uiDoc);
                if (targetView == null)
                {
                    MessageBox.Show("Could not get target view.", "Why Not Here");
                    return;
                }

                CheckResult[] results = AnalyzeVisibility(doc, selectedElement, targetView).ToArray();

                var vm = new CollectorUnhideElementVM(
                    uiApp,
                    toolName,
                    selectedElement,
                    targetView,
                    results);

                var window = new CollectorUnhideElementView(vm);
                vm.MainWindow = window;

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Execute");
            }

        }


        #region Check Methods
        private static ElementId GetSelectedElement(UIDocument uiDoc)
        {
            var selectedIds = uiDoc.Selection.GetElementIds();
            if (selectedIds.Count == 0)
            {
                return null;
            }
            else if (selectedIds.Count > 1)
            {
                return null;
            }
            else
            {
                return selectedIds.First();
            }
        }

        private static View GetSeclectedView(UIDocument uiDoc)
        {
            var selectedView = uiDoc.ActiveView;
            if (selectedView == null)
            {
                return null;
            }
            else
            {
                return selectedView;
            }
        }

        private static List<CheckResult> CheckElementHidden(Element element, View targetView)
        {
            var results = new List<CheckResult>();
            try
            {
                if (element.IsHidden(targetView))
                {
                    results.Add(new CheckResult(
                        "confirmed",
                        "Element hidden in view",
                        $"This element is hidden individually in view.\n Element Id: {element.Id}",
                        null,
                        "Unhide this element in the target view.",
                        "unhide_element_in_view"
                    ));
                }
                else
                {
                    results.Add(new CheckResult(
                        "passed",
                        "Element not hidden in view",
                        $"This element is not hidden individually in view.\n Element Id: {element.Id}",
                        null,
                        "No action needed."
                    ));
                }
            }
            catch (Exception ex)
            {
                results.Add(new CheckResult(
                    "error",
                    "Error checking if element is hidden",
                    $"Error checking if element is hidden: {ex.Message}",
                    null,
                    "Investigate the error."
                ));
            }
            return results;
        }


        private static List<CheckResult> CheckCategoryHidden(Element element, View targetView)
        {
            var results = new List<CheckResult>();
            Category category = element.Category;

            try
            {
                if (targetView.GetCategoryHidden(category.Id))
                {
                    results.Add(new CheckResult(
                        "confirmed",
                        "Category hidden in view",
                        $"This element's category is hidden in view.\n Element Id: {element.Id}\n Category: {category.Name}",
                        null,
                        "If the category is hidden, unhide it in the view.",
                        "unhide_category_in_view"
                    ));
                }
                else
                {
                    results.Add(new CheckResult(
                        "passed",
                        "Category not hidden in view",
                        $"This element's category is not hidden in view.\n Element Id: {element.Id}\n Category: {category.Name}",
                        null,
                        "No action needed."
                    ));
                }
            }
            catch (Exception ex)
            {
                results.Add(new CheckResult(
                    "not_applicable",
                    "Error checking if category is hidden",
                    $"Error checking if category is hidden: {ex.Message}",
                    null,
                    "Investigate the error."
                ));
            }
            return results;
        }

        private static bool FilterAppliesToElement(
            Document revitDoc,
            FilterElement filterElement,
            Element element)
        {
            if (filterElement is SelectionFilterElement selectionFilter)
            {
                try
                {
                    foreach (ElementId selectedId in selectionFilter.GetElementIds())
                    {
                        if (CollectorUnhideElementHelper.IdsAreEqual(selectedId, element.Id))
                            return true;
                    }
                }
                catch
                {
                }

                return false;
            }

            if (filterElement is ParameterFilterElement parameterFilter)
            {
                try
                {
                    ICollection<ElementId> categoryIds = parameterFilter.GetCategories();
                    bool categoryMatch = false;

                    foreach (ElementId categoryId in categoryIds)
                    {
                        if (element.Category != null && CollectorUnhideElementHelper.IdsAreEqual(categoryId, element.Category.Id))
                        {
                            categoryMatch = true;
                            break;
                        }
                    }

                    if (!categoryMatch)
                        return false;
                }
                catch
                {
                }

                try
                {
                    ElementFilter elementFilter = parameterFilter.GetElementFilter();
                    return elementFilter.PassesFilter(revitDoc, element.Id);
                }
                catch
                {
                    return true;
                }
            }

            return false;
        }

        private static List<CheckResult> CheckViewFilters(Document doc, Element element, View targetView)
        {
            var results = new List<CheckResult>();
            ICollection<ElementId> filterIds;

            try
            {
                filterIds = targetView.GetFilters();
            }
            catch (Exception ex)
            {
                return new List<CheckResult>
        {
            new CheckResult(
                "not_applicable",
                "Could not check view filters",
                "Revit could not list filters on the target view.",
                ex.Message,
                "Open Visibility/Graphics and review view filters manually."
            )
        };
            }

            var invisibleMatchingFilters = new List<FilterElement>();
            var visibleMatchingFilters = new List<FilterElement>();

            foreach (var filterId in filterIds)
            {
                var filterElement = doc.GetElement(filterId) as FilterElement;
                if (filterElement == null)
                    continue;

                if (FilterAppliesToElement(doc, filterElement, element))
                {
                    bool isVisible;
                    try
                    {
                        isVisible = targetView.GetFilterVisibility(filterId);
                    }
                    catch
                    {
                        isVisible = true;
                    }

                    if (isVisible)
                        visibleMatchingFilters.Add(filterElement);
                    else
                        invisibleMatchingFilters.Add(filterElement);
                }
            }

            foreach (var filterElement in invisibleMatchingFilters)
            {
                results.Add(new CheckResult(
                    "confirmed",
                    "View filter hides the element",
                    "A view filter applies to this element and is set to invisible.",
                    $"Filter: {filterElement.Name}",
                    "Show this filter in the target view or adjust its rules.",
                    "show_filter_in_view",
                    filterElement.Id
                ));
            }

            if (invisibleMatchingFilters.Any())
                return results;

            if (visibleMatchingFilters.Any())
            {
                var names = string.Join(", ", visibleMatchingFilters.Select(x => x.Name));
                return new List<CheckResult>
        {
            new CheckResult(
                "passed",
                "Matching view filters are visible",
                "Matching filters are not hiding this element.",
                names,
                "No correction needed."
            )
        };
            }

            return new List<CheckResult>
    {
        new CheckResult(
            "passed",
            "No hiding view filter found",
            "No invisible target-view filter was found for this element.",
            $"Checked {filterIds.Count} filter(s).",
            "No correction needed."
        )
    };
        }

        private static List<CheckResult> CheckWorkset(Document doc, Element element, View targetView)
        {
            var results = new List<CheckResult>();
            WorksetVisibility viewVisibility;
            Workset workset;

            if (!doc.IsWorkshared)
            {
                return results = new List<CheckResult>
                {
                    new CheckResult(
                        "not_applicable",
                        "Worksharing not enabled",
                        "The document is not workshared, so worksets are not applicable.",
                        null,
                        "No action needed."
                    )
                };
            }
            try
            {
                WorksetId worksetId = element.WorksetId;
                WorksetTable worksetTable = doc.GetWorksetTable();
                workset = worksetTable.GetWorkset(worksetId);

            }
            catch (Exception ex)
            {
                return results = new List<CheckResult>
                {
                    new CheckResult(
                        "error",
                        "Error checking workset",
                        $"Error checking the workset of the element: {ex.Message}",
                        null,
                        "Investigate the error."
                    )
                };
            }
            try
            {
                viewVisibility = targetView.GetWorksetVisibility(element.WorksetId);
                if (viewVisibility == WorksetVisibility.Hidden)
                {
                    return results = new List<CheckResult>
                    {
                        new CheckResult(
                            "confirmed",
                            "Workset hidden in view",
                            $"The workset of the element is hidden in the target view.",
                            $"Workset: {workset.Name}",
                            "Unhide the workset in the target view.",
                            "show_workset_in_view"
                        )
                    };
                }
            }
            catch (Exception ex)
            {
            }

            try
            {
                var defaultVisibility = WorksetDefaultVisibilitySettings.GetWorksetDefaultVisibilitySettings(doc);
                viewVisibility = targetView.GetWorksetVisibility(element.WorksetId);
                if (viewVisibility == WorksetVisibility.Hidden)
                {
                    return results = new List<CheckResult>
                    {
                        new CheckResult(
                            "confirmed",
                            "Workset default visibility is hidden",
                            $"The workset of the element has a default visibility of hidden.",
                            $"Workset: {workset.Name}",
                            "Change the default visibility of the workset to visible.",
                            "change_workset_default_visibility"
                        )
                    };
                }
            }
            catch
            {

            }

            try
            {
                if (!workset.IsOpen)
                {
                    return results = new List<CheckResult>
                    {
                        new CheckResult(
                            "confirmed",
                            "Workset is closed",
                            $"The element workset is closed in this session.",
                            $"Workset: {workset.Name}",
                            "Open the workset before expecting this element to display.",
                            "open_workset_in_session"
                        )
                    };
                }
            }
            catch { }

            return results = new List<CheckResult>
            {
                new CheckResult(
                    "passed",
                    "Workset is not hidden",
                    $"The workset of the element is visible in the target view.",
                    $"Workset: {workset.Name}",
                    "No action needed."
                )
            };
        }

        private static CheckResult CheckPhase(Document revitDoc, Element element, View targetView)
        {
            ElementId viewPhaseId = CollectorUnhideElementHelper.GetParameterElementId(targetView, BuiltInParameter.VIEW_PHASE);
            ElementId phaseFilterId = CollectorUnhideElementHelper.GetParameterElementId(targetView, BuiltInParameter.VIEW_PHASE_FILTER);
            ElementId createdPhaseId = CollectorUnhideElementHelper.GetParameterElementId(element, BuiltInParameter.PHASE_CREATED);
            ElementId demolishedPhaseId = CollectorUnhideElementHelper.GetParameterElementId(element, BuiltInParameter.PHASE_DEMOLISHED);

            if (!CollectorUnhideElementHelper.IdIsValid(viewPhaseId))
            {
                return new CheckResult(
                    "not_applicable",
                    "Target view phase not available",
                    "The target view phase could not be read.",
                    string.Empty,
                    "Open the view phase settings manually.");
            }

            Dictionary<long, long> phaseOrder = CollectorUnhideElementHelper.GetPhaseOrder(revitDoc);

            long viewOrder;
            long createdOrder;
            long demolishedOrder;

            bool hasViewOrder = phaseOrder.TryGetValue(CollectorUnhideElementHelper.GetElementIdValue(viewPhaseId), out viewOrder);
            bool hasCreatedOrder = phaseOrder.TryGetValue(CollectorUnhideElementHelper.GetElementIdValue(createdPhaseId), out createdOrder);
            bool hasDemolishedOrder = phaseOrder.TryGetValue(CollectorUnhideElementHelper.GetElementIdValue(demolishedPhaseId), out demolishedOrder);

            string viewPhaseName = CollectorUnhideElementHelper.GetName(revitDoc, viewPhaseId);
            string createdPhaseName = CollectorUnhideElementHelper.GetName(revitDoc, createdPhaseId);
            string demolishedPhaseName = CollectorUnhideElementHelper.GetName(revitDoc, demolishedPhaseId);
            string phaseFilterName = CollectorUnhideElementHelper.GetName(revitDoc, phaseFilterId);

            if (hasViewOrder && hasCreatedOrder && createdOrder > viewOrder)
            {
                return new CheckResult(
                    "confirmed",
                    "Element is created after the view phase",
                    "The selected element belongs to a later project phase than the view is showing.",
                    string.Format("Element was created in '{0}', but the target view is set to '{1}'.", createdPhaseName, viewPhaseName),
                    "Use a view set to the element's phase, or review the target view's Phase setting.",
                    "use_view_set_to_element_phase");
            }

            if (hasViewOrder && hasDemolishedOrder && demolishedOrder <= viewOrder)
            {
                return new CheckResult(
                    "not_applicable",
                    "Element may be demolished for this view phase",
                    "The element demolition phase may affect visibility in this target view.",
                    string.Format("Demolished: {0}; view phase: {1}; phase filter: {2}", demolishedPhaseName, viewPhaseName, phaseFilterName),
                    "Review the view phase and phase filter.");
            }

            if (CollectorUnhideElementHelper.IdIsValid(createdPhaseId) || CollectorUnhideElementHelper.IdIsValid(demolishedPhaseId))
            {
                return new CheckResult(
                    "passed",
                    "No obvious phase blocker",
                    "The element phase data does not show an obvious conflict with the target view phase.",
                    string.Format("View phase: {0}", viewPhaseName),
                    "No correction needed.");
            }

            return new CheckResult(
                "not_applicable",
                "Element phase data not available",
                "This element does not expose normal phase-created or phase-demolished parameters.",
                string.Empty,
                "Confirm phase behavior manually if the element is still missing.");
        }

        private static CheckResult CheckDesignOption(Document revitDoc, Element element, View targetView)
        {
            BuiltInParameter? designOptionParameter = CollectorUnhideElementHelper.GetBuiltInParameter("DESIGN_OPTION_ID");
            BuiltInParameter? viewOptionParameter = CollectorUnhideElementHelper.GetBuiltInParameter("VIEWER_OPTION_VISIBILITY");

            if (!designOptionParameter.HasValue)
            {
                return new CheckResult(
                    "not_applicable",
                    "Could not check design option",
                    "This Revit API did not expose the expected design option parameter.",
                    string.Empty,
                    "Review design option settings manually.");
            }

            ElementId elementOptionId = CollectorUnhideElementHelper.GetParameterElementId(element, designOptionParameter.Value);
            if (!CollectorUnhideElementHelper.IdIsValid(elementOptionId))
            {
                return new CheckResult(
                    "passed",
                    "Element is not in a design option",
                    "The element does not appear to belong to a design option.",
                    string.Empty,
                    "No correction needed.");
            }

            ElementId viewOptionId = ElementId.InvalidElementId;
            try
            {
                if (viewOptionParameter.HasValue)
                    viewOptionId = CollectorUnhideElementHelper.GetParameterElementId(targetView, viewOptionParameter.Value);
            }
            catch
            {
                viewOptionId = ElementId.InvalidElementId;
            }

            string elementOptionName = CollectorUnhideElementHelper.GetName(revitDoc, elementOptionId);

            if (CollectorUnhideElementHelper.IdIsValid(viewOptionId) && !CollectorUnhideElementHelper.IdsAreEqual(viewOptionId, elementOptionId))
            {
                return new CheckResult(
                    "confirmed",
                    "Different design option",
                    "The element belongs to a different design option than the target view.",
                    string.Format("Element option: {0}", elementOptionName),
                    "Change the target view design option or inspect an appropriate view.",
                    "change_view_design_option");
            }

            return new CheckResult(
                "not_applicable",
                "Design option may affect visibility",
                "The element belongs to a design option.",
                string.Format("Element option: {0}", elementOptionName),
                "Confirm the target view design option settings.");
        }

        //Check view range

        private static CheckResult CheckPlanViewRange(Document revitDoc, Element element, View targetView)
        {
            if (!CollectorUnhideElementHelper.IsPlanViewWithViewRange(targetView))
            {
                return new CheckResult(
                    "passed",
                    "View range does not apply",
                    "The target view is not a plan view with a normal view range.",
                    $"View type: {targetView.ViewType}",
                    "No correction needed."
                );
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
            {
                return new CheckResult(
                    "not_applicable",
                    "Could not check view range",
                    "Revit did not return a model bounding box for this element.",
                    string.Empty,
                    "Open View Range and compare it manually."
                );
            }

            Dictionary<string, object> topInfo;
            Dictionary<string, object> bottomInfo;
            Dictionary<string, object> depthInfo;
            double? topElevation;
            double? bottomElevation;
            double? depthElevation;

            try
            {
                PlanViewRange viewRange = ((ViewPlan)targetView).GetViewRange();
                topInfo = CollectorUnhideElementHelper.GetViewPlaneInfo(revitDoc, PlanViewPlane.TopClipPlane, viewRange);
                bottomInfo = CollectorUnhideElementHelper.GetViewPlaneInfo(revitDoc, PlanViewPlane.BottomClipPlane, viewRange);
                depthInfo = CollectorUnhideElementHelper.GetViewPlaneInfo(revitDoc, PlanViewPlane.ViewDepthPlane, viewRange);

                topElevation = topInfo != null ? Convert.ToDouble(topInfo["elevation"]) : (double?)null;
                bottomElevation = bottomInfo != null ? Convert.ToDouble(bottomInfo["elevation"]) : (double?)null;
                depthElevation = depthInfo != null ? Convert.ToDouble(depthInfo["elevation"]) : (double?)null;
            }
            catch (Exception ex)
            {
                return new CheckResult(
                    "not_applicable",
                    "Could not check view range",
                    "Revit could not read the target view range.",
                    ex.Message,
                    "Open View Range and compare it manually."
                );
            }

            var lowerCandidates = new List<double>();
            if (bottomElevation.HasValue)
                lowerCandidates.Add(bottomElevation.Value);
            if (depthElevation.HasValue)
                lowerCandidates.Add(depthElevation.Value);

            double? lowerElevation = lowerCandidates.Any() ? lowerCandidates.Min() : (double?)null;

            var evidenceParts = new List<string>
                            {
                                $"Element min: {CollectorUnhideElementHelper.FormatMm(elementBox.Min.Z)}",
                                $"Element max: {CollectorUnhideElementHelper.FormatMm(elementBox.Max.Z)}"
                            };

            double? locationMinZ = null;
            double? locationMaxZ = null;
            string locationLabel = null;

            try
            {
                LocationCurve locationCurve = element.Location as LocationCurve;
                if (locationCurve != null && locationCurve.Curve != null)
                {
                    XYZ start = locationCurve.Curve.GetEndPoint(0);
                    XYZ end = locationCurve.Curve.GetEndPoint(1);

                    locationMinZ = Math.Min(start.Z, end.Z);
                    locationMaxZ = Math.Max(start.Z, end.Z);
                    locationLabel = "Location line";

                    evidenceParts.Add($"{locationLabel} min: {CollectorUnhideElementHelper.FormatMm(locationMinZ.Value)}");
                    evidenceParts.Add($"{locationLabel} max: {CollectorUnhideElementHelper.FormatMm(locationMaxZ.Value)}");
                }
            }
            catch
            {
            }

            if (topElevation.HasValue)
                evidenceParts.Add($"View top: {CollectorUnhideElementHelper.FormatMm(topElevation.Value)}");
            if (lowerElevation.HasValue)
                evidenceParts.Add($"View lower/depth: {CollectorUnhideElementHelper.FormatMm(lowerElevation.Value)}");

            string evidence = string.Join("; ", evidenceParts);

            if (locationLabel != null && topElevation.HasValue && locationMinZ.HasValue && locationMinZ.Value > topElevation.Value)
            {
                return new CheckResult(
                    "confirmed",
                    "Outside view range",
                    $"The element's {locationLabel.ToLowerInvariant()} is above the target plan view's top plane.",
                    evidence,
                    "Adjust the target view range to include the element.",
                    "raise_top",
                    new Dictionary<string, object> { { "direction", "raise_top" } }
                );
            }

            if (locationLabel != null && lowerElevation.HasValue && locationMaxZ.HasValue && locationMaxZ.Value < lowerElevation.Value)
            {
                return new CheckResult(
                    "confirmed",
                    "Outside view range",
                    $"The element's {locationLabel.ToLowerInvariant()} is below the target plan view's lower view range.",
                    evidence,
                    "Adjust the target view range to include the element.",
                    "lower_range",
                    new Dictionary<string, object> { { "direction", "lower_range" } }
                );
            }

            if (topElevation.HasValue && elementBox.Min.Z > topElevation.Value)
            {
                return new CheckResult(
                    "confirmed",
                    "Outside view range",
                    "The element is above the target plan view's top plane.",
                    evidence,
                    "Adjust the target view range to include the element.",
                    "raise_top",
                    new Dictionary<string, object> { { "direction", "raise_top" } }
                );
            }

            if (lowerElevation.HasValue && elementBox.Max.Z < lowerElevation.Value)
            {
                return new CheckResult(
                    "confirmed",
                    "Outside view range",
                    "The element is below the target plan view's lower view range.",
                    evidence,
                    "Adjust the target view range to include the element.",
                    "raise_top",
                    new Dictionary<string, object> { { "direction", "lower_range" } }
                );
            }

            if (!topElevation.HasValue || !lowerElevation.HasValue)
            {
                return new CheckResult(
                    "not_applicable",
                    "View range may affect visibility",
                    "The target view range could only be partially read.",
                    evidence,
                    "Open View Range and confirm the top, bottom, and view depth settings."
                );
            }

            return new CheckResult(
                "passed",
                "Inside view range",
                "The element vertical extents intersect the target plan view range.",
                evidence,
                "No correction needed."
            );
        }

        //Check crop or section box
        private static CheckResult CheckCropOrSectionBox(Element element, View targetView)
        {
            BoundingBoxXYZ elementBox;
            try
            {
                elementBox = element.get_BoundingBox(targetView) ?? element.get_BoundingBox(null);
            }
            catch
            {
                elementBox = null;
            }

            if (elementBox == null)
            {
                return new CheckResult(
                    "not_applicable",
                    "Element bounding box not available",
                    "Revit did not return a model/view bounding box for this element.",
                    string.Empty,
                    "Try opening the target view and using Zoom to Selection."
                );
            }

            if (targetView is View3D view3D)
            {
                try
                {
                    if (view3D.IsSectionBoxActive)
                    {
                        BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
                        if (!CollectorUnhideElementHelper.BoxesIntersectInViewCoordinates(elementBox, sectionBox, true))
                        {
                            return new CheckResult(
                                "confirmed",
                                "Outside the 3D section box",
                                "The element bounding box is outside the target view section box.",
                                $"Target view: {targetView.Name}",
                                "Adjust the 3D section box.",
                                "adjust_section_box"
                            );
                        }

                        return new CheckResult(
                            "passed",
                            "Inside the 3D section box",
                            "The element bounding box intersects the target view section box.",
                            $"Target view: {targetView.Name}",
                            "No correction needed."
                        );
                    }
                }
                catch (Exception ex)
                {
                    return new CheckResult(
                        "not_applicable",
                        "Could not check 3D section box",
                        "Revit could not compare the element with the section box.",
                        ex.Message,
                        "Review the section box manually."
                    );
                }
            }

            try
            {
                if (targetView.CropBoxActive)
                {
                    BoundingBoxXYZ cropBox = targetView.CropBox;

                    // Important: crop-region check should be XY only for non-3D views
                    if (!CollectorUnhideElementHelper.BoxesIntersectInViewCoordinates(elementBox, cropBox, false))
                    {
                        return new CheckResult(
                            "confirmed",
                            "Outside the crop region",
                            "The element bounding box is outside the target view crop region.",
                            $"Target view: {targetView.Name}",
                            "Expand the target view crop boundary so it includes the selected element with a 300 mm margin. This may reveal more of the model in this view.",
                            "expand_crop_region"
                        );
                    }

                    return new CheckResult(
                        "passed",
                        "Inside the crop region",
                        "The element bounding box intersects the target view crop region.",
                        $"Target view: {targetView.Name}",
                        "No correction needed."
                    );
                }
            }
            catch (Exception ex)
            {
                return new CheckResult(
                    "not_applicable",
                    "Could not check crop region",
                    "Revit could not compare the element with the crop region.",
                    ex.Message,
                    "Review the crop region manually."
                );
            }

            return new CheckResult(
                "passed",
                "No active crop or section blocker found",
                "The target view has no active crop or section condition that obviously excludes the element.",
                string.Empty,
                "No correction needed."
            );
        }
        #endregion


        // Analyze visibility of the element in the target view
        private static List<CheckResult> AnalyzeVisibility(Document doc, Element element, View targetView)
        {
            var results = new List<CheckResult>();
            // Check if the element is hidden in the view
            List<CheckResult> hiddenCheckResult = CheckElementHidden(element, targetView);
            results.AddRange(hiddenCheckResult);

            // Check if the category is hidden in the view
            List<CheckResult> categoryHiddenCheckResult = CheckCategoryHidden(element, targetView);
            results.AddRange(categoryHiddenCheckResult);

            // Check view filters
            var filterResults = CheckViewFilters(doc, element, targetView);
            results.AddRange(filterResults);

            // Check workset visibility
            var worksetResults = CheckWorkset(doc, element, targetView);
            results.AddRange(worksetResults);

            // Check phase
            var phaseResult = CheckPhase(doc, element, targetView);
            results.Add(phaseResult);

            // Check design option
            var designOptionResult = CheckDesignOption(doc, element, targetView);
            results.Add(designOptionResult);

            //Check plan view range
            var viewRangeResult = CheckPlanViewRange(doc, element, targetView);
            results.Add(viewRangeResult);

            // Check crop or section box
            var cropOrSectionBoxResult = CheckCropOrSectionBox(element, targetView);
            results.Add(cropOrSectionBoxResult);


            return results;
        }

    }
}
