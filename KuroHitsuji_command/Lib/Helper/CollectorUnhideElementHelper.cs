using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuroHitsuji.Lib.Helper
{
    public class CollectorUnhideElementHelper
    {

        #region Helper Methods
        public const double VIEW_RANGE_CLEARANCE_FEET = 1.0 / 304.8;
        private const double CropCheckToleranceFeet = 1.0 / 304.8; // 1 mm

        public static bool IdsAreEqual(ElementId id1, ElementId id2)
        {
            if (id1 == null && id2 == null)
                return true;
            if (id1 == null || id2 == null)
                return false;
            return id1 == id2;
        }

        public static long GetElementIdValue(ElementId id)
        {
#if R2026
            if (id == null)
                return ElementId.InvalidElementId.Value;
            return id.Value;

#else
            if (id == null)
                return ElementId.InvalidElementId.IntegerValue;
            return id.IntegerValue;
#endif


        }

        public static bool IdIsValid(ElementId id)
        {
#if R2026
            return id != null && id.Value != ElementId.InvalidElementId.Value;
#else
            return id != null && id.IntegerValue != ElementId.InvalidElementId.IntegerValue;
#endif

        }

        public static string GetName(Document revitDoc, ElementId id)
        {
            if (revitDoc == null || !IdIsValid(id))
                return string.Empty;

            try
            {
                Element element = revitDoc.GetElement(id);
                return element != null ? element.Name : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static ElementId GetParameterElementId(Element element, BuiltInParameter builtInParameter)
        {
            try
            {
                Parameter parameter = element.get_Parameter(builtInParameter);
                if (parameter != null && parameter.HasValue)
                    return parameter.AsElementId();
            }
            catch
            {
            }

            return ElementId.InvalidElementId;
        }

        public static BuiltInParameter? GetBuiltInParameter(string parameterName)
        {
            try
            {
                BuiltInParameter builtInParameter;
                if (System.Enum.TryParse(parameterName, out builtInParameter))
                    return builtInParameter;
            }
            catch
            {
            }

            return null;
        }

        public static Dictionary<long, long> GetPhaseOrder(Document revitDoc)
        {
            var result = new Dictionary<long, long>();

            try
            {
                int index = 0;
                foreach (Phase phase in revitDoc.Phases)
                {
                    result[GetElementIdValue(phase.Id)] = index;
                    index++;
                }
            }
            catch
            {
            }

            return result;
        }

        public static List<ElementId> GetElementAndDependenceIds(Element element)
        {
            List<ElementId> elementIds = new List<ElementId> { element.Id };

            List<ElementId> dependentIds = new List<ElementId>();

            foreach (ElementId dependentId in dependentIds)
            {
                if (IdIsValid(dependentId))
                {
                    elementIds.Add(dependentId);
                }
            }
            return elementIds;
        }
        public static string FormatMm(double feetValue)
        {
            double mmValue = UnitHelper.FeetToMilimeter(feetValue, 2);
            return $"{mmValue:0.##} mm";
        }

        public static List<XYZ> GetBoundingBoxCorners(BoundingBoxXYZ box)
        {
            return new List<XYZ>
    {
        new XYZ(box.Min.X, box.Min.Y, box.Min.Z),
        new XYZ(box.Min.X, box.Min.Y, box.Max.Z),
        new XYZ(box.Min.X, box.Max.Y, box.Min.Z),
        new XYZ(box.Min.X, box.Max.Y, box.Max.Z),
        new XYZ(box.Max.X, box.Min.Y, box.Min.Z),
        new XYZ(box.Max.X, box.Min.Y, box.Max.Z),
        new XYZ(box.Max.X, box.Max.Y, box.Min.Z),
        new XYZ(box.Max.X, box.Max.Y, box.Max.Z)
    };
        }

        public static bool BoxesIntersect(BoundingBoxXYZ modelBox, BoundingBoxXYZ viewBox)
        {
            if (modelBox == null || viewBox == null)
                return false;

            Transform inverse;
            try
            {
                inverse = viewBox.Transform != null ? viewBox.Transform.Inverse : null;
            }
            catch
            {
                inverse = null;
            }

            var xs = new List<double>();
            var ys = new List<double>();
            var zs = new List<double>();

            foreach (XYZ corner in GetBoundingBoxCorners(modelBox))
            {
                XYZ point = inverse != null ? inverse.OfPoint(corner) : corner;
                xs.Add(point.X);
                ys.Add(point.Y);
                zs.Add(point.Z);
            }

            XYZ modelMin = new XYZ(xs.Min(), ys.Min(), zs.Min());
            XYZ modelMax = new XYZ(xs.Max(), ys.Max(), zs.Max());

            if (modelMax.X < viewBox.Min.X || modelMin.X > viewBox.Max.X)
                return false;
            if (modelMax.Y < viewBox.Min.Y || modelMin.Y > viewBox.Max.Y)
                return false;
            if (modelMax.Z < viewBox.Min.Z || modelMin.Z > viewBox.Max.Z)
                return false;

            return true;
        }

        public static bool BoxesIntersectInViewCoordinates(BoundingBoxXYZ elementBox, BoundingBoxXYZ viewBox, bool includeZ)
        {
            if (elementBox == null || viewBox == null)
                return false;

            Transform inverse;
            try
            {
                inverse = viewBox.Transform != null ? viewBox.Transform.Inverse : null;
            }
            catch
            {
                inverse = null;
            }

            if (inverse == null)
                return true; // avoid false negative if transform is unavailable

            var corners = GetBoundingBoxCorners(elementBox);

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (XYZ corner in corners)
            {
                XYZ p = inverse.OfPoint(corner);

                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Z < minZ) minZ = p.Z;

                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                if (p.Z > maxZ) maxZ = p.Z;
            }

            if (maxX < viewBox.Min.X - CropCheckToleranceFeet || minX > viewBox.Max.X + CropCheckToleranceFeet)
                return false;
            if (maxY < viewBox.Min.Y - CropCheckToleranceFeet || minY > viewBox.Max.Y + CropCheckToleranceFeet)
                return false;

            if (includeZ)
            {
                if (maxZ < viewBox.Min.Z - CropCheckToleranceFeet || minZ > viewBox.Max.Z + CropCheckToleranceFeet)
                    return false;
            }

            return true;
        }
        public static bool IsPlanViewWithViewRange(View targetView)
        {
            if (!(targetView is ViewPlan))
            {
                return false;
            }
            try
            {
                return targetView.ViewType == ViewType.FloorPlan || targetView.ViewType == ViewType.CeilingPlan || targetView.ViewType == ViewType.AreaPlan;
            }
            catch
            {
                return false;
            }

        }
        public static Dictionary<string, object> GetViewPlaneInfo(Document doc, PlanViewPlane plane, PlanViewRange planViewRange)
        {
            var viewPlaneInfo = new Dictionary<string, object>();
            ElementId levelId = planViewRange.GetLevelId(plane);
            double offset = planViewRange.GetOffset(plane);
            Level level = doc.GetElement(levelId) as Level;
            if (level == null)
            {
                return null;
            }
            try
            {
                return new Dictionary<string, object>
                {
                    {"plane", plane },
                    {"levelId", levelId },
                    {"levelElevation", level.Elevation },
                    {"offset", offset },
                    {"elevation", level.Elevation + offset}
                };
            }
            catch
            {
                return null;
            }
        }

        public static Dictionary<string, object> GetLowerViewRangePlaneInfo(Document revitDoc, PlanViewRange planViewRange)
        {
            Dictionary<string, object> bottom = GetViewPlaneInfo(revitDoc, PlanViewPlane.BottomClipPlane, planViewRange);
            Dictionary<string, object> depth = GetViewPlaneInfo(revitDoc, PlanViewPlane.ViewDepthPlane, planViewRange);

            if (bottom == null)
                return depth;

            if (depth == null)
                return bottom;

            double depthElevation = Convert.ToDouble(depth["elevation"]);
            double bottomElevation = Convert.ToDouble(bottom["elevation"]);

            if (depthElevation <= bottomElevation)
                return depth;

            return bottom;
        }
        public static Dictionary<string, object> MakeViewRangeActionData(
            string direction,
            Dictionary<string, object> planeInfo,
            double previousOffset,
            double newOffset)
        {
            double levelElevation = Convert.ToDouble(planeInfo["levelElevation"]);
            double previousElevation = Convert.ToDouble(planeInfo["elevation"]);

            return new Dictionary<string, object>
    {
        { "direction", direction },
        { "plane", planeInfo["plane"] },
        { "previous_offset", previousOffset },
        { "new_offset", newOffset },
        { "previous_elevation", previousElevation },
        { "new_elevation", levelElevation + newOffset }
    };
        }




#endregion

    }
}
