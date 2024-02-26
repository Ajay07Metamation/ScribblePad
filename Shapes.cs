namespace ScribblePad;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;


#region Shapes--------------------------------------------------------------------------------------
public abstract class Shapes { // Abstract class for shapes
   public int Type { // To indicate the type of shape
      get => mType;
      set => mType = value;
   }

   public Point StartPoint { // Get and set start point
      get => mStartPoint;
      set => mStartPoint = value;
   }

   public Point EndPoint { // Get and set end point
      get => mEndPoint;
      set => mEndPoint = value;
   }

   public Brush Brush { // Brush colour
      get => mBrush;
      set => mBrush = value;
   }
   public double Thickness { // Thickness 
      get => mThickness;
      set => mThickness = value;
   }
   public abstract void Draw (DrawingContext drawingContext); // To render the drawing
   public abstract void SaveText (TextWriter tw); // Save as text
   public abstract void SaveBinary (BinaryWriter bw); // Save as binary
   public abstract Shapes LoadText (StreamReader sr); // Load as text
   public abstract Shapes LoadBinary (BinaryReader br); // Load as binary

   Brush mBrush;
   double mThickness;
   int mType;
   Point mStartPoint;
   Point mEndPoint;
}
#endregion

#region Scribble--------------------------------------------------------------------------------------
class Scribble : Shapes {
   public Scribble (Pen pen) {
      mPoints = new List<Point> ();
      Brush = pen.Brush;
      Thickness = pen.Thickness;
      Type = 1;
   }
   public List<Point> Points { // List of points of scribble
      get => mPoints;
   }
   public void AddPoints (Point point) => mPoints.Add (point); // Add points to the list
   List<Point> mPoints;

   public override void Draw (DrawingContext drawingContext) { // Render the scribble
      for (int i = 1; i < Points.Count; i++)
         drawingContext.DrawLine (new Pen (Brush, Thickness), Points[i - 1], Points[i]);
   }

   public override void SaveText (TextWriter tw) { // Save the scribble as text
      tw.WriteLine (Brush.ToString ()); // Brush color
      tw.WriteLine (Thickness); // Brush thickness
      tw.WriteLine (Points.Count); // Total number of points in one scribble
      foreach (var point in Points) {
         tw.WriteLine (point.X); // X coordinate of point
         tw.WriteLine (point.Y); // Y coordinate of point
      }
   }

   public override void SaveBinary (BinaryWriter bw) { // Save the scribble as binary
      if (Brush is SolidColorBrush sb) { // Brush color
         bw.Write (sb.Color.A);
         bw.Write (sb.Color.R);
         bw.Write (sb.Color.G);
         bw.Write (sb.Color.B);
      }
      bw.Write (Thickness); // Brush thickness
      bw.Write (Points.Count); // Total number of points in one scribble
      foreach (var point in Points) {
         bw.Write (point.X); // X coordinate of point
         bw.Write (point.Y); // Y coordinate of point
      }
   }
   public override Shapes LoadText (StreamReader sr) { // Load the scribble from text file
      Brush = new SolidColorBrush ((Color)ColorConverter.ConvertFromString (sr.ReadLine ())); // Brush color
      double.TryParse (sr.ReadLine (), out double thickness); // Pen thickness
      Thickness = thickness;
      Scribble scribble = new (new Pen (Brush, Thickness));
      int.TryParse (sr.ReadLine (), out int pCount); // Total number of points in one scribble
      for (int j = 0; j < pCount; j++) {
         double.TryParse (sr.ReadLine (), out double x); // X co-ordinate of point
         double.TryParse (sr.ReadLine (), out double y); // Y co-ordinate of point
         Point point = new (x, y);
         scribble.AddPoints (point);
      }
      return scribble;
   }

   public override Shapes LoadBinary (BinaryReader br) { // Load the scribble from binary file
      byte a = br.ReadByte ();
      byte r = br.ReadByte ();
      byte g = br.ReadByte ();
      byte b = br.ReadByte ();
      SolidColorBrush brush = new () {
         Color = Color.FromArgb (a, r, g, b) // Brush color
      };
      Brush = brush;
      Thickness = br.ReadDouble (); // Pen thickness
      Scribble scribble = new (new Pen (Brush, Thickness));
      int pCount = br.ReadInt32 (); // Total number of points in one scribble
      for (int j = 0; j < pCount; j++) {
         (var x, var y) = (br.ReadDouble (), br.ReadDouble ()); // X & Y co-ordinate of point
         Point p = new (x, y);
         scribble.AddPoints (p);
      }
      return scribble;
   }
}
#endregion

#region Line--------------------------------------------------------------------------------------
class Line : Shapes {
   public Line (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness;
      Type = 2;
   }
   public override void Draw (DrawingContext drawingContext) { // Render the line
      drawingContext.DrawLine (new Pen (Brush, Thickness), StartPoint, EndPoint);
   }

   public override void SaveBinary (BinaryWriter bw) { // Save the line as binary
      if (Brush is SolidColorBrush sb) { // Brush color
         bw.Write (sb.Color.A);
         bw.Write (sb.Color.R);
         bw.Write (sb.Color.G);
         bw.Write (sb.Color.B);
      }
      bw.Write (Thickness); // Brush thickness
      bw.Write (StartPoint.X);// Start point of line
      bw.Write (StartPoint.Y);
      bw.Write (EndPoint.X);// End point of line
      bw.Write (EndPoint.Y);
   }

   public override void SaveText (TextWriter tw) { // Save the line as text
      tw.WriteLine (Brush.ToString ()); // Brush color
      tw.WriteLine (Thickness); // Brush thickness
      tw.WriteLine (StartPoint.X);// Start point of line
      tw.WriteLine (StartPoint.Y);
      tw.WriteLine (EndPoint.X);// End point of line
      tw.WriteLine (EndPoint.Y);
   }

   public override Shapes LoadText (StreamReader sr) { // Load the line from text file
      Brush = new SolidColorBrush ((Color)ColorConverter.ConvertFromString (sr.ReadLine ())); // Brush color
      double.TryParse (sr.ReadLine (), out double thickness); // Pen thickness
      Thickness = thickness;
      double.TryParse (sr.ReadLine (), out double startX);
      double.TryParse (sr.ReadLine (), out double startY);
      StartPoint = new Point (startX, startY); // Start point of line
      double.TryParse (sr.ReadLine (), out double endX);
      double.TryParse (sr.ReadLine (), out double endY);
      EndPoint = new Point (endX, endY); // End point of line
      Line line = new (new Pen (Brush, Thickness)) {
         StartPoint = StartPoint,
         EndPoint = EndPoint
      };
      return line;
   }

   public override Shapes LoadBinary (BinaryReader br) { // Load the line from binary file
      byte a = br.ReadByte ();
      byte r = br.ReadByte ();
      byte g = br.ReadByte ();
      byte b = br.ReadByte ();
      SolidColorBrush brush = new () {
         Color = Color.FromArgb (a, r, g, b) // Brush color
      };
      Brush = brush;
      Thickness = br.ReadDouble (); // Pen thickness
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      StartPoint = new Point (startX, startY); // Start point of line
      EndPoint = new Point (endX, endY); // End point of line
      Line line = new (new Pen (Brush, Thickness)) {
         StartPoint = StartPoint,
         EndPoint = EndPoint
      };
      return line;
   }
}
#endregion

#region Rectangle--------------------------------------------------------------------------------------
class Rectangle : Shapes {
   public Rectangle (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness;
      Type = 3;
   }
   public override void Draw (DrawingContext drawingContext) { // Render the rectangle
      Rect rect = new (StartPoint, EndPoint);
      drawingContext.DrawRectangle (null, new Pen (Brush, Thickness), rect);
   }
   public override void SaveText (TextWriter tw) { // Save the rectangle as text
      tw.WriteLine (Brush.ToString ()); // Brush color
      tw.WriteLine (Thickness); // Brush thickness
      tw.WriteLine (StartPoint.X);// Start point of rectangle
      tw.WriteLine (StartPoint.Y);
      tw.WriteLine (EndPoint.X);// End point of rectangle
      tw.WriteLine (EndPoint.Y);

   }

   public override void SaveBinary (BinaryWriter bw) { // Save the rectangle as binary
      if (Brush is SolidColorBrush sb) { // Brush color
         bw.Write (sb.Color.A);
         bw.Write (sb.Color.R);
         bw.Write (sb.Color.G);
         bw.Write (sb.Color.B);
      }
      bw.Write (Thickness); // Brush thickness
      bw.Write (StartPoint.X);// Start point of rectangle
      bw.Write (StartPoint.Y);
      bw.Write (EndPoint.X);// End point of rectangle
      bw.Write (EndPoint.Y);
   }

   public override Shapes LoadText (StreamReader sr) { // Load the rectangle from text file
      Brush = new SolidColorBrush ((Color)ColorConverter.ConvertFromString (sr.ReadLine ())); // Brush color
      double.TryParse (sr.ReadLine (), out double thickness); // Pen thickness
      Thickness = thickness;
      double.TryParse (sr.ReadLine (), out double startX);
      double.TryParse (sr.ReadLine (), out double startY);
      StartPoint = new Point (startX, startY);
      double.TryParse (sr.ReadLine (), out double endX);
      double.TryParse (sr.ReadLine (), out double endY);
      EndPoint = new Point (endX, endY);
      Rectangle rectangle = new (new Pen (Brush, Thickness)) {
         StartPoint = StartPoint,
         EndPoint = EndPoint,
      };
      return rectangle;
   }

   public override Shapes LoadBinary (BinaryReader br) { // Load the rectangle from binary file
      byte a = br.ReadByte ();
      byte r = br.ReadByte ();
      byte g = br.ReadByte ();
      byte b = br.ReadByte ();
      SolidColorBrush brush = new () {
         Color = Color.FromArgb (a, r, g, b) // Brush color
      };
      Brush = brush;
      Thickness = br.ReadDouble (); // Pen thickness
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      StartPoint = new Point (startX, startY);
      EndPoint = new Point (endX, endY);
      Rectangle rectangle = new (new Pen (Brush, Thickness)) {
         StartPoint = StartPoint,
         EndPoint = EndPoint
      };
      return rectangle;
   }
}
#endregion

#region Ellipse--------------------------------------------------------------------------------------
class Ellipse : Shapes {
   public Ellipse (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness;
      Type = 4;
   }
   public override void Draw (DrawingContext dc) { // Render the ellipse
      mRadiusX = Math.Abs (EndPoint.X - StartPoint.X) / 2;
      mRadiusY = Math.Abs (EndPoint.Y - StartPoint.Y) / 2;
      mCenter = new (StartPoint.X + mRadiusX, StartPoint.Y + mRadiusY);
      mEllipse = new EllipseGeometry (mCenter, mRadiusX, mRadiusY);
      dc.DrawGeometry (null, new Pen (Brush, Thickness), mEllipse);
   }
   public override void SaveText (TextWriter tw) { // Save the ellipse as text
      tw.WriteLine (Brush.ToString ()); // Brush color
      tw.WriteLine (Thickness); // Brush thickness
      tw.WriteLine (StartPoint.X);// Start point of rectangle
      tw.WriteLine (StartPoint.Y);
      tw.WriteLine (EndPoint.X);// End point of rectangle
      tw.WriteLine (EndPoint.Y);
   }

   public override void SaveBinary (BinaryWriter bw) { // Save the ellipse as binary
      if (Brush is SolidColorBrush sb) { // Brush color
         bw.Write (sb.Color.A);
         bw.Write (sb.Color.R);
         bw.Write (sb.Color.G);
         bw.Write (sb.Color.B);
      }
      bw.Write (Thickness); // Brush thickness
      bw.Write (StartPoint.X);// Start point of rectangle
      bw.Write (StartPoint.Y);
      bw.Write (EndPoint.X);// End point of rectangle
      bw.Write (EndPoint.Y);
   }

   public override Shapes LoadText (StreamReader sr) { // Load the ellipse from text file
      Brush = new SolidColorBrush ((Color)ColorConverter.ConvertFromString (sr.ReadLine ())); // Brush color
      double.TryParse (sr.ReadLine (), out double thickness); // Pen thickness
      Thickness = thickness;
      double.TryParse (sr.ReadLine (), out double startX);
      double.TryParse (sr.ReadLine (), out double startY);
      StartPoint = new Point (startX, startY);
      double.TryParse (sr.ReadLine (), out double endX);
      double.TryParse (sr.ReadLine (), out double endY);
      EndPoint = new Point (endX, endY);
      Ellipse ellipse = new (new Pen (Brush, Thickness)) {
         StartPoint = StartPoint,
         EndPoint = EndPoint,
      };
      return ellipse;
   }

   public override Shapes LoadBinary (BinaryReader br) { // Load the ellipse from binary file
      byte a = br.ReadByte ();
      byte r = br.ReadByte ();
      byte g = br.ReadByte ();
      byte b = br.ReadByte ();
      SolidColorBrush brush = new () {
         Color = Color.FromArgb (a, r, g, b) // Brush color
      };
      Brush = brush;
      Thickness = br.ReadDouble (); // Pen thickness
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      StartPoint = new Point (startX, startY);
      EndPoint = new Point (endX, endY);
      Ellipse ellipse = new (new Pen (Brush, Thickness)) {
         StartPoint = StartPoint,
         EndPoint = EndPoint
      };
      return ellipse;
   }

   EllipseGeometry mEllipse;
   double mRadiusX, mRadiusY;
   Point mCenter;
}
#endregion