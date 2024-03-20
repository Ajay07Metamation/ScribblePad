using Shapes;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Forms;
using Point = Shapes.Point;
using Shape = Shapes.Shape;
using System.Collections.Generic;
using Line = Shapes.Line;
using Rectangle = Shapes.Rectangle;
using System.IO;
using System.Windows.Input;
using System.Linq;

namespace SPA;
class DrawingEditor {

   #region Implementation---------------------------------------------------------------------------------

   #region Shape Selection ------------------------------------------------------------------------
   /// <summary>Sets the current shape</summary>
   /// <param name="sender"></param>
   public void SetShape (object sender) {
      (byte, byte, byte, byte) brush = (255, 255, 255, 255);
      if (mShape != null) brush = mShape.Brush;
      if (sender is ToggleButton toggleButton) int.TryParse (toggleButton.Tag?.ToString (), out mType);
      switch (mType) {
         case 1: mShape = new Scribbles (); break;
         case 2: mShape = new Line (); break;
         case 3: mShape = new Rectangle (); break;
         case 4: mShape = new Circle (); break;
      }
      (mShape.Brush, IsSet) = (brush, true);
   }

   /// <summary>Adds the shape to the collection</summary>
   public void AddShape () => mShapeCollection.Add (mShape);
   #endregion

   #region Draw -----------------------------------------------------------------------------------

   /// <summary>Draw all the shapes in the collection</summary>
   /// <param name="dc"></param>
   public void DrawShape (DrawingContext dc) {
      foreach (var shape in mShapeCollection)
         Draw (dc, shape);
   }

   /// <summary>Draw the shape</summary>
   /// <param name="dc"></param>
   /// <param name="shape">Shape to be drawn</param>
   void Draw (DrawingContext dc, Shape shape) {
      var (a, r, g, b) = shape.Brush;
      SolidColorBrush brush = new () { Color = Color.FromArgb (a, r, g, b) };
      switch (shape) {
         case Scribbles scribble:
            var points = scribble.mPoints;
            for (int i = 1; i < points.Count; i++)
               dc.DrawLine (new Pen (brush, 2), new (points[i - 1].X, points[i - 1].Y), new (points[i].X, points[i].Y));
            break;
         case Line line:
            dc.DrawLine (new Pen (brush, 2), new (line.StartPoint.X, line.StartPoint.Y), new (line.EndPoint.X, line.EndPoint.Y));
            break;
         case Rectangle rectangle:
            Rect rect = new (new System.Windows.Point (rectangle.StartPoint.X, rectangle.StartPoint.Y),
                             new System.Windows.Point (rectangle.EndPoint.X, rectangle.EndPoint.Y));
            dc.DrawRectangle (null, new Pen (brush, 2), rect);
            break;
         case Circle circle:
            var center = new System.Windows.Point (circle.StartPoint.X, circle.StartPoint.Y);
            var radius = Math.Sqrt (Math.Pow (center.X - circle.EndPoint.X, 2) + Math.Pow (center.Y - circle.EndPoint.Y, 2));
            dc.DrawEllipse (null, new Pen (brush, 2), center, radius, radius);
            break;
      }
   }
   /// <summary>Constructs the shape</summary>
   /// <param name="cursor"></param>
   public void ConstructShape (System.Windows.Point cursor) {
      if (mUndoRedo.Count > 0) mUndoRedo.Clear ();
      Point point = new (cursor.X, cursor.Y);
      if (mShape != null) {
         if (!IsDraw) mShape.StartPoint = point;
         else mShape.EndPoint = point;
      }
   }
   #endregion

   #region Undo Redo ------------------------------------------------------------------------------

   /// <summary>Undo the last drawn shape</summary>
   public void Undo () {
      if (mShapeCollection.Count != 0) {
         int last = mShapeCollection.Count - 1;
         mUndoRedo.Push (mShapeCollection[last]);
         mShapeCollection.RemoveAt (last);
      }
   }

   /// <summary>Redo the last undone shape</summary>
   public void Redo () {
      if (mUndoRedo.Count != 0) {
         var shape = mUndoRedo.Pop ();
         mShapeCollection.Add (shape);
      }
   }

   // Determine whether undo or redo can be executed
   public void CanRedo (object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = mUndoRedo.Any ();
   public void CanUndo (object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = mShapeCollection.Any ();
   #endregion

   #region SaveLoad -------------------------------------------------------------------------------

   /// <summary>Save the drawing</summary>
   public void Save () {
      if (!IsSaved) {
         SaveFileDialog save = new () {
            FileName = "scribble.bin",
            Filter = "Binary files (*.bin)|*.bin"
         };
         if (save.ShowDialog () == DialogResult.OK)
            Save (save.FileName);
         (IsSaved, mCurrentFilePath) = (true, save.FileName);
      } else if (IsModified) Save (mCurrentFilePath);
      if (IsSaved) IsModified = false;
   }
   void Save (string path) {
      if (mShapeCollection.Count > 0)
         using (BinaryWriter bw = new (File.Open (path, FileMode.Create))) {
            bw.Write (mShapeCollection.Count); // Total drawing count
            foreach (var shape in mShapeCollection) {
               bw.Write (shape.Type); // Type of shape
               bw.Write (shape.Brush.Item1); bw.Write (shape.Brush.Item2); bw.Write (shape.Brush.Item3); bw.Write (shape.Brush.Item4);
               shape.Save (bw);
            }
         }
   }

   /// <summary>Load the drawing</summary>
   public void Load () {
      OpenFileDialog load = new () {
         FileName = "scribble.bin",
         Filter = "Binary files (*.bin)|*.bin"
      };
      if (load.ShowDialog () == DialogResult.OK) {
         string path = load.FileName;
         if (path != null) {
            Load (path);
            (IsSaved, mCurrentFilePath) = (true, path);
         }
      }
   }
   void Load (string filePath) {
      Clear ();
      using (BinaryReader reader = new (File.Open (filePath, FileMode.Open))) {
         Shape shape = null;
         var sCount = reader.ReadInt32 (); // Total drawing count
         for (int i = 0; i < sCount; i++) {
            var type = reader.ReadInt32 ();
            switch (type) {
               case 1: shape = new Scribbles (); break;
               case 2: shape = new Line (); break;
               case 3: shape = new Rectangle (); break;
               case 4: shape = new Circle (); break;
            }
            mShapeCollection.Add (shape.Load (reader));
         }
      }
   }
   #endregion

   #region Tools ----------------------------------------------------------------------------------
   /// <summary>Keyboard shortcuts for commands</summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   public void Shortcuts (object sender, System.Windows.Input.KeyEventArgs e) {
      if (Keyboard.Modifiers == ModifierKeys.Control)
         switch (e.Key) {
            case Key.Z: Undo (); break;
            case Key.Y: Redo (); break;
            case Key.N: CheckAndPrompt (); Clear (); mCurrentFilePath = ""; break;
            case Key.S: Save (); break;
            case Key.O: Load (); break;
         } else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F4) {
         Window window = new ();
         window.Close ();
      }
   }

   // Save and clear the display
   void SaveAndClear () { Save (); Clear (); }

   /// <summary>Clear the display</summary>
   public void Clear () {
      if (mShapeCollection.Count != 0) { mShapeCollection.Clear (); mUndoRedo.Clear (); }
   }

   /// <summary>Provides user prompt</summary>
   /// Provides user prompt to save changes made to the drawing
   /// <returns></returns>
   public bool CheckAndPrompt () {
      bool isCanceled = false;
      if (IsModified) {
         MessageBoxResult messageBox = System.Windows.MessageBox.Show ("Do you want to save your changes?", "Scribble Pad",
                                                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
         switch (messageBox) {
            case MessageBoxResult.Yes: SaveAndClear (); break;
            case MessageBoxResult.No: Clear (); break;
            case MessageBoxResult.Cancel: isCanceled = true; break;
         }
      }
      return isCanceled;
   }

   /// <summary>Changes the colour of the current shape</summary>
   public void ColorChange () {
      var colour = new ColorDialog ();
      if (mShape != null && colour.ShowDialog () == DialogResult.OK)
         mShape.Brush = (colour.Color.A, colour.Color.R, colour.Color.G, colour.Color.B);
   }
   #endregion
   #endregion

   #region Properties-------------------------------------------------------------------------------------
   public bool IsDraw { get => mIsDraw; set => mIsDraw = value; } // Indicates whether the user is currently drawing
   public bool IsSet { get; set; } // Indicate if the current shape is set
   public bool IsSaved { get; set; } // Indicate if the drawing is saved
   public bool IsModified { get; set; } // Indicate if the drawing is modified 
   public Shape CurrentShape { get => mShape; }
   #endregion

   #region Private Field----------------------------------------------------------------------------------
   ObservableCollection<Shape> mShapeCollection = new (); // Collection of shapes
   Stack<Shape> mUndoRedo = new (); // Stack for redo and undo operation
   Shape mShape; // Current shape
   bool mIsDraw = false; // Boolean variable to indicate whether user is drawing
   int mType = 1; // Type of shape
   string mCurrentFilePath = ""; // Current file path where the drawing is saved
   #endregion
}