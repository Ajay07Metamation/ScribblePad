namespace DesignLib;

#region Interface IDrawable---------------------------------------------------------------------
public interface IDrawable {
void DrawLine (Point start, Point end, (byte, byte, byte, byte) brush);
   void DrawRectangle (Point start, Point end, (byte, byte, byte, byte) brush);
   void DrawCircle (Point centre, Point end, (byte, byte, byte, byte) brush);
   void DrawCLine (List<Point> pointList, Point start, Point end, (byte, byte, byte, byte) brush);
}
#endregion

#region Struct Point ---------------------------------------------------------------------------
public struct Point {
   public Point Default () { return default; }
   public Point (double x, double y) { X = x; Y = y; }
   public double X { get; set; }
   public double Y { get; set; }
}
#endregion

#region Class Shape ---------------------------------------------------------------------------- 
/// <summary>Abstract base class for all shapes</summary>
public abstract class Shape {

   /// <summary>Save the shape</summary>
   /// <param name="bw"></param>
   public virtual void Save (BinaryWriter bw) {
      bw.Write (StartPoint.X);
      bw.Write (StartPoint.Y);
      bw.Write (EndPoint.X);
      bw.Write (EndPoint.Y);
   }
   /// <summary>Load the shape</summary>
   /// <param name="br"></param>
   /// <returns>Returns the loaded shape</returns>
   public abstract Shape Load (BinaryReader br);
   public abstract void Draw (IDrawable draw); // Draw the shape
   public virtual Point StartPoint { get; set; } // Get and set start point of line
   public virtual Point EndPoint { get; set; } // Get and set end point of line
   public virtual int Type { get; set; } // Indicate the type of shape
   public void AddPoint (Point point) => Points.Add (point);
   protected void UpdatePoint (Point point) => EndPoint = point;
   public List<Point> Points = new ();
   public virtual (byte, byte, byte, byte) Brush { get => mBrush; set => mBrush = value; } // Brush colour 
   (byte, byte, byte, byte) mBrush = (255, 0, 0, 0);
}
#endregion


#region Class Connected Line -------------------------------------------------------------------
public class CLine : Shape {
   public CLine () => Type = 1;
   public override void Draw (IDrawable draw) => draw.DrawCLine (Points, StartPoint, EndPoint, Brush);

   public override void Save (BinaryWriter bw) {
      bw.Write (Points.Count);
      foreach (var point in Points) {
         bw.Write (point.X);
         bw.Write (point.Y);
      }
   }
   public override Shape Load (BinaryReader br) {
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var pCount = br.ReadInt32 ();
      for (int i = 0; i < pCount; i++) {
         var (pointX, pointY) = (br.ReadDouble (), br.ReadDouble ());
         Points.Add (new Point (pointX, pointY));
      }
      return this;
   }
}


#endregion

#region Class Line -----------------------------------------------------------------------------
public class Line : Shape {
   public Line () => Type = 2;
   public override void Draw (IDrawable draw) => draw.DrawLine (StartPoint, EndPoint, Brush);
   public override Shape Load (BinaryReader br) {
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      StartPoint = new Point (startX, startY);
      EndPoint = new (endX, endY);
      Brush = (a, r, g, b);
      return this;
   }
}
#endregion

#region Class Rectangle ------------------------------------------------------------------------
public class Rectangle : Shape {
   public Rectangle () => Type = 3;

   public override void Draw (IDrawable draw) => draw.DrawRectangle (StartPoint, EndPoint, Brush);

   public override Shape Load (BinaryReader br) { // Load the rectangle from binary file
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      StartPoint = new Point (startX, startY);
      EndPoint = new (endX, endY);
      Brush = (a, r, g, b);
      return this;
   }
}
#endregion

#region Class Circle ---------------------------------------------------------------------------
public class Circle : Shape {
   public Circle () => Type = 4;

   public override void Draw (IDrawable draw) => draw.DrawCircle (StartPoint, EndPoint, Brush);

   public override Shape Load (BinaryReader br) {
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      StartPoint = new Point (startX, startY);
      EndPoint = new (endX, endY);
      Brush = (a, r, g, b);
      return this;
   }
}
#endregion