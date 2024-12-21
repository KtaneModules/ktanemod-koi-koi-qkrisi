using UnityEngine;

namespace KoiKoi
{
    public static class BezierCurves
    {
        public static Vector3 Quad(Vector3 start, Vector3 control, Vector3 end, float t) => (((1-t)*(1-t)) * start) + (2 * t * (1 - t) * control) + ((t * t) * end);

        public static Vector3 Cube(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end, float t) =>
            (((-start + 3 * (control1 - control2) + end) * t + (3 * (start + control2) - 6 * control1)) * t + 3 * (control1 - start)) * t + start;
    }
}