using System.Windows;
using System;
using System.Windows.Input;

namespace DesignCraft;
class PanWidget {
   #region Constructors ------------------------------------------------------------------------------------------------
   public PanWidget (UIElement eventSource, Action<Vector> onPan) {
      mOnPan = onPan;
      eventSource.MouseDown += (sender, e) => {
         if (e.ChangedButton == MouseButton.Middle) PanStart (e.GetPosition (eventSource));
      };
      eventSource.MouseUp += (sender, e) => {
         if (IsPanning) PanEnd (e.GetPosition (eventSource));
      };
      eventSource.MouseMove += (sender, e) => {
         if (IsPanning) PanMove (e.GetPosition (eventSource));
      };
      eventSource.MouseLeave += (sender, e) => {
         if (IsPanning) PanCancel ();
      };
   }
   #endregion

   #region Implementation ----------------------------------------------------------------------------------------------
   bool IsPanning => mPrevPt != null;

   void PanStart (Point pt) => mPrevPt = pt;

   void PanMove (Point pt) {
      mOnPan.Invoke (pt - mPrevPt!.Value);
      mPrevPt = pt;
   }

   void PanEnd (Point? pt) {
      if (pt.HasValue)
         PanMove (pt.Value);
      mPrevPt = null;
   }

   void PanCancel () => PanEnd (null);
   #endregion

   #region Private -----------------------------------------------------------------------------------------------------
   Point? mPrevPt;
   readonly Action<Vector> mOnPan;
   #endregion
}