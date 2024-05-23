using DesignCraft.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Point = DesignCraft.Lib.Point;

namespace DesignCraft;

public class Selector : Drawing {

   #region Constructor -------------------------------------------------------------------------------------
   public Selector (DrawingSurface eventSource) {
      mEventSource = eventSource;
      mDrawing = mEventSource.Drawing;
      Attach ();
   }
   #endregion

   #region Implementation ----------------------------------------------------------------------------------

   #region Mouse Events ------------------------------------------------------------------------------------
   void Attach () {
      mEventSource.MouseDown += OnMouseDown;
      mEventSource.MouseMove += OnMouseMove;
      mEventSource.MouseUp += OnMouseUp;
   }

   public void Detach () {
      mEventSource.MouseDown -= OnMouseDown;
      mEventSource.MouseMove -= OnMouseMove;
      mEventSource.MouseUp -= OnMouseUp;
   }

   void OnMouseDown (object sender, MouseButtonEventArgs e) {
      if (mStartPt == null)
         mStartPt = PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource)));
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         mEndPt = PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource)));
         if (!mIsDragging) mIsDragging = true;
         mEventSource.InvalidateVisual ();
      }
   }

   void OnMouseUp (object sender, MouseButtonEventArgs e) {
      if (mIsDragging) {
         RectangleSelect ();
         mIsDragging = false;
      } else {
         if (mIsSelected) Deselect ();
         PickSelect (mStartPt);
      }
      Reset ();
      mEventSource.InvalidateVisual ();
   }
   #endregion

   #region Selection Methods -------------------------------------------------------------------------------
   void RectangleSelect () {
      var (corner1, corner2) = (PointOperation.ToCustomPoint (mEndPt.Value.X, mStartPt.Value.Y),
                                PointOperation.ToCustomPoint (mStartPt.Value.X, mEndPt.Value.Y));
      var bound = new Bound (new List<Point> { mStartPt.Value, corner1, corner2, mEndPt.Value });
      foreach (var pLine in mDrawing.Plines)
         if (bound.IsInside (pLine.Bound))
            pLine.IsSelected = mIsSelected = true;
   }

   void PickSelect (Point? point) {
      foreach (var pLine in mDrawing.Plines) {
         var points = pLine.GetPoints ().ToList ();
         for (int i = 0; i < points.Count - 1; i++) {
            var (start, end) = (points[i], points[i + 1]);
            double distance = DistanceFromLineToPoint (start, end, point.Value);
            if (distance < 5) {
               pLine.IsSelected = mIsSelected = true;
               return;
            }
         }
      }
   }

   double DistanceFromLineToPoint (Point start, Point end, Point point) {
      var (A, B, C) = (end.Y - start.Y, start.X - end.X, start.Y * (end.X - start.X) - (end.Y - start.Y) * start.X);
      return Math.Abs (A * point.X + B * point.Y + C) / Math.Sqrt (A * A + B * B);
   }

   public void Deselect () {
      mIsSelected = false;
      foreach (var pLine in mDrawing.Plines.Where (x => x.IsSelected))
         pLine.IsSelected = false;
   }
   #endregion

   #region Utility Methods ---------------------------------------------------------------------------------
   void Reset () => mStartPt = mEndPt = null;
   #endregion

   #region Feedback ----------------------------------------------------------------------------------------
   public override void Draw (DrawingCommands drawingCommands) {
      if (mStartPt == null || mEndPt == null) return;
      var (brush, xfm, dc) = (Brushes.LightBlue, drawingCommands.Xfm, drawingCommands.DC);
      Rect rect = new Rect (xfm.Transform (PointOperation.ToSystemPoint (mStartPt.Value)),
                           xfm.Transform (PointOperation.ToSystemPoint (mEndPt.Value)));
      dc.DrawRectangle (brush, null, rect);
   }
   #endregion

   #endregion

   #region Private Field -----------------------------------------------------------------------------------
   bool mIsDragging = false, mIsSelected = false;
   Drawing mDrawing;
   DrawingSurface mEventSource;
   Point? mStartPt, mEndPt;
   #endregion
}
