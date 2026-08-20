#region Namespaces

using PropertyChanged;
using KuroHitsuji.Lib;
using System.Windows;

#endregion Namespaces

namespace KuroHitsuji.ModelItems
{
    [AddINotifyPropertyChangedInterface]
    public class DiagnosticItem
    {
        public CheckResult SourceResult { get; set; }

        public string Group { get; set; }
        public string Header { get; set; }
        public string Description { get; set; }
        public string Evidence { get; set; }
        public string Recommendation { get; set; }

        public string IconText { get; set; }
        public string IconFontSize { get; set; }

        public string CardBackground { get; set; }
        public string CardBorderBrush { get; set; }
        public string IconBackground { get; set; }
        public string IconForeground { get; set; }
        public string DescriptionForeground { get; set; }

        public Visibility FixButtonVisibility { get; set; }
        public string FixButtonText { get; set; }

        public DiagnosticItem(CheckResult result)
        {
            SourceResult = result;

            Group = result?.Group ?? string.Empty;
            Header = result?.Title ?? string.Empty;
            Description = result?.Explanation ?? string.Empty;
            Evidence = result?.Evidence ?? string.Empty;
            Recommendation = result?.Recommendation ?? string.Empty;

            BuildStyle();
            BuildFixState();
        }

        private void BuildStyle()
        {
            switch (Group)
            {
                case "confirmed":
                    IconText = "!";
                    IconFontSize = "16";
                    CardBackground = "#FFFEF2F2";
                    CardBorderBrush = "#FFFECACA";
                    IconBackground = "#FFFEE2E2";
                    IconForeground = "#FFDC2626";
                    DescriptionForeground = "#FFB91C1C";
                    break;

                case "passed":
                    IconText = "✓";
                    IconFontSize = "16";
                    CardBackground = "#FFF0FDF4";
                    CardBorderBrush = "#FFBBF7D0";
                    IconBackground = "#FFDCFCE7";
                    IconForeground = "#FF16A34A";
                    DescriptionForeground = "#FF166534";
                    break;

                case "not_applicable":
                    IconText = "?";
                    IconFontSize = "16";
                    CardBackground = "#FFFFFBEB";
                    CardBorderBrush = "#FFFDE68A";
                    IconBackground = "#FFFEF3C7";
                    IconForeground = "#FFD97706";
                    DescriptionForeground = "#FF92400E";
                    break;

                default:
                    IconText = "•";
                    IconFontSize = "16";
                    CardBackground = "#FFFFFFFF";
                    CardBorderBrush = "#FFE5E7EB";
                    IconBackground = "#FFF3F4F6";
                    IconForeground = "#FF6B7280";
                    DescriptionForeground = "#FF374151";
                    break;
            }
        }

        private void BuildFixState()
        {
            if (Group == "confirmed")
            {
                FixButtonVisibility = Visibility.Visible;
                FixButtonText = "Fix";
            }
            else
            {
                FixButtonVisibility = Visibility.Collapsed;
                FixButtonText = string.Empty;
            }
        }
    }
}
