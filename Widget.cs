using DesignCraft.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Point = DesignCraft.Lib.Point;

namespace DesignCraft;

public abstract class Widget : Drawing, INotifyPropertyChanged {

   #region Constructor ------------------------------------------------------------------------------------
   public Widget (DrawingSurface eventSource) {
      mEventSource = eventSource;
      mDrawing = new ();
      mEventSource.MouseWheel += MEventSource_MouseWheel;
      mEventSource.MouseRightButtonDown += MEventSource_MouseRightButtonDown;
      Initialize ();
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

   void MEventSource_MouseLeftButtonDown (object sender, MouseButtonEventArgs e) =>
       DrawingClicked (PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource))));

   void MEventSource_MouseMove (object sender, MouseEventArgs e) =>
       DrawingHover (PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource))));

   void MEventSource_MouseRightButtonDown (object sender, MouseButtonEventArgs e) {
      mEventSource.mProjXfm = Util.ComputeZoomExtentsProjXfm (mEventSource.ActualWidth, mEventSource.ActualHeight, mViewMargin, mDrawing.Bound);
      mEventSource.mInvProjXfm = mEventSource.mProjXfm; mEventSource.mInvProjXfm.Invert ();
      mEventSource.Xfm = mEventSource.mProjXfm;
      mEventSource.InvalidateVisual ();
   }

   void MEventSource_MouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0)
         zoomFactor = 1 / zoomFactor;
      var ptDraw = PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (e.GetPosition (mEventSource)));
      var cornerA = PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (PointOperation.ToSystemPoint (mViewMargin, mViewMargin)));
      var cornerB = PointOperation.ToCustomPoint (mEventSource.mInvProjXfm.Transform (PointOperation.ToSystemPoint (mEventSource.ActualWidth - mViewMargin, mEventSource.ActualHeight - mViewMargin)));
      var b = new Bound (cornerA, cornerB);
      b = b.Inflated (ptDraw, zoomFactor);
      mEventSource.mProjXfm = Util.ComputeZoomExtentsProjXfm (mEventSource.ActualWidth, mEventSource.ActualHeight, mViewMargin, b);
      mEventSource.mInvProjXfm = mEventSource.mProjXfm; mEventSource.mInvProjXfm.Invert ();
      mEventSource.Xfm = mEventSource.mProjXfm;
      mEventSource.InvalidateVisual ();
   }
   #endregion

   #region Construct Entity -------------------------------------------------------------------------------
   protected abstract Pline PointClicked (Point drawingPt);
   protected virtual void PointHover (Point drawingPt) { mHoverPt = drawingPt; mEventSource.InvalidateVisual (); }
   void DrawingClicked (Point drawingPt) {
      var pline = PointClicked (drawingPt);
      if (pline == null) return;
      mDrawing.AddPline (pline);
      this.IsModified = true;
   }
   void DrawingHover (Point drawingPt) { if (mFirstPt != null) PointHover (drawingPt); }
   protected virtual void BuildEntity () { }
   public virtual void EndEntity () { }
   #endregion

   #region PropertyChangedEventHandler --------------------------------------------------------------------
   protected virtual void OnPropertyChanged (string propertyName) {
      PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
   }
   public event PropertyChangedEventHandler PropertyChanged;
   #endregion

   #region Status Bar -------------------------------------------------------------------------------------
   void Initialize () { GetElements (); UpdateInputBar (); }

   void GetElements () => InputBar = ((MainWindow)((DockPanel)((Border)mEventSource.Parent).Parent).Parent).mInputBar;
   void UpdateInputBar () {
      InputBar.Children.Clear ();
      var tblock = new TextBlock () { Margin = new Thickness (10, 5, 10, 0), FontWeight = FontWeights.Bold };
      tblock.SetBinding (TextBlock.TextProperty, new Binding (nameof (Prompt)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
      InputBar.Children.Add (tblock);
      foreach (var str in InputBox) {
         var tBlock = new TextBlock () { Text = str + ":", Margin = new Thickness (5, 5, 5, 0) };
         var tBox = new TextBox () { Name = str + "TextBox", Width = 50, Height = 20 };
         tBox.PreviewKeyDown += Tb_PreviewKeyDown;
         var binding = new Binding (str) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
         tBox.SetBinding (TextBox.TextProperty, binding);
         InputBar.Children.Add (tBlock);
         InputBar.Children.Add (tBox);
      }

      void Tb_PreviewKeyDown (object sender, KeyEventArgs e) {
         var key = e.Key;
         e.Handled = !((key is >= Key.D0 and <= Key.D9) ||
                       (key is >= Key.NumPad0 and <= Key.NumPad9) ||
                       (key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Tab));
         if (key == Key.Enter) BuildEntity ();
      }
   }
   #endregion
   #endregion

   #region Properties -------------------------------------------------------------------------------------

   public bool IsModified { get => mIsModified; set => mIsModified = value; }
   public Drawing Drawing { get => mDrawing; set => mDrawing = value; }

   #region Status bar -------------------------------------------------------------------------------------
   public double X { get => Math.Round (mX, 3); set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => Math.Round (mY, 3); set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public double DX { get => Math.Round (mDX, 3); set { mDX = value; OnPropertyChanged (nameof (DX)); } }
   public double DY { get => Math.Round (mDY, 3); set { mDY = value; OnPropertyChanged (nameof (DY)); } }
   public double Length { get => Math.Round (mLength, 3); set { mLength = value; OnPropertyChanged (nameof (Length)); } }
   public double Angle { get => Math.Round (mAngle, 3); set { mAngle = value; OnPropertyChanged (nameof (Angle)); } }
   public string Prompt { get => mPrompt; set { mPrompt = value; OnPropertyChanged (nameof (Prompt)); } }
   protected virtual string[] InputBox { get; set; }
   protected StackPanel InputBar { get; set; }
   #endregion

   #endregion

   #region Private Field ----------------------------------------------------------------------------------

   double mX, mY, mDX, mDY, mLength, mAngle; // Input bar 
   protected Point? mFirstPt, mHoverPt;
   protected Drawing mDrawing;
   protected DrawingSurface mEventSource;
   protected string[] mPrompts;
   string mPrompt;
   static bool mIsModified;
   readonly double mViewMargin = 20;
   #endregion
}

public class LineBuilder : Widget {

   #region Constructor ------------------------------------------------------------------------------------
   public LineBuilder (DrawingSurface eventSource) : base (eventSource) {
      mPrompts = new string[] { " Line: Pick start point", " Line: Pick end point" };
      Prompt = mPrompts[0];
   }
   #endregion

   #region Implementation ---------------------------------------------------------------------------------

   #region Construct Entity -------------------------------------------------------------------------------
   protected override Pline PointClicked (Point drawingPt) {
      if (mFirstPt is null) {
         (mFirstPt, X, Y, Prompt) = (drawingPt, drawingPt.X, drawingPt.Y, mPrompts[1]);
         return null;
      } else {
         var firstPt = mFirstPt.Value;
         mFirstPt = null;
         (DX, DY, Length, Angle, IsModified) = (drawingPt.X - firstPt.X, drawingPt.Y - firstPt.Y,
                                   PointOperation.Distance (firstPt, drawingPt),
                                   PointOperation.Angle (firstPt, drawingPt), true);
         return Pline.CreateLine (firstPt, drawingPt);
      }
   }

   protected override void BuildEntity () {
      if (DX == 0 && DY == 0 && Length == 0 && Angle == 0) return;
      else if (DX != 0 || DY != 0) {
         var (start, end) = (PointOperation.ToCustomPoint (X, Y), PointOperation.ToCustomPoint (DX, DY));
         (Length, Angle) = (PointOperation.Distance (start, end), PointOperation.Angle (start, end));
      } else {
         double radians = Angle * Math.PI / 180;
         (DX, DY) = (Length * Math.Cos (radians), Length * Math.Sin (radians));
      }
      var pLine = Pline.CreateLine (PointOperation.ToCustomPoint (X, Y), PointOperation.ToCustomPoint (X + DX, Y + DY));
      mDrawing.AddPline (pLine);
   }

   public override void EndEntity () {
      mFirstPt = mHoverPt = null;
      mEventSource.InvalidateVisual ();
   }
   #endregion

   #region Feedback ---------------------------------------------------------------------------------------
   public override void Draw (DrawingCommands drawingCommands) {
      if (mFirstPt == null || mHoverPt == null) return;
      drawingCommands.Brush = Brushes.Red;
      drawingCommands.DrawLine (mFirstPt.Value, mHoverPt.Value);
   }
   #endregion
   #endregion

   #region Properties -------------------------------------------------------------------------------------
   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (DX), nameof (DY), nameof (Length), nameof (Angle) };
   #endregion

}
public class RectBuilder : Widget {

   #region Constructor ------------------------------------------------------------------------------------
   public RectBuilder (DrawingSurface eventSource) : base (eventSource) {
      mPrompts = new string[] { " Rectangle: Pick first corner of rectangle", " Rectangle: Pick opposite corner of rectangle" };
      Prompt = mPrompts[0];
   }
   #endregion

   #region Implementation ---------------------------------------------------------------------------------

   #region Construct Entity -------------------------------------------------------------------------------
   protected override Pline PointClicked (Point drawingPt) {
      if (mFirstPt is null) {
         (mFirstPt, X, Y, Prompt) = (drawingPt, drawingPt.X, drawingPt.Y, mPrompts[1]);
         return null;
      } else {
         UpdateCorners (drawingPt);
         var (firstPt, hoverPt) = (mFirstPt.Value, mHoverPt.Value);
         (Prompt, mFirstPt) = (mPrompts[0], null);
         (Length, Breadth) = (PointOperation.Distance (firstPt, mCorner2), PointOperation.Distance (mCorner2, hoverPt));
         IsModified = true;
         return Pline.CreateRectangle (firstPt, mCorner2, hoverPt, mCorner4);
      }
   }

   protected override void PointHover (Point drawingPt) {
      UpdateCorners (drawingPt);
      base.PointHover (drawingPt);
   }

   void UpdateCorners (Point drawingPt) {
      if (mFirstPt == null && mHoverPt == null) return;
      (mCorner2, mCorner4) = (PointOperation.ToCustomPoint (drawingPt.X, mFirstPt.Value.Y), PointOperation.ToCustomPoint (mFirstPt.Value.X, drawingPt.Y));
   }

   protected override void BuildEntity () {
      if (Length == 0 || Breadth == 0) return;
      else
         (mFirstPt, mCorner2, mHoverPt, mCorner4) = (PointOperation.ToCustomPoint (X, Y), PointOperation.ToCustomPoint (X + Length, Y),
                                                  PointOperation.ToCustomPoint (X + Length, Y + Breadth), PointOperation.ToCustomPoint (X, Y + Breadth));
      var pLine = Pline.CreateRectangle (mFirstPt.Value, mCorner2, mHoverPt.Value, mCorner4);
      mDrawing.AddPline (pLine);
   }

   public override void EndEntity () {
      mFirstPt = mHoverPt = null;
      mEventSource.InvalidateVisual ();
   }
   #endregion

   #region Feedback ---------------------------------------------------------------------------------------
   public override void Draw (DrawingCommands drawingCommands) {
      if (mFirstPt == null || mHoverPt == null) return;
      drawingCommands.Brush = Brushes.Red;
      drawingCommands.DrawLines (new List<Point> { mFirstPt.Value, mCorner2, mHoverPt.Value, mCorner4, mFirstPt.Value });
   }
   #endregion

   #endregion

   #region Properties -------------------------------------------------------------------------------------
   public double Breadth { get => Math.Round (mBreadth, 3); set { mBreadth = value; OnPropertyChanged (nameof (Breadth)); } }
   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (Length), nameof (Breadth) };

   #endregion

   #region Private Field ----------------------------------------------------------------------------------
   Point mCorner2, mCorner4;
   double mBreadth;
   #endregion
}

public class CLineBuilder : Widget {

   #region Constructor ------------------------------------------------------------------------------------
   public CLineBuilder (DrawingSurface eventSource) : base (eventSource) {
      mPrompts = new string[] { " Connected Line: Pick start point", " Connected Line: Pick end point" };
      Prompt = mPrompts[0];
      Reset ();
   }
   #endregion

   #region Implementation ---------------------------------------------------------------------------------

   #region Construct Entity -------------------------------------------------------------------------------
   protected override Pline PointClicked (Point drawingPt) {
      if (mFirstPt is null) {
         mFirstPt = drawingPt;
         mPoints.Add (drawingPt);
      } else {
         (DX, DY) = (X + drawingPt.X, Y + drawingPt.Y);
         var start = PointOperation.ToCustomPoint (X, Y);
         (Length, Angle) = (PointOperation.Distance (start, drawingPt), PointOperation.Angle (start, drawingPt));
         mPoints.Add (drawingPt);
         mPos++;
      }
      (X, Y) = (drawingPt.X, drawingPt.Y);
      return null;
   }
   protected override void PointHover (Point drawingPt) {
      mHoverPt = drawingPt;
      mEventSource.InvalidateVisual ();
   }
   public override void EndEntity () {
      if (mPoints.Count >= 2) {
         var pLine = new Pline (mPoints);
         mDrawing.AddPline (pLine);
         IsModified = true;
      }
      Reset ();
   }
   void Reset () {
      mFirstPt = mHoverPt = null;
      mPoints = new ();
      mPos = 0;
   }
   protected override void BuildEntity () {
      if (DX == 0 && DY == 0 && Length == 0 && Angle == 0) return;
      else {
         if (mFirstPt is null) {
            var point = PointOperation.ToCustomPoint (X, Y);
            mPoints.Add (point);
            mFirstPt = point;
         }
         if (DX != 0 || DY != 0) {
            var (start, end) = (PointOperation.ToCustomPoint (X, Y), PointOperation.ToCustomPoint (DX, DY));
            (Length, Angle) = (PointOperation.Distance (start, end), PointOperation.Angle (start, end));
         } else {
            double radians = Angle * Math.PI / 180;
            (DX, DY) = (Length * Math.Cos (radians), Length * Math.Sin (radians));
         }
         var endPoint = PointOperation.ToCustomPoint (X + DX, Y + DY);
         mPoints.Add (endPoint);
         mPos++;
         (X, Y) = (endPoint.X, endPoint.Y);
         mEventSource.InvalidateVisual ();
      }
   }
   #endregion

   #region Feedback ---------------------------------------------------------------------------------------
   public override void Draw (DrawingCommands drawingCommands) {
      drawingCommands.Brush = Brushes.Red;
      if (mPoints.Count == 0) return;
      if (mPoints.Count >= 2)
         for (int i = 1; i < mPoints.Count; i++)
            drawingCommands.DrawLine (mPoints[i - 1], mPoints[i]);
      if (mHoverPt != null) drawingCommands.DrawLine (mPoints[mPos], mHoverPt.Value);
   }
   #endregion

   #endregion

   #region Properties -------------------------------------------------------------------------------------
   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (DX), nameof (DY), nameof (Length), nameof (Angle) };
   #endregion

   #region Private field ----------------------------------------------------------------------------------
   int mPos = 0;
   List<Point> mPoints;
   #endregion
}