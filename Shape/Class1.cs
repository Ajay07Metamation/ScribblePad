using System.Collections.ObjectModel;

namespace Shapes;
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
   public virtual Point StartPoint { get; set; } // Get and set start point of line
   public virtual Point EndPoint { get; set; } // Get and set end point of line
   public virtual int Type { get; set; } // Indicate the type of shape
   public virtual (byte, byte, byte, byte) Brush { get => mBrush; set => mBrush = value; } // Brush colour 
   (byte, byte, byte, byte) mBrush = (255, 255, 255, 255);
}
#endregion

#region Class Scribble -------------------------------------------------------------------------
public class Scribbles : Shape {
   public Scribbles () { mPoints = new (); Type = 1; }
   public override Point StartPoint {
      set { base.StartPoint = value; AddPoints (base.StartPoint); }
   }
   public override Point EndPoint {
      set { base.EndPoint = value; AddPoints (base.EndPoint); }
   }
   void AddPoints (Point point) => mPoints.Add (point); // Add points to the list
   public override void Save (BinaryWriter bw) {
      bw.Write (mPoints.Count); // Total number of points in one scribble
      foreach (var point in mPoints) {
         bw.Write (point.X);
         bw.Write (point.Y);
      }
   }
   public override Shape Load (BinaryReader br) {
      Scribbles scribble = new () { Brush = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ()) };
      int pCount = br.ReadInt32 ();
      for (int j = 0; j < pCount; j++) {
         var (x, y) = (br.ReadDouble (), br.ReadDouble ());
         scribble.AddPoints (new (x, y));
      }
      return scribble;
   }

   public ObservableCollection<Point> mPoints;
}
#endregion

#region Class Line -----------------------------------------------------------------------------
public class Line : Shape {
   public Line () => Type = 2;
   public override Shape Load (BinaryReader br) {
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      Line line = new () { StartPoint = new (startX, startY), EndPoint = new (endX, endY), Brush = (a, r, g, b) };
      return line;
   }
}
#endregion

#region Class Rectangle ------------------------------------------------------------------------
public class Rectangle : Shape {
   public Rectangle () => Type = 3;
   public override Shape Load (BinaryReader br) { // Load the rectangle from binary file
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      Rectangle rectangle = new () { StartPoint = new (startX, startY), EndPoint = new (endX, endY), Brush = (a, r, g, b) };
      return rectangle;
   }
}
#endregion

#region Class Circle ---------------------------------------------------------------------------
public class Circle : Shape {
   public Circle () => Type = 4;
   public override Shape Load (BinaryReader br) {
      var (a, r, g, b) = (br.ReadByte (), br.ReadByte (), br.ReadByte (), br.ReadByte ());
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      Circle circle = new () { StartPoint = new (startX, startY), EndPoint = new (endX, endY), Brush = (a, r, g, b) };
      return circle;
   }
}
#endregion

#region Struct Point ---------------------------------------------------------------------------
public struct Point {
   public Point (double x, double y) { X = x; Y = y; }
   public double X { get; set; }
   public double Y { get; set; }
}
#endregion
