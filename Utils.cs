using DesignCraft.Lib;
using System;
using System.Windows.Media;

namespace DesignCraft;
static class Util {
   #region Implementation -----------------------------------------------------------------------
   public static Matrix ComputeZoomExtentsProjXfm (double viewWidth, double viewHeight, double viewMargin, Bound b) {
      double scaleX = (viewWidth - 2 * viewMargin) / b.Width, scaleY = (viewHeight - 2 * viewMargin) / b.Height;
      double scale = Math.Min (scaleX, scaleY);
      var scaleMatrix = Matrix.Identity; scaleMatrix.Scale (scale, -scale);
      var midPoint = new System.Windows.Point (b.Mid.X, b.Mid.Y);
      var projectedMidPt = scaleMatrix.Transform (midPoint);
      Point viewMidPt = new (viewWidth / 2, viewHeight / 2);
      scaleMatrix.Translate (viewMidPt.X - projectedMidPt.X, viewMidPt.Y - projectedMidPt.Y);
      return scaleMatrix;
   }
   #endregion
}

public static class PointOperation {
   #region Implementation -----------------------------------------------------------------------
   public static Point ToCustomPoint (double x, double y) => new Point (x, y);
   public static Point ToCustomPoint (System.Windows.Point point) => new Point (point.X, point.Y);
   public static System.Windows.Point ToSystemPoint (double x, double y) => new System.Windows.Point (x, y);
   public static System.Windows.Point ToSystemPoint (Point point) => new System.Windows.Point (point.X, point.Y);
   public static double Distance (Point a, Point b) => Math.Sqrt (Math.Pow (b.X - a.X, 2) + Math.Pow (b.Y - a.Y, 2));
   public static double Angle (Point a, Point b) => Math.Atan2 (b.Y - a.Y, b.X - a.X) * (180 / Math.PI);
   #endregion
}