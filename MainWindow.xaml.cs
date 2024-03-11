using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;


namespace ScribblePad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      public MainWindow () {
         InitializeComponent ();
         mPen = new Pen (Brushes.White, 3);
         PopulateThicknessComboBox ();
         this.PreviewKeyDown += mDisplay_PreviewKeyDown;
      }

      #region Event Handlers-----------------------------------------------------------------------------------
      private void Window_Loaded (object sender, RoutedEventArgs e) => UpdateButtonStates ();

      private void mDisplay_PreviewKeyDown (object sender, System.Windows.Input.KeyEventArgs e) {
         if (e.Key == Key.Escape) { if (mShape != null && mShape is Line cLine && cLine.IsCLine == true) cLine.mCLines.RemoveAt (cLine.mCLines.Count - 1); } else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z) OnUndo_Clicked (mUndo, new RoutedEventArgs (ToggleButton.ClickEvent));
         else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y) OnRedo_Clicked (mRedo, new RoutedEventArgs (ToggleButton.ClickEvent));
         else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F4) Close ();
         Reset ();
         InvalidateVisual ();
      }

      void Reset () =>
         mIsDrawing = false;

      private void UpdateButtonStates () {
         bool notEmpty = mShapes.Any ();
         mEraser.IsEnabled = mUndo.IsEnabled = mClear.IsEnabled = notEmpty;
         mRedo.IsEnabled = mUndoRedo.Count > 0;
      }

      private void OnPick_Click (object sender, RoutedEventArgs e) {
         Deselect ();
         UncheckOtherToggleButtons (sender as ToggleButton);
      }

      #region Thickness------------------------------------------------------------------------
      private void OnThickness_SelectionChanged (object sender, SelectionChangedEventArgs e) {
         if (mThickness.SelectedItem != null) {
            mThickness.Text = mThickness.SelectedItem.ToString ();
            if (mSelectedShape != null) mSelectedShape.Pen.Thickness = double.Parse (mThickness.SelectedItem.ToString ());
            else mPen.Thickness = double.Parse (mThickness.Text);
         }
         InvalidateVisual ();
      }

      void PopulateThicknessComboBox () {
         var thicknessRange = Enumerable.Range (1, 100).ToList ();
         (mThickness.ItemsSource, mThickness.SelectedIndex) = (thicknessRange, 2);
      }
      #endregion

      #region User Prompts-------------------------------------------------------------------
      private void New_Click (object sender, RoutedEventArgs e) => CheckAndPrompt ();

      private void Exit_Click (object sender, RoutedEventArgs e) => CheckAndPrompt ();

      private void Window_Closed (object sender, System.ComponentModel.CancelEventArgs e) {
         CheckAndPrompt ();
         if (e.Cancel)
            e.Cancel = false;
      }

      /// <summary>Check prompt from user</summary>
      /// Checks if the document has been modified 
      /// Prompts the user to save changes if necessary.
      void CheckAndPrompt () {
         if (mIsModified) {
            MessageBoxResult messageBox = System.Windows.MessageBox.Show ("Do you want to save your changes?", "Scribble Pad",
                                                           MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            switch (messageBox) {
               case MessageBoxResult.Yes:
                  SaveAndClear ();
                  break;
               case MessageBoxResult.No:
                  ClearDrawing ();
                  break;
               case MessageBoxResult.Cancel:
                  return;
            }
         }
      }

      /// <summary>Save the drawing and clear the display</summary>
      void SaveAndClear () {
         Save ();
         ClearDrawing ();
         InvalidateVisual ();
      }
      #endregion

      private void UncheckOtherToggleButtons (ToggleButton clickedButton) {
         foreach (var child in mPanel.Children)
            if (child is ToggleButton toggleButton && toggleButton != clickedButton)
               toggleButton.IsChecked = false;
      }


      #region Select Shape-------------------------------------------------------------------------
      // Select the type of shape
      void OnPen_Clicked (object sender, RoutedEventArgs e) => SetShape (sender, new Scribble (mPen));
      void OnCLine_Clicked (object sender, RoutedEventArgs e) => SetShape (sender, new Line (mPen) { IsCLine = true });
      void OnLine_Clicked (object sender, RoutedEventArgs e) => SetShape (sender, new Line (mPen));
      void OnRectangle_Clicked (object sender, RoutedEventArgs e) => SetShape (sender, new Rectangle (mPen));
      void OnEllipse_Clicked (object sender, RoutedEventArgs e) => SetShape (sender, new Ellipse (mPen));
      private void OnEraser_Clicked (object sender, RoutedEventArgs e) {
         mPen.Brush = Brushes.Black;
         SetShape (sender, new Scribble (mPen));
      }
      private void SetShape (object sender, Shape shape) {
         Deselect ();
         shape.Pen = new Pen (mPen.Brush, mPen.Thickness);
         mShape = shape;
         UncheckOtherToggleButtons (sender as ToggleButton);
      }
      #endregion

      /// <summary>Changes the colour of the pen</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnColourChange_Clicked (object sender, RoutedEventArgs e) {
         var colour = new ColorDialog ();
         UncheckOtherToggleButtons (sender as ToggleButton);
         if (colour.ShowDialog () == System.Windows.Forms.DialogResult.OK)
            if (mSelectedShape != null)
               mSelectedShape.Pen.Brush = new SolidColorBrush (Color.FromArgb (colour.Color.A, colour.Color.R,
                                                                                                  colour.Color.G, colour.Color.B));
            else mPen.Brush = new SolidColorBrush (Color.FromArgb (colour.Color.A, colour.Color.R,
                                                                colour.Color.G, colour.Color.B));
         InvalidateVisual ();
      }

      /// <summary>Clear drawing on the screen</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void OnClear_Clicked (object sender, RoutedEventArgs e) {
         UncheckOtherToggleButtons (sender as ToggleButton);
         ClearDrawing ();
         UpdateButtonStates ();
         InvalidateVisual ();
      }

      /// <summary>Clear the scribbles list and undo and redo stack</summary>
      void ClearDrawing () {
         if (mShapes.Count != 0) {
            mShapes.Clear ();
            mUndoRedo.Clear ();
         }
      }
      #endregion

      #region Undo-Redo-------------------------------------------------------------------------
      /// <summary>Undo a scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void OnUndo_Clicked (object sender, RoutedEventArgs e) {
         if (mShapes.Count != 0) {
            int last = mShapes.Count - 1;
            mUndoRedo.Push (mShapes[last]);
            mShapes.RemoveAt (last);
            UpdateButtonStates ();
            InvalidateVisual ();
         }
      }

      /// <summary>Redo a scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void OnRedo_Clicked (object sender, RoutedEventArgs e) {
         if (mUndoRedo.Count != 0) {
            var shape = mUndoRedo.Pop ();
            mShapes.Add (shape);
            InvalidateVisual ();
            UpdateButtonStates ();
         }
      }
      #endregion

      #region Save-------------------------------------------------------------------------------
      private void SaveAs_Click (object sender, RoutedEventArgs e) {
         mIsSaved = false;
         Save ();
      }

      /// <summary>Save the drawing</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Save_Click (object sender, RoutedEventArgs e) {
         if (mIsModified)
            Save ();
      }
      void Save () {
         if (!mIsSaved) {
            SaveFileDialog save = new () {
               FileName = "scribble.bin",
               Filter = "Binary files (*.bin)|*.bin"
            };
            if (save.ShowDialog () == System.Windows.Forms.DialogResult.OK)
               SaveBinary (save.FileName);
            mIsSaved = true;
            mCurrentFilePath = save.FileName;
         } else
            SaveBinary (mCurrentFilePath);
         if (mIsSaved) mIsModified = false;
      }

      /// <summary>Save the drawing as a bin file</summary>
      /// <param name="path">Path of the bin file</param>
      void SaveBinary (string path) {
         if (mShapes.Count > 0)
            using (BinaryWriter bw = new (File.Open (path, FileMode.Create))) {
               bw.Write (mShapes.Count); // Total drawing count
               foreach (var shape in mShapes) {
                  bw.Write (shape.Type); // Type of shape
                  if (shape.Pen.Brush is SolidColorBrush sb) { // Brush color
                     bw.Write (sb.Color.A); bw.Write (sb.Color.R); bw.Write (sb.Color.G); bw.Write (sb.Color.B);
                  }
                  bw.Write (shape.Pen.Thickness); // Brush thickness
                  shape.SaveBinary (bw);
               }
            }
      }
      #endregion

      #region Load-------------------------------------------------------------------------------

      /// <summary>Load the drawing</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Open_Click (object sender, RoutedEventArgs e) {
         OpenFileDialog load = new () {
            FileName = "scribble.bin",
            Filter = "Binary files (*.bin)|*.bin"
         };
         if (load.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            string path = load.FileName;
            if (path != null) {
               LoadBinary (path);
               mIsSaved = true;
               mCurrentFilePath = path;
            }
         }
      }

      /// <summary>Load the drawing from a bin file</summary>
      /// <param name="filePath">Path of the bin file</param>
      void LoadBinary (string filePath) {
         ClearDrawing ();
         using (BinaryReader reader = new (File.Open (filePath, FileMode.Open))) {
            Shape shape = null;
            var sCount = reader.ReadInt32 (); // Total drawing count
            for (int i = 0; i < sCount; i++) {
               var (type, a, r, g, b, thickness) = (reader.ReadInt32 (), reader.ReadByte (), reader.ReadByte (),
                                                    reader.ReadByte (), reader.ReadByte (), reader.ReadDouble ());
               SolidColorBrush brush = new () { Color = Color.FromArgb (a, r, g, b) };
               var pen = new Pen (brush, thickness);
               switch (type) {
                  case 1: shape = new Scribble (pen); break;
                  case 2: shape = new Line (pen); break;
                  case 3: shape = new Rectangle (pen); break;
                  case 4: shape = new Ellipse (pen); break;
               }
               mShapes.Add (shape.LoadBinary (reader));
            }
         }
         UpdateButtonStates ();
         InvalidateVisual ();
      }
      #endregion

      private bool IsPointInsideShape (Point point, Shape shape) {
         if (shape is Line line) {
            if (line.IsCLine) {
               foreach (var lines in line.mCLines) {
                  var (start, end) = (lines.StartPoint, lines.EndPoint);
                  var (A, B, C) = (end.Y - start.Y, start.X - end.X, (start.Y * (end.X - start.X)) - ((end.Y - start.Y) * start.X));
                  double distance = Math.Abs ((A * point.X) + (B * point.Y) + C) / Math.Sqrt (A * A + B * B);
                  if (distance < mPen.Thickness / 2) return true;
               }
               return false;
            } else {
               var (start, end) = (line.StartPoint, line.EndPoint);
               var (A, B, C) = (end.Y - start.Y, start.X - end.X, (start.Y * (end.X - start.X)) - ((end.Y - start.Y) * start.X));
               double distance = Math.Abs ((A * point.X) + (B * point.Y) + C) / Math.Sqrt (A * A + B * B);
               return distance <= mPen.Thickness / 2;
            }
         } else if (shape is Rectangle rectangle) {
            Rect rect = new (rectangle.StartPoint, rectangle.EndPoint);
            return rect.Contains (point);
         } else if (shape is Ellipse ellipse) {
            var (xRadius, yRadius) = (Math.Abs (ellipse.EndPoint.X - ellipse.StartPoint.X) / 2, Math.Abs (ellipse.EndPoint.Y - ellipse.StartPoint.Y) / 2);
            Point center = new (ellipse.StartPoint.X + xRadius, ellipse.StartPoint.Y + yRadius);
            var (dx, dy) = ((point.X - center.X) / xRadius, (point.Y - center.Y) / yRadius);
            return dx * dx + dy * dy <= 1;
         }
         return false;
      }

      void Deselect () {
         if (mSelectedShape != null) {
            mSelectedShape.IsSelected = false;
            mSelectedShape = null;
         }
      }

      #region Drawing-------------------------------------------------------------------------------
      /// <summary>To start the drawing and collect the start point</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Drawing_MouseDown (object sender, MouseButtonEventArgs draw) {
         if (mShape != null) {
            var point = draw.GetPosition (this);
            if (mPickTool.IsChecked == true) {
               Deselect ();
               foreach (var shape in mShapes) {
                  if (shape is Scribble) continue;
                  else if (IsPointInsideShape (point, shape)) {
                     shape.IsSelected = true;
                     mSelectedShape = shape;
                     break;
                  } else shape.IsSelected = false;
               }
               InvalidateVisual ();
            } else if (draw.LeftButton == MouseButtonState.Pressed) {
               if (!mIsDrawing) {
                  var pen = new Pen (mPen.Brush, mPen.Thickness);
                  switch (mShape.Type) {
                     case 1: mShape = new Scribble (pen); break;
                     case 2: mShape = new Line (pen) { IsCLine = (mShape is Line line && line.IsCLine) }; break;
                     case 3: mShape = new Rectangle (pen); break;
                     case 4: mShape = new Ellipse (pen); break;
                  }
                  (mIsDrawing, mIsModified) = (true, true);
                  if (mUndoRedo.Count > 0) mUndoRedo.Clear ();
                  SetShape (sender, mShape);
                  if (mShape is Line cLine && cLine.IsCLine == true) cLine.mCLines.Add (new Line (mShape.Pen) { StartPoint = point });
                  else mShape.StartPoint = point;
                  mShapes.Add (mShape);
               } else {
                  if (mShape is Line cLine && cLine.IsCLine == true) {
                     cLine.mCLines.Last ().EndPoint = draw.GetPosition (this);
                     cLine.mCLines.Add (new Line (mShape.Pen) { StartPoint = cLine.mCLines.Last ().EndPoint });
                  } else {
                     AddPoints (draw);
                     Reset ();
                  }
               }
               UpdateButtonStates ();
            }
         }
      }

      /// <summary>Update state of drawing and collect current point</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Drawing_MouseMove (object sender, MouseEventArgs draw) {
         if (mIsDrawing)
            AddPoints (draw);
      }

      void AddPoints (MouseEventArgs draw) {
         Point currentPoint = draw.GetPosition (this);
         if (mShape is Line cLine && cLine.IsCLine == true) cLine.mCLines.Last ().EndPoint = currentPoint;
         else mShape.EndPoint = currentPoint;
         InvalidateVisual ();
      }

      /// <summary>To render the drawings on the display</summary>
      /// <param name="drawingContext">Drawing context variable to render the drawing</param>
      protected override void OnRender (DrawingContext drawingContext) {
         foreach (var shape in mShapes)
            shape.Draw (drawingContext);
      }
      #endregion

      #region Private Data-------------------------------------------------------------------------------
      Pen mPen; // Pen with brush colour and thickness
      Stack<Shape> mUndoRedo = new (); // Stack to undo and redo shapes
      bool mIsDrawing = false, mIsSaved = false, mIsModified = false; // boolean variable to check if it is drawing,it is saved and if it is modified
      List<Shape> mShapes = new (); // List of shapes
      Shape mShape = null, mSelectedShape = null;
      string mCurrentFilePath = "";
      #endregion
   }
}
