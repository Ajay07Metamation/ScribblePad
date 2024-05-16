using DesignCraft.Lib;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Point = DesignCraft.Lib.Point;

namespace DesignCraft;
public partial class MainWindow : Window {

   #region Constructor -----------------------------------------------------------------------------------------------
   public MainWindow () {
      InitializeComponent ();
      mType = 0;
      mDrawing = new ();
      mDocManager = new ();
      mPanWidget = new PanWidget (mDwgSurface, OnPan);
      Loaded += delegate {
         SetTitle ();
         var bound = new Bound (new Point (-10, -10), new Point (1000, 1000));
         mDwgSurface.mProjXfm = Util.ComputeZoomExtentsProjXfm (mDwgSurface.ActualWidth, mDwgSurface.ActualHeight, 20, bound);
         mDwgSurface.mInvProjXfm = mDwgSurface.mProjXfm; mDwgSurface.mInvProjXfm.Invert ();
         mDwgSurface.Drawing = mDocManager.Drawing = mDrawingEditor.Drawing = mDrawing;
         mDwgSurface.Xfm = mDwgSurface.mProjXfm;
         mDrawing.RedrawReq += () => mDwgSurface.InvalidateVisual ();
         PreviewKeyDown += OnPreviewKeyDown;
      };
      mDwgSurface.MouseMove += (sender, e) => {
         var ptCanvas = e.GetPosition (mDwgSurface);
         var ptDrawing = mDwgSurface.mInvProjXfm.Transform (ptCanvas);
         mInfoBar.Text = $"Co-ordinates X : {ptDrawing.X:F2}  Y : {ptDrawing.Y:F2}";
      };
   }
   #endregion

   #region Implementation --------------------------------------------------------------------------------------------

   #region Entity ------------------------------------------------------------------------------------------
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
         3 => new CLineBuilder (mDwgSurface),
         _ => throw new NotImplementedException ()
      };
      mDwgSurface.EntityBuilder = mWidget;
      mWidget.Drawing = mDrawing;
      mWidget.Attach ();
   }
   #endregion

   #region Drawing Editor ----------------------------------------------------------------------------------
   void OnRedo_Click (object sender, RoutedEventArgs e) => mDrawingEditor.Redo ();
   void OnUndo_Click (object sender, RoutedEventArgs e) => mDrawingEditor.Undo ();
   #endregion

   #region Menu Items --------------------------------------------------------------------------------------
   void OnNew_Click (object sender, RoutedEventArgs e) {
      if (DrawingUpdated ())
         CheckAndPrompt ();
      else mDrawingEditor.Clear ();
   }

   void OnOpen_Click (object sender, RoutedEventArgs e) {
      if (DrawingUpdated ())
         CheckAndPrompt ();
      mDocManager.Load ();
      SetTitle ();
   }

   void OnSave_Click (object sender, RoutedEventArgs e) {
      if (DrawingUpdated ()) {
         mDocManager.Save ();
         if (mDocManager.IsSaved) {
            mWidget.IsModified = false;
            SetTitle ();
         }
      }
   }

   void OnSaveAs_Click (object sender, RoutedEventArgs e) {
      if (mWidget != null) {
         mDocManager.IsSaved = false;
         mDocManager.Save ();
         if (mDocManager.IsSaved) {
            mWidget.IsModified = false;
            SetTitle ();
         }
      }
   }

   void OnPreviewKeyDown (object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape) mWidget.EndEntity ();
      else Shortcuts (sender, e);
   }

   void OnClosing_Click (object sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = DrawingUpdated () ? CheckAndPrompt () : false;
   #endregion

   #region Helper functions --------------------------------------------------------------------------------
   bool DrawingUpdated () => mWidget != null && mDrawing.Count != 0 && mWidget.IsModified;
   bool CheckAndPrompt () {
      bool isCanceled = false;
      MessageBoxResult messageBox = MessageBox.Show ("Do you want to save your changes?", "CAD2Point",
                                               MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      switch (messageBox) {
         case MessageBoxResult.Yes: mDocManager.Save (); mDrawingEditor.Clear (); break;
         case MessageBoxResult.No: mDrawingEditor.Clear (); break;
         case MessageBoxResult.Cancel: isCanceled = true; break;
      }
      return isCanceled;
   }
   void Shortcuts (object sender, KeyEventArgs e) {
      if (Keyboard.Modifiers == ModifierKeys.Control)
         switch (e.Key) {
            case Key.Z: OnUndo_Click (sender, e); break;
            case Key.Y: OnRedo_Click (sender, e); break;
            case Key.N: OnNew_Click (sender, e); mDocManager.SavedFileName = "Untitled"; break;
            case Key.S: OnSave_Click (sender, e); break;
            case Key.O: OnOpen_Click (sender, e); break;
         } else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F4) {
         Window window = new ();
         window.Close ();
      }
   }
   void OnPan (Vector panDisp) {
      Matrix m = Matrix.Identity; m.Translate (panDisp.X, panDisp.Y);
      mDwgSurface.mProjXfm.Append (m);
      mDwgSurface.mInvProjXfm = mDwgSurface.mProjXfm; mDwgSurface.mInvProjXfm.Invert ();
      mDwgSurface.Xfm = mDwgSurface.mProjXfm;
      mDwgSurface.InvalidateVisual ();
   }
   void SetTitle () => Title = Path.GetFileNameWithoutExtension (mDocManager.SavedFileName) + " - Design Craft";
   #endregion

   #region CommandBinding --------------------------------------------------------------------------------------------
   void CanExecute_Undo (object sender, CanExecuteRoutedEventArgs e) {
      if (mDrawing.Count > 0) mUndo.IsEnabled = e.CanExecute = true;
   }
   void CanExecute_Redo (object sender, CanExecuteRoutedEventArgs e) {
      mDrawingEditor.CanRedo (sender, e);
      mRedo.IsEnabled = e.CanExecute;
   }
   void CanExecute_Save (object sender, CanExecuteRoutedEventArgs e) {
      if (mDrawing.Count > 0 && mWidget != null) {
         var result = mWidget.IsModified;
         mSave.IsEnabled = e.CanExecute = result;
      }
   }
   #endregion

   #endregion

   #region Private Field ---------------------------------------------------------------------------------------------
   Widget mWidget;
   Drawing mDrawing = new ();
   DrawingEditor mDrawingEditor = new ();
   DocManager mDocManager;
   PanWidget mPanWidget;
   int mType;
   public static RoutedCommand Undo = new (), Redo = new (), Save = new ();
   #endregion
}