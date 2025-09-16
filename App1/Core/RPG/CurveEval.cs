using System.Collections.Generic;

namespace Untolia.Core.RPG;

public static class CurveEval
{
    public static int EvalAtLevel(List<CurvePoint> points, int level)
    {
        if (points == null || points.Count == 0) return 0;
        points.Sort((a, b) => a.Lvl.CompareTo(b.Lvl));

        if (level <= points[0].Lvl) return points[0].Val;
        if (level >= points[^1].Lvl) return points[^1].Val;

        for (int i = 0; i < points.Count - 1; i++)
        {
            var a = points[i];
            var b = points[i + 1];
            if (level >= a.Lvl && level <= b.Lvl)
            {
                if (a.Lvl == b.Lvl) return a.Val;
                float t = (level - a.Lvl) / (float)(b.Lvl - a.Lvl);
                return (int)System.MathF.Round(a.Val + t * (b.Val - a.Val));
            }
        }
        return points[^1].Val;
    }
}