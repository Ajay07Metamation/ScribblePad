using SPA;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;


namespace ScribblePad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      public MainWindow () {
         InitializeComponent ();
         mScribbleTool.IsChecked = true;
         PreviewKeyDown += OnDisplay_PreviewKeyDown;
      }

      #region Event Handlers---------------------------------------------------------------------------------------------
      private void OnDisplay_MouseDown (object sender, MouseButtonEventArgs e) {
         if (!mDrawing.IsDraw) {
            if (!mDrawing.IsSet) mDrawing.SetShape (sender);
            mDrawing.ConstructShape (e.GetPosition (this));
            mDrawing.AddShape ();
            (mDrawing.IsModified, mDrawing.IsDraw) = (true, true);
         } else if (mDrawing.CurrentShape != null) {
            mDrawing.ConstructShape (e.GetPosition (this));
            (mDrawing.IsDraw, mDrawing.IsSet) = (false, false);
            InvalidateVisual ();
         }
      }
      private void OnDisplay_MouseMove (object sender, MouseEventArgs e) {
         if (mDrawing.IsDraw) {
            mDrawing.ConstructShape (e.GetPosition (this));
            InvalidateVisual ();
         }
      }

      private void ToggleButton_Click (object sender, RoutedEventArgs e) {
         foreach (var child in mPanel.Children)
            if (child is ToggleButton toggleButton && toggleButton != e.Source)
               toggleButton.IsChecked = false;
      }
      private void OnDisplay_PreviewKeyDown (object sender, KeyEventArgs e) { mDrawing.Shortcuts (sender, e); InvalidateVisual (); }
      private void OnClear_Click (object sender, RoutedEventArgs e) { mDrawing.Clear (); InvalidateVisual (); }
      private void OnColourChange_Click (object sender, RoutedEventArgs e) { mDrawing.ColorChange (); InvalidateVisual (); }
      private void OnShape_Click (object sender, RoutedEventArgs e) => mDrawing.SetShape (sender);
      private void OnRedo_Click (object sender, RoutedEventArgs e) { mDrawing.Redo (); InvalidateVisual (); }
      private void OnUndo_Click (object sender, RoutedEventArgs e) { mDrawing.Undo (); InvalidateVisual (); }
      private void Window_Closing (object sender, CancelEventArgs e) => e.Cancel = mDrawing.CheckAndPrompt ();
      protected override void OnRender (DrawingContext dc) => mDrawing.DrawShape (dc);

      #region Menu Items-------------------------------------------------------------------------------------------------
      private void OnNew_Click (object sender, RoutedEventArgs e) { mDrawing.CheckAndPrompt (); mDrawing.Clear (); InvalidateVisual (); }
      private void OnOpen_Click (object sender, RoutedEventArgs e) { mDrawing.Load (); InvalidateVisual (); }
      private void OnSave_Click (object sender, RoutedEventArgs e) => mDrawing.Save ();
      private void OnSaveAs_Click (object sender, RoutedEventArgs e) { mDrawing.IsSaved = false; mDrawing.Save (); }
      private void OnExit_Click (object sender, RoutedEventArgs e) => Close ();
      #endregion
      #endregion

      #region CommandBinding --------------------------------------------------------------------------------------------
      private void CanExecute_Undo (object sender, CanExecuteRoutedEventArgs e) {
         mDrawing.CanUndo (sender, e);
         mUndo.IsEnabled = e.CanExecute;
      }
      private void CanExecute_Redo (object sender, CanExecuteRoutedEventArgs e) {
         mDrawing.CanRedo (sender, e);
         mRedo.IsEnabled = e.CanExecute;
      }
      #endregion

      #region Fields ----------------------------------------------------------------------------------------------------
      readonly DrawingEditor mDrawing = new ();
      public static RoutedCommand Undo = new ();
      public static RoutedCommand Redo = new ();
      #endregion
   }
}
