using DesignCraft.Lib;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DesignCraft;
public class DrawingEditor {

   #region Implementation---------------------------------------------------------------------------------
   public void Undo () {
      if (Drawing.Count != 0) {
         int last = Drawing.Count - 1;
         mUndoRedo.Push (Drawing.Plines[last]);
         Drawing.Plines.RemoveAt (last);
         EntityCount = Drawing.Count;
      }
      Drawing.RedrawReq ();
   }

   public void Redo () {
      if (mUndoRedo.Count != 0) {
         var pLine = mUndoRedo.Pop ();
         Drawing.AddPline (pLine);
         EntityCount = Drawing.Count;
      }
      Drawing.RedrawReq ();
   }

   // Determine whether undo or redo can be executed
   public void CanRedo (object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = mUndoRedo.Any () && EntityCount == Drawing.Count; if (!e.CanExecute) mUndoRedo.Clear (); }
   public void CanUndo (object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Drawing.Plines.Any ();

   /// <summary>Clear the display</summary>
   public void Clear () {
      if (Drawing.Count != 0) { Drawing.Clear (); mUndoRedo.Clear (); Drawing.RedrawReq (); }
   }


   #endregion

   #region Properties-------------------------------------------------------------------------------------
   public int EntityCount { get => mCount; set => mCount = value; }
   public Drawing Drawing { get => mDrawing; set => mDrawing = value; }
   #endregion

   #region Private Field----------------------------------------------------------------------------------
   Stack<Pline> mUndoRedo = new (); // Stack for redo and undo operation
   Drawing mDrawing = new ();
   int mCount = 0;
   #endregion
}