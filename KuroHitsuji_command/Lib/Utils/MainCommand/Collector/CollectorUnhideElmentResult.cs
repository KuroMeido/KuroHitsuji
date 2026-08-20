using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuroHitsuji.Lib
{
    public class CheckResult
    {
        // KuroHitsuji
        public string Group { get; set; }
        public string Title { get; set; }
        public string Explanation { get; set; }
        public string Evidence { get; set; }
        public string Recommendation { get; set; }
        public string ActionName { get; set; }
        public object ActionData { get; set; }
        public bool Fixed { get; set; }
        public string FixMessage { get; set; }


        // constructor
        public CheckResult(
            string group,
            string title,
            string explanation,
            string evidence = null,
            string recommendation = null,
            string actionName = null,
            object actionData = null)
        {
            Group = group;
            Title = title;
            Explanation = explanation;
            Evidence = evidence ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            ActionName = actionName;
            ActionData = actionData;
            Fixed = false;
            FixMessage = string.Empty;
        }
    }
}
