using DesignLib;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Point = System.Windows.Point;
using Shape = DesignLib.Shape;

namespace DesignCraft;

#region Class Paint --------------------------------------------------------------------------------
public class Paint : Canvas {
   #region Constructor ---------------------------------------------------------------------------
   public Paint () {}
   #endregion

   #region Implementation ------------------------------------------------------------------------
   protected override void OnRender (DrawingContext dc) {
      base.OnRender (dc);
      foreach (var entity in mEntityCollection) entity.Draw (new DrawEntity (dc) { Thickness = 2 });
      if (CurrentEntity != null) CurrentEntity.Draw (new DrawEntity (dc) { Thickness = 1 });
   }
   public void AddEntity (Shape entity) => EntityCollection.Add (entity);
   #endregion

   #region Properties ----------------------------------------------------------------------------
   public List<Shape> EntityCollection => mEntityCollection;
   public Shape CurrentEntity { get => mCurrentEntity; set => mCurrentEntity = value; }
   #endregion

   #region Private Field -------------------------------------------------------------------------
   List<Shape> mEntityCollection = new ();
   Shape mCurrentEntity;
   #endregion
}
#endregion

#region Class DrawEntity ---------------------------------------------------------------------------
public class DrawEntity : IDrawable {
   #region Constructor -------------------------------------------------------------------------------
   public DrawEntity (DrawingContext dc) { mDc = dc; }
   #endregion

   #region Implementation ----------------------------------------------------------------------------
   public void DrawLine (DesignLib.Point start, DesignLib.Point end, (byte, byte, byte, byte) brush) {
      var (colour, startPoint, endPoint) = Convert (brush, start, end);
      mDc.DrawLine (new Pen (colour, Thickness), startPoint, endPoint);
   }

   public void DrawRectangle (DesignLib.Point start, DesignLib.Point end, (byte, byte, byte, byte) brush) {
      var (colour, startPoint, endPoint) = Convert (brush, start, end);
      Rect rect = new (startPoint, endPoint);
      mDc.DrawRectangle (null, new Pen (colour, Thickness), rect);
   }

   public void DrawCircle (DesignLib.Point centre, DesignLib.Point end, (byte, byte, byte, byte) brush) {
      var (colour, startPoint, endPoint) = Convert (brush, centre, end);
      var radius = Math.Sqrt (Math.Pow (startPoint.X - endPoint.X, 2) + Math.Pow (startPoint.Y - endPoint.Y, 2));
      mDc.DrawEllipse (null, new Pen (colour, Thickness), startPoint, radius, radius);
   }
   public void DrawCLine (List<DesignLib.Point> pointList, DesignLib.Point start, DesignLib.Point end, (byte, byte, byte, byte) brush) {
      var (colour, startPoint, endPoint) = Convert (brush, start, end);
      for (int i = 1; i < pointList.Count; i++)
         mDc.DrawLine (new Pen (colour, Thickness), new Point (pointList[i - 1].X, pointList[i - 1].Y), new Point (pointList[i].X, pointList[i].Y));
      mDc.DrawLine (new Pen (colour, Thickness), startPoint, endPoint);
   }

   (Brush, Point, Point) Convert ((byte, byte, byte, byte) brush, DesignLib.Point point1, DesignLib.Point point2) {
      var cPoint1 = new Point (point1.X, point1.Y);
      var cPoint2 = new Point (point2.X, point2.Y);
      SolidColorBrush brushColour = new () { Color = Color.FromArgb (brush.Item1, brush.Item2, brush.Item3, brush.Item4) };
      return (brushColour, cPoint1, cPoint2);
   }
   #endregion

   #region Properties --------------------------------------------------------------------------------
   public double Thickness { get => mThickness; set => mThickness = value; }
   #endregion

   #region Private Field -----------------------------------------------------------------------------
   double mThickness;
   DrawingContext mDc;
   #endregion
}
#endregion