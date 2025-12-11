#if false

using UnityEngine;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner.Utils
{
    public class GetChildSums
    {
        public static float GetSums(StepNode stepNode)
        {
            Log.Info("GetSums, " + stepNode.data.title + ",  number of children: " + stepNode.Children.Count);
            stepNode.data.sumOfChildNumbers = 0;
            for (int i = 0; i < stepNode.Children.Count; i++)
            {
                StepNode r = stepNode.Children[i];
                if (r.data.stepType == CriterionType.Number)
                    stepNode.data.sumOfChildNumbers += r.data.number;
                if (r.Children.Count > 0)
                    stepNode.data.sumOfChildNumbers += GetSums(r);
            }
            return stepNode.data.sumOfChildNumbers;
        }
    }
}

#endif