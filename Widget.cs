using DesignLib;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Point = DesignLib.Point;

namespace DesignCraft;

public abstract class EntityWidget : Drawing {

   #region Constructor ------------------------------------------------------------------------------------
   public EntityWidget (Paint eventSource) {
      mEventSource = eventSource;
      mDrawing = new ();
      mPanWidget = new PanWidget (eventSource, OnPan);
      mEventSource.MouseWheel += MEventSource_MouseWheel;
      mEventSource.MouseRightButtonDown += MEventSource_MouseRightButtonDown;
   }
   #endregion

   #region Implementation ---------------------------------------------------------------------------------

   #region Mouse Events------------------------------------------------------------------------------------
   public void Attach () {
      mEventSource.MouseLeftButtonDown += MEventSource_MouseLeftButtonDown;
      mEventSource.MouseMove += MEventSource_MouseMove;
   }
   public void Detach () {
      mEventSource.MouseLeftButtonDown -= MEventSource_MouseLeftButtonDown;
      mEventSource.MouseMove -= MEventSource_MouseMove;
   }
   void MEventSource_MouseLeftButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e) {
      DrawingClicked (PointConverter.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource))));
   }

   void MEventSource_MouseMove (object sender, System.Windows.Input.MouseEventArgs e) {
      DrawingHover (PointConverter.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource))));
   }

   void MEventSource_MouseRightButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e) {
      mEventSource.mProjXfm = Util.ComputeZoomExtentsProjXfm (mEventSource.ActualWidth, mEventSource.ActualHeight, mViewMargin, mDrawing.Bound);
      mEventSource.mInvProjXfm = mEventSource.mProjXfm; mEventSource.mInvProjXfm.Invert ();
      mEventSource.Xfm = mEventSource.mProjXfm;
      mEventSource.InvalidateVisual ();
   }

   void MEventSource_MouseWheel (object sender, System.Windows.Input.MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      var ptDraw = PointConverter.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource))); // mouse point in drawing space
      var cornerA = PointConverter.ToCustomPoint (mEventSource.mInvProjXfm.Transform (PointConverter.ToSystemPoint (mViewMargin, mViewMargin)));
      var cornerB = PointConverter.ToCustomPoint (mEventSource.mInvProjXfm.Transform (PointConverter.ToSystemPoint (mEventSource.ActualWidth - mViewMargin, mEventSource.ActualHeight - mViewMargin)));
      var b = new Bound (cornerA, cornerB);
      b = b.Inflated (ptDraw, zoomFactor);
      mEventSource.mProjXfm = Util.ComputeZoomExtentsProjXfm (mEventSource.ActualWidth, mEventSource.ActualHeight, mViewMargin, b);
      mEventSource.mInvProjXfm = mEventSource.mProjXfm; mEventSource.mInvProjXfm.Invert ();
      mEventSource.Xfm = mEventSource.mProjXfm;
      mEventSource.InvalidateVisual ();
   }

   void OnPan (Vector panDisp) {
      Matrix m = Matrix.Identity; m.Translate (panDisp.X, panDisp.Y);
      mEventSource.mProjXfm.Append (m);
      mEventSource.mInvProjXfm = mEventSource.mProjXfm; mEventSource.mInvProjXfm.Invert ();
      mEventSource.Xfm = mEventSource.mProjXfm;
      mEventSource.InvalidateVisual ();
   }
   #endregion

   #region Construct Entity -------------------------------------------------------------------------------
   protected abstract Pline? PointClicked (Point drawingPt);
   protected virtual void PointHover (Point drawingPt) { mHoverPt = drawingPt; mEventSource.InvalidateVisual (); }
   protected virtual void DrawingClicked (Point drawingPt) {
      var pline = PointClicked (drawingPt);
      if (pline == null) return;
      mDrawing.AddPline (pline);
   }
   protected virtual void DrawingHover (Point drawingPt) => PointHover (drawingPt);
   #endregion
   #endregion

   #region Properties -------------------------------------------------------------------------------------
   public Drawing Drawing { get => mDrawing; set => mDrawing = value; }
   #endregion

   #region Private Field ----------------------------------------------------------------------------------
   protected Point? mFirstPt;
   protected Point? mHoverPt;
   protected Drawing mDrawing;
   protected Paint mEventSource;
   readonly PanWidget mPanWidget;
   readonly double mViewMargin = 20;
   #endregion
}

public class LineBuilder : EntityWidget {

   #region Constructor ------------------------------------------------------------------------------------
   public LineBuilder (Paint eventSource) : base (eventSource) { }
   #endregion

   #region Implementation ---------------------------------------------------------------------------------

   #region Construct Entity -------------------------------------------------------------------------------
   protected override Pline? PointClicked (Point drawingPt) {
      if (mFirstPt is null) {
         mFirstPt = drawingPt;
         return null;
      } else {
         var firstPt = mFirstPt.Value;
         mFirstPt = null;
         return Pline.CreateLine (firstPt, drawingPt);
      }
   }
   #endregion

   #region Feedback ---------------------------------------------------------------------------------------
   public override void Draw (DrawingCommands drawingCommands) {
      if (mFirstPt == null || mHoverPt == null) return;
      drawingCommands.DrawLine (mFirstPt.Value, mHoverPt.Value, Brushes.Red);
   }
   #endregion

   #endregion

}
public class RectBuilder : EntityWidget {

   #region Constructor ------------------------------------------------------------------------------------
   public RectBuilder (Paint eventSource) : base (eventSource) { }
   #endregion

   #region Implementation ---------------------------------------------------------------------------------

   #region Construct Entity -------------------------------------------------------------------------------
   protected override Pline? PointClicked (Point drawingPt) {
      if (mFirstPt is null) {
         mFirstPt = mCorner1 = drawingPt;
         return null;
      } else {
         mCorner2 = PointConverter.ToCustomPoint (drawingPt.X, mCorner1.Y);
         mCorner3 = drawingPt;
         mCorner4 = PointConverter.ToCustomPoint (mCorner1.X, drawingPt.Y);
         mFirstPt = null;
         return Pline.CreateRectangle (mCorner1, mCorner2, mCorner3, mCorner4);
      }
   }
   protected override void PointHover (Point drawingPt) {
      mCorner2 = PointConverter.ToCustomPoint (drawingPt.X, mCorner1.Y);
      mHoverPt = mCorner3 = drawingPt;
      mCorner4 = PointConverter.ToCustomPoint (mCorner1.X, drawingPt.Y);
      mEventSource.InvalidateVisual ();
   }
   #endregion

   #region Feedback ---------------------------------------------------------------------------------------
   public override void Draw (DrawingCommands drawingCommands) {
      if (mFirstPt == null || mHoverPt == null) return;
      drawingCommands.DrawLines (new List<Point> { mCorner1, mCorner2, mCorner3, mCorner4, mCorner1 });
   }
   #endregion

   #endregion

   #region Private Field ----------------------------------------------------------------------------------
   Point mCorner1, mCorner2, mCorner3, mCorner4;
   #endregion
}