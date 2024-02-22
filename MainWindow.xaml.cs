using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;


namespace Scribble {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      public MainWindow () {
         InitializeComponent ();
         mPen = new Pen (Brushes.White, 3);
      }

      #region Undo-Redo-------------------------------------------------------------------------
      /// <summary>Undo a scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Undo_Click (object sender, RoutedEventArgs e) {
         if (mUndo.Count != 0) {
            mRedo.Push (mUndo.Pop ());
            mDrawing.RemoveAt (mDrawing.Count - 1);
            InvalidateVisual ();
         }
      }

      /// <summary>Redo a scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Redo_Click (object sender, RoutedEventArgs e) {
         if (mRedo.Count != 0) {
            var data = mRedo.Pop ();
            mUndo.Push (data);
            mDrawing.Add (data);
            InvalidateVisual ();
         }
      }
      #endregion

      #region PenColour---------------------------------------------------------------------------
      /// <summary>Changes the colour of the pen</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Pen_Click (object sender, RoutedEventArgs e) {
         mIsDrawing = true;
         var colour = new ColorDialog ();
         if (colour.ShowDialog () == System.Windows.Forms.DialogResult.OK)
            mPen.Brush = new SolidColorBrush (Color.FromArgb (colour.Color.A, colour.Color.R,
                                                              colour.Color.G, colour.Color.B));
      }
      #endregion

      #region Eraser-------------------------------------------------------------------------------
      /// <summary>Choose the eraser tool</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Eraser_Click (object sender, RoutedEventArgs e) {
         mPen.Brush = Brushes.Black;
         mIsDrawing = false;
      }
      #endregion

      #region Thickness-------------------------------------------------------------------------
      /// <summary>Change thickness of scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ThicknessSlider_ValueChanged (object sender, RoutedPropertyChangedEventArgs<double> e) {
         if (mPen != null) {
            mPen.Thickness = e.NewValue;
         }
         mIsDrawing = true;
      }
      #endregion

      #region Clear-------------------------------------------------------------------------
      /// <summary>Clear drawing on the screen</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Clear_Click (object sender, RoutedEventArgs e) {
         ClearDrawing ();
         InvalidateVisual ();
      }

      /// <summary>Clear the scribbles list and undo and redo stack</summary>
      void ClearDrawing () {
         mDrawing.Clear ();
         mUndo.Clear ();
         mRedo.Clear ();
      }
      #endregion

      #region Save-------------------------------------------------------------------------------
      /// <summary>Save the drawing</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Save_Click (object sender, RoutedEventArgs e) {
         SaveFileDialog save = new ();
         save.FileName = "scribble.txt";
         save.Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin";
         if (save.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            if (save.FilterIndex == 1) SaveText (save.FileName);
            else SaveBinary (save.FileName);
         }
      }

      /// <summary>Save the drawing as a bin file</summary>
      /// <param name="path">Path of the bin file</param>
      void SaveBinary (string path) {
         if (mDrawing.Count > 0)
            using (BinaryWriter tw = new (File.Open (path, FileMode.Create))) {
               tw.Write (mDrawing.Count); // Total drawing count
               foreach (var (brush, thickness, points) in mDrawing) {
                  if (brush is SolidColorBrush sb) { // Brush color
                     tw.Write (sb.Color.A);
                     tw.Write (sb.Color.R);
                     tw.Write (sb.Color.G);
                     tw.Write (sb.Color.B);
                  }
                  tw.Write (thickness); // Brush thickness
                  tw.Write (points.Count); // Total number of points in one scribble
                  foreach (var point in points) {
                     tw.Write (point.X); // X coordinate of point
                     tw.Write (point.Y); // Y coordinate of point
                  }
               }
            }
      }

      /// <summary>Save the drawing as a text file</summary>
      /// <param name="path">Path of the text file</param>
      void SaveText (string path) {
         if (mDrawing.Count > 0) {
            using (TextWriter tw = new StreamWriter (path, true)) {
               tw.WriteLine (mDrawing.Count); // Total drawing count
               foreach (var (brush, thickness, points) in mDrawing) {
                  tw.WriteLine (brush.ToString ()); // Brush color
                  tw.WriteLine (thickness); // Brush thickness
                  tw.WriteLine (points.Count); // Total number of points in one scribble
                  foreach (var point in points) {
                     tw.WriteLine (point.X); // X coordinate of point
                     tw.WriteLine (point.Y); // Y coordinate of point
                  }
               }
            }
         }
      }
      #endregion

      #region Load-------------------------------------------------------------------------------

      /// <summary>Load the drawing</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Open_Click (object sender, RoutedEventArgs e) {
         OpenFileDialog load = new ();
         load.Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin";
         if (load.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            string path = load.FileName;
            if (path != null) {
               if (load.FilterIndex == 1) LoadText (path);
               else LoadBinary (path);
            }
         }
      }

      /// <summary>Load the drawing from a bin file</summary>
      /// <param name="filePath">Path of the bin file</param>
      void LoadBinary (string filePath) {
         ClearDrawing ();
         using (BinaryReader reader = new (File.Open (filePath, FileMode.Open))) {
            var sCount = reader.ReadInt32 (); // Total drawing count
            for (int i = 0; i < sCount; i++) {
               SolidColorBrush brush = new ();
               double thickness;
               List<Point> points = new ();
               byte a = reader.ReadByte ();
               byte r = reader.ReadByte ();
               byte g = reader.ReadByte ();
               byte b = reader.ReadByte ();
               brush.Color = Color.FromArgb (a, r, g, b); // Brush color
               thickness = reader.ReadDouble (); // Pen thickness
               int pCount = reader.ReadInt32 (); // Total number of points in one scribble
               for (int j = 0; j < pCount; j++) {
                  (var x, var y) = (reader.ReadDouble (), reader.ReadDouble ()); // X & Y co-ordinate of point
                  Point p = new (x, y);
                  points.Add (p);
               }
               mDrawing.Add ((brush, thickness, points));
               mUndo.Push ((brush, thickness, points));
            }
         }
         InvalidateVisual ();
      }

      /// <summary>Load the drawing from a text file</summary>
      /// <param name="filePath">Path of the text file</param>
      void LoadText (string filePath) {
         ClearDrawing ();
         using (StreamReader reader = new (filePath)) {
            int.TryParse (reader.ReadLine (), out int dCount); // Total drawing count
            for (int i = 0; i < dCount; i++) {
               SolidColorBrush brush;
               List<Point> points = new ();
               string line = reader.ReadLine ();
               brush = new SolidColorBrush ((Color)ColorConverter.ConvertFromString (line)); // Brush color
               double.TryParse (reader.ReadLine (), out double thickness); // Pen thickness
               int.TryParse (reader.ReadLine (), out int pCount); // Total number of points in one scribble
               for (int j = 0; j < pCount; j++) {
                  double.TryParse (reader.ReadLine (), out double x); // X co-ordinate of point
                  double.TryParse (reader.ReadLine (), out double y); // Y co-ordinate of point
                  Point point = new Point (x, y);
                  points.Add (point);
               }
               mDrawing.Add ((brush, thickness, points));
               mUndo.Push ((brush, thickness, points));
            }
         }
         InvalidateVisual ();
      }
      #endregion

      #region Drawing-------------------------------------------------------------------------------
      /// <summary>To start the drawing and collect the start point</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Drawing_MouseDown (object sender, MouseButtonEventArgs draw) {
         Reset ();
         if (draw.LeftButton == MouseButtonState.Pressed) {
            if (mIsDrawing && mRedo.Count > 0) mRedo.Clear ();
            mStartPoint = draw.GetPosition (this);
            mPoints.Add (mStartPoint);
         }
      }

      /// <summary>Add drawing to the list when mouse is released</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Drawing_MouseUp (object sender, MouseButtonEventArgs draw) {
         if (draw.ButtonState == MouseButtonState.Released) {
            mIsDrawing = false;
            InvalidateVisual ();
         }
      }

      /// <summary>Update state of drawing and collect current point</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Drawing_MouseMove (object sender, MouseEventArgs draw) {
         if (draw.LeftButton == MouseButtonState.Pressed) {
            Point currentPoint = draw.GetPosition (this);
            mPoints.Add (currentPoint);
            InvalidateVisual ();
         }
      }

      /// <summary>Resets the list after each scribble</summary>
      void Reset () {
         mPoints = new List<Point> ();
         mDrawing.Add ((mPen.Brush, mPen.Thickness, mPoints));
         mUndo.Push ((mPen.Brush, mPen.Thickness, mPoints));
      }

      /// <summary>To render the points on the display</summary>
      /// <param name="drawingContext">Drawing context variable to draw to lines between each point</param>
      protected override void OnRender (DrawingContext drawingContext) {
         foreach (var (brush, thickness, points) in mDrawing) {
            for (int i = 1; i < points.Count; i++)
               drawingContext.DrawLine (new Pen (brush, thickness), points[i - 1], points[i]);
         }
      }
      #endregion

      #region Private Data-------------------------------------------------------------------------------
      Point mStartPoint; // Start point of the scribble
      Pen mPen; // Pen with brush colour and thickness
      List<(Brush, double, List<Point>)> mDrawing = new (); // List to store all scribbles
      List<Point> mPoints = new (); // List of scribbled points
      Stack<(Brush, double, List<Point>)> mUndo = new (), mRedo = new (); // Stack to undo and redo scribbles
      bool mIsDrawing = false; // boolean variable to check if it is drawing
      #endregion
   }
}
