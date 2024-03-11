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
public abstract class Shape { // Abstract class for shapes
   public int Type { get; set; } // To indicate the type of object
   public bool IsSelected { get; set; }// To check if any shape is selected 
   public Pen Pen { get; set; }
   public virtual Point StartPoint { get; set; }// Get and set start point of line
   public virtual Point EndPoint { get; set; }// Get and set end point of line
   public abstract void Draw (DrawingContext drawingContext); // To render the drawing
   public virtual void SaveBinary (BinaryWriter bw) {
      bw.Write (StartPoint.X);
      bw.Write (StartPoint.Y);
      bw.Write (EndPoint.X);
      bw.Write (EndPoint.Y);
   }
   public abstract Shape LoadBinary (BinaryReader br);// Load as binary
   public Pen Highlighter => new (Brushes.Green, 1) { DashStyle = new DashStyle (new double[] { 2, 2 }, 0) };
}
#endregion

#region Scribble--------------------------------------------------------------------------------------
class Scribble : Shape {
   public Scribble (Pen pen) {
      mPoints = new List<Point> ();
      (Type, Pen) = (1, pen);
   }

   public override Point StartPoint {
      set { base.StartPoint = value; AddPoints (base.StartPoint); }
   }

   public override Point EndPoint {
      set { base.EndPoint = value; AddPoints (base.EndPoint); }
   }
   void AddPoints (Point point) => mPoints.Add (point); // Add points to the list
   List<Point> mPoints;

   public override void Draw (DrawingContext dc) { // Render the scribble
      for (int i = 1; i < mPoints.Count; i++)
         dc.DrawLine (Pen, mPoints[i - 1], mPoints[i]);
   }

   public override void SaveBinary (BinaryWriter bw) { // Save the scribble as binary
      bw.Write (mPoints.Count); // Total number of points in one scribble
      foreach (var point in mPoints) {
         bw.Write (point.X); // X coordinate of point
         bw.Write (point.Y); // Y coordinate of point
      }
   }

   public override Shape LoadBinary (BinaryReader br) { // Load the scribble from binary file
      Scribble scribble = new (Pen);
      int pCount = br.ReadInt32 (); // Total number of points in one scribble
      for (int j = 0; j < pCount; j++) {
         var (x, y) = (br.ReadDouble (), br.ReadDouble ()); // X & Y co-ordinate of point
         scribble.AddPoints (new (x, y));
      }
      return scribble;
   }
}
#endregion

#region Line--------------------------------------------------------------------------------------
class Line : Shape {
   public Line (Pen pen) {
      mCLines = new List<Line> ();
      (Type, Pen) = (2, pen);
   }
   public bool IsCLine {
      get => mIsCLine;
      set => mIsCLine = value;
   }

   public List<Line> mCLines;
   public override void Draw (DrawingContext dc) {
      if (IsCLine) {
         foreach (var line in mCLines) {
            dc.DrawLine (Pen, line.StartPoint, line.EndPoint);
            if (IsSelected) dc.DrawLine (Highlighter, line.StartPoint, line.EndPoint);
         }
      } else {
         dc.DrawLine (Pen, StartPoint, EndPoint);
         if (IsSelected) dc.DrawLine (Highlighter, StartPoint, EndPoint);
      }
   }
   public override void SaveBinary (BinaryWriter bw) {
      bw.Write (IsCLine); // Boolean variable to check if it is connected line
      if (IsCLine) {
         bw.Write (mCLines.Count);
         for (int i = 0; i < mCLines.Count; i++) {
            var (start, end) = (mCLines[i].StartPoint, mCLines[i].EndPoint);
            bw.Write (start.X);
            bw.Write (start.Y);
            bw.Write (end.X);
            bw.Write (end.Y);
         }
      } else base.SaveBinary (bw);
   }
   public override Shape LoadBinary (BinaryReader br) { // Load the line from binary file
      var isCLine = br.ReadBoolean ();
      if (isCLine) {
         Line cLine = new (Pen) { IsCLine = isCLine };
         var count = br.ReadInt32 ();
         for (int i = 0; i < count; i++) {
            var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
            Line line = new (Pen) { StartPoint = new (startX, startY), EndPoint = new (endX, endY) };
            cLine.mCLines.Add (line);
         }
         return cLine;
      } else {
         var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
         Line line = new (Pen) { StartPoint = new (startX, startY), EndPoint = new (endX, endY) };
         return line;
      }
   }

   bool mIsCLine = false;
}
#endregion

#region Rectangle--------------------------------------------------------------------------------------
class Rectangle : Shape {
   public Rectangle (Pen pen) => (Type, Pen) = (3, pen);
   public override void Draw (DrawingContext dc) { // Render the rectangle
      Rect rect = new (StartPoint, EndPoint);
      dc.DrawRectangle (null, Pen, rect);
      if (IsSelected) dc.DrawRectangle (null, Highlighter, rect);
   }

   public override Shape LoadBinary (BinaryReader br) { // Load the rectangle from binary file
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      Rectangle rectangle = new (Pen) {
         StartPoint = new (startX, startY),
         EndPoint = new (endX, endY)
      };
      return rectangle;
   }
}
#endregion

#region Ellipse--------------------------------------------------------------------------------------
class Ellipse : Shape {
   public Ellipse (Pen pen) => (Type, Pen) = (4, pen);
   public override void Draw (DrawingContext dc) { // Render the ellipse
      mCenter = new (StartPoint.X, StartPoint.Y);
      (mRadiusX, mRadiusY) = (Math.Abs (EndPoint.X - mCenter.X) / 2, Math.Abs (EndPoint.Y - mCenter.Y) / 2);
      mEllipse = new EllipseGeometry (mCenter, mRadiusX, mRadiusY);
      dc.DrawGeometry (null, Pen, mEllipse);
      if (IsSelected) dc.DrawGeometry (null, Highlighter, mEllipse);
   }

   public override Shape LoadBinary (BinaryReader br) { // Load the ellipse from binary file
      var (startX, startY, endX, endY) = (br.ReadDouble (), br.ReadDouble (), br.ReadDouble (), br.ReadDouble ());
      Ellipse ellipse = new (Pen) {
         StartPoint = new (startX, startY),
         EndPoint = new (endX, endY)
      };
      return ellipse;
   }

   EllipseGeometry mEllipse;
   double mRadiusX, mRadiusY;
   Point mCenter;
}
#endregion