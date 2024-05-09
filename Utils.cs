using DesignLib;
using System;
using System.Windows.Media;

namespace DesignCraft;
static class Util {
   #region Implementation -----------------------------------------------------------------------
   public static Matrix ComputeZoomExtentsProjXfm (double viewWidth, double viewHeight, double viewMargin, Bound b) {
      // Compute the scaling, to fit specified drawing extents into the view space
      double scaleX = (viewWidth - 2 * viewMargin) / b.Width, scaleY = (viewHeight - 2 * viewMargin) / b.Height;
      double scale = Math.Min (scaleX, scaleY);
      var scaleMatrix = Matrix.Identity; scaleMatrix.Scale (scale, -scale);
      // translation...
      var midPoint = new System.Windows.Point (b.Mid.X, b.Mid.Y);
      var projectedMidPt = scaleMatrix.Transform (midPoint);
      Point viewMidPt = new (viewWidth / 2, viewHeight / 2);
      scaleMatrix.Translate (viewMidPt.X - projectedMidPt.X, viewMidPt.Y - projectedMidPt.Y);
      // Final zoom extents matrix, is a product of scale and translate matrices
      return scaleMatrix;
   }
   #endregion
}

public static class PointConverter {
   #region Implementation -----------------------------------------------------------------------
   public static Point ToCustomPoint (double x, double y) {
      return new Point (x, y);
   }
   public static Point ToCustomPoint (System.Windows.Point point) {
      return new Point (point.X, point.Y);
   }
   public static System.Windows.Point ToSystemPoint (double x, double y) {
      return new System.Windows.Point (x, y);
   }
   public static System.Windows.Point ToSystemPoint (Point point) {
      return new System.Windows.Point (point.X, point.Y);
   }
   #endregion
}
