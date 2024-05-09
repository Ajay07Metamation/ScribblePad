namespace DesignLib;

public class Pline {
   #region Constructors ------------------------------------------------------------------------------------
   public Pline (IEnumerable<Point> pts) => (mPoints, Bound) = (pts.ToList (), new Bound (pts));
   #endregion

   #region Implementation ----------------------------------------------------------------------------------
   public static Pline CreateLine (Point startPt, Point endPt) {
      return new Pline (Enum (startPt, endPt));

      static IEnumerable<Point> Enum (Point a, Point b) {
         yield return a;
         yield return b;
      }
   }

   public static Pline CreateRectangle (Point corner1, Point corner2, Point corner3, Point corner4) {
      return new Pline (Enum (corner1, corner2, corner3, corner4, corner1));

      static IEnumerable<Point> Enum (Point a, Point b, Point c, Point d, Point e) {
         yield return a;
         yield return b;
         yield return c;
         yield return d;
         yield return e;
      }
   }
   #endregion

   #region Properties --------------------------------------------------------------------------------------
   public Bound Bound { get; }
   public IEnumerable<Point> GetPoints () => mPoints;
   #endregion

   #region Private Field -----------------------------------------------------------------------------------
   readonly List<Point> mPoints = new ();
   #endregion
}


public readonly struct Bound { // Bound in drawing space
   #region Constructors ------------------------------------------------------------------------------------
   public Bound (Point cornerA, Point cornerB) {
      MinX = Math.Min (cornerA.X, cornerB.X);
      MaxX = Math.Max (cornerA.X, cornerB.X);
      MinY = Math.Min (cornerA.Y, cornerB.Y);
      MaxY = Math.Max (cornerA.Y, cornerB.Y);
   }

   public Bound (IEnumerable<Point> pts) {
      MinX = pts.Min (p => p.X);
      MaxX = pts.Max (p => p.X);
      MinY = pts.Min (p => p.Y);
      MaxY = pts.Max (p => p.Y);
   }

   public Bound (IEnumerable<Bound> bounds) {
      MinX = bounds.Min (b => b.MinX);
      MaxX = bounds.Max (b => b.MaxX);
      MinY = bounds.Min (b => b.MinY);
      MaxY = bounds.Max (b => b.MaxY);
   }

   public Bound () {
      this = Empty;
   }

   public static readonly Bound Empty = new () { MinX = double.MaxValue, MinY = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue };
   #endregion

   #region Properties --------------------------------------------------------------------------------------
   public double MinX { get; init; }
   public double MaxX { get; init; }
   public double MinY { get; init; }
   public double MaxY { get; init; }
   public double Width => MaxX - MinX;
   public double Height => MaxY - MinY;
   public Point Mid => new ((MaxX + MinX) / 2, (MaxY + MinY) / 2);
   public bool IsEmpty => MinX > MaxX || MinY > MaxY;
   #endregion

   #region Implementation ----------------------------------------------------------------------------------
   public Bound Inflated (Point ptAt, double factor) {
      if (IsEmpty) return this;
      var minX = ptAt.X - (ptAt.X - MinX) * factor;
      var maxX = ptAt.X + (MaxX - ptAt.X) * factor;
      var minY = ptAt.Y - (ptAt.Y - MinY) * factor;
      var maxY = ptAt.Y + (MaxY - ptAt.Y) * factor;
      return new () { MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY };
   }
   #endregion
}
public struct Point {

   #region Constructor -------------------------------------------------------------------------------------
   public Point Default () { return default; }
   public Point (double x, double y) { X = x; Y = y; }
   #endregion

   #region Properties --------------------------------------------------------------------------------------
   public double X { get; set; }
   public double Y { get; set; }
   #endregion
}

