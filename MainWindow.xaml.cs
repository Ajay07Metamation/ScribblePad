using DesignLib;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Point = DesignLib.Point;


namespace DesignCraft;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {

   #region Constructor -----------------------------------------------------------------------------------------------
   public MainWindow () {
      InitializeComponent ();
      mType = 0;
      mDrawing = new ();
      mDocManager = new ();
      Loaded += delegate {
         var bound = new Bound (new Point (-10, -10), new Point (1000, 1000));
         mDwgSurface.mProjXfm = Util.ComputeZoomExtentsProjXfm (mDwgSurface.ActualWidth, mDwgSurface.ActualHeight, 20, bound);
         mDwgSurface.mInvProjXfm = mDwgSurface.mProjXfm; mDwgSurface.mInvProjXfm.Invert ();
         mDwgSurface.Drawing = mDocManager.Drawing = mDrawingEditor.Drawing = mDrawing;
         mDwgSurface.Xfm = mDwgSurface.mProjXfm;
         mDrawing.RedrawReq += () => mDwgSurface.InvalidateVisual ();
      };
   }
   #endregion

   #region Implementation --------------------------------------------------------------------------------------------
   void ToggleButton_Click (object sender, RoutedEventArgs e) {
      foreach (var child in mPanel.Children)
         if (child is ToggleButton toggleButton && toggleButton != e.Source)
            toggleButton.IsChecked = false;
   }

   void OnShape_Click (object sender, RoutedEventArgs e) {
      mWidget?.Detach ();
      if (sender is ToggleButton toggleButton) int.TryParse (toggleButton.Tag?.ToString (), out mType);
      mWidget = mType switch {
         1 => new LineBuilder (mDwgSurface),
         2 => new RectBuilder (mDwgSurface),
         _ => throw new NotImplementedException ()
      };
      mDwgSurface.EntityBuilder = mWidget;
      mWidget.Drawing = mDrawing;
      mWidget.Attach ();
   }
   private void OnRedo_Click (object sender, RoutedEventArgs e) { mDrawingEditor.Redo (); }
   private void OnUndo_Click (object sender, RoutedEventArgs e) { mDrawingEditor.Undo (); }
   private void OnSave_Click (object sender, RoutedEventArgs e) { mDocManager.Save (); }
   private void OnOpen_Click (object sender, RoutedEventArgs e) { mDocManager.Load (); }
   private void OnClear_Click (object sender, RoutedEventArgs e) { mDrawingEditor.Clear (); }


   #region CommandBinding --------------------------------------------------------------------------------------------
   private void CanExecute_Undo (object sender, CanExecuteRoutedEventArgs e) {
      var result = mDrawing.Plines.Any ();
      (mUndo.IsEnabled, e.CanExecute) = (result, result);
   }
   private void CanExecute_Redo (object sender, CanExecuteRoutedEventArgs e) {
      mDrawingEditor.CanRedo (sender, e);
      mRedo.IsEnabled = e.CanExecute;
   }
   private void CanExecute_Save (object sender, CanExecuteRoutedEventArgs e) {
      var result = mDrawing.Plines.Any ();
      (mSave.IsEnabled, e.CanExecute) = (result, result);
   }
   #endregion
   #endregion

   #region Private Field ---------------------------------------------------------------------------------------------
   EntityWidget? mWidget;
   Drawing mDrawing = new ();
   DrawingEditor mDrawingEditor = new ();
   DocManager mDocManager;
   int mType;
   public static RoutedCommand Undo = new ();
   public static RoutedCommand Redo = new ();
   public static RoutedCommand Save = new ();
   #endregion
}