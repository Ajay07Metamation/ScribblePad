using DesignLib;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DesignCraft;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
   #region Constructor -----------------------------------------------------------------------------------------------
   public MainWindow () {
      InitializeComponent ();
      Initialize ();
      KeyDown += OnKeyDown;
      Title = GetFileName + " DesignPro";
   }
   #endregion

   #region Event Handlers---------------------------------------------------------------------------------------------
   void ToggleButton_Click (object sender, RoutedEventArgs e) {
      foreach (var child in mPanel.Children)
         if (child is ToggleButton toggleButton && toggleButton != e.Source)
            toggleButton.IsChecked = false;
   }

   private void OnKeyDown (object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape && mDisplay.CurrentEntity is CLine) mCurrentDrawer.EndEntity ();
   }

   #region Drawing Editor --------------------------------------------------------------------------------------------
   void OnClear_Click (object sender, RoutedEventArgs e) { mDisplay.CurrentEntity = null; mDrawing.Clear (); mDisplay.InvalidateVisual (); }
   void OnColourChange_Click (object sender, RoutedEventArgs e) { mDrawing.ColorChange (); mDisplay.InvalidateVisual (); }
   private void OnRedo_Click (object sender, RoutedEventArgs e) { mDrawing.Redo (); mDisplay.InvalidateVisual (); }
   private void OnUndo_Click (object sender, RoutedEventArgs e) { mDrawing.Undo (); mDisplay.InvalidateVisual (); }
   #endregion

   #region Widget ----------------------------------------------------------------------------------------------------
   void OnShape_Click (object sender, RoutedEventArgs e) {
      mCurrentDrawer?.Detach ();
      if (sender is ToggleButton toggleButton) int.TryParse (toggleButton.Tag?.ToString (), out mType);
      mCurrentDrawer = mType switch {
         1 => new DrawCLine (mDisplay),
         2 => new DrawLine (mDisplay),
         3 => new DrawRectangle (mDisplay),
         4 => new DrawCircle (mDisplay),
         _ => throw new NotImplementedException ()
      };
      mCurrentDrawer.Attach ();
      Initialize ();
   }
   void Initialize () {
      var collection = mDisplay.EntityCollection;
      mDrawing = new (collection);
      mDoc = new (collection);
   }
   #endregion

   #region Menu Items-------------------------------------------------------------------------------------------------
   void OnNew_Click (object sender, RoutedEventArgs e) { mDoc.CheckAndPrompt (); mDisplay.InvalidateVisual (); }
   void OnOpen_Click (object sender, RoutedEventArgs e) { mDoc.Load (); mDisplay.InvalidateVisual (); }
   void OnSave_Click (object sender, RoutedEventArgs e) {
      if (mCurrentDrawer.IsModified) {
         mDoc.Save ();
         Title = GetFileName + " DesignPro";
         mCurrentDrawer.IsModified = false;
      }
   }
   void OnSaveAs_Click (object sender, RoutedEventArgs e) { mDoc.IsSaved = false; mDoc.Save (); }
   void OnExit_Click (object sender, RoutedEventArgs e) => Close ();
   void Window_Closing (object sender, CancelEventArgs e) => e.Cancel = mCurrentDrawer.IsModified ? mDoc.CheckAndPrompt () : true;
   #endregion

   #region CommandBinding --------------------------------------------------------------------------------------------
   private void CanExecute_Undo (object sender, CanExecuteRoutedEventArgs e) {
      if (mDisplay != null) {
         var result = mDisplay.EntityCollection.Any ();
         (mUndo.IsEnabled, e.CanExecute) = (result, result);
      }
   }
   private void CanExecute_Redo (object sender, CanExecuteRoutedEventArgs e) {
      mDrawing?.CanRedo (sender, e);
      mRedo.IsEnabled = e.CanExecute;
   }
   private void CanExecute_Save (object sender, CanExecuteRoutedEventArgs e) {
      if (mDisplay != null) {
         var result = mDisplay.EntityCollection.Any ();
         (mSave.IsEnabled, e.CanExecute) = (result, result);
      }
   }
   #endregion

   #region Properties ------------------------------------------------------------------------------------------------
   string GetFileName => Path.GetFileNameWithoutExtension (mDoc.TitleName);
   #endregion

#endregion

   #region Fields ----------------------------------------------------------------------------------------------------
   Widget mCurrentDrawer;
   DocManager mDoc;
   DrawingEditor mDrawing;
   int mType;
   public static RoutedCommand Undo = new ();
   public static RoutedCommand Redo = new ();
   public static RoutedCommand Save = new ();
   #endregion
}