using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Shape = DesignLib.Shape;

namespace DesignCraft;
class DrawingEditor {
   #region Constructor------------------------------------------------------------------------------------
   public DrawingEditor (List<Shape> entityList) { mEntityCollection = entityList; }
   #endregion

   #region Implementation---------------------------------------------------------------------------------
   public void Undo () {
      if (mEntityCollection.Count != 0) {
         int last = mEntityCollection.Count - 1;
         mUndoRedo.Push (mEntityCollection[last]);
         mEntityCollection.RemoveAt (last);
      }
   }

   /// <summary>Redo the last undone shape</summary>
   public void Redo () {
      if (mUndoRedo.Count != 0) {
         var shape = mUndoRedo.Pop ();
         mEntityCollection.Add (shape);
      }
   }

   // Determine whether undo or redo can be executed
   public void CanRedo (object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = mUndoRedo.Any ();
   public void CanUndo (object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = mEntityCollection.Any ();

   /// <summary>Clear the display</summary>
   public void Clear () {
      if (mEntityCollection.Count != 0) { mEntityCollection.Clear (); mUndoRedo.Clear (); }
   }
   /// <summary>Changes the colour of the current shape</summary>
   public void ColorChange () {
      var colour = new ColorDialog ();
      if (CurShape != null && colour.ShowDialog () == DialogResult.OK)
         CurShape.Brush = (colour.Color.A, colour.Color.R, colour.Color.G, colour.Color.B);
   }
   #endregion

   #region Properties-------------------------------------------------------------------------------------
   public Shape CurShape { get => mEntityCollection.Count > 0 ? mEntityCollection.Last () : null; }
   #endregion

   #region Private Field----------------------------------------------------------------------------------
   Stack<Shape> mUndoRedo = new (); // Stack for redo and undo operation
   List<Shape> mEntityCollection;
   #endregion
}