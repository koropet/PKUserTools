/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 01.02.2019
 * Время: 16:00
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using XL = Microsoft.Office.Interop.Excel;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;

using System.Windows.Forms;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.Utilities
{
    /// <summary>
    /// Description of LayoutTools.
    /// </summary>
    public class LayoutTools
    {
        public LayoutTools()
        {
        }

        public static void VORLayouts()
        {
            var lm = LayoutManager.Current;

            var acDoc = App.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;
            var acEd = acDoc.Editor;

            int pages = Input.Integer("Введите количество страниц"); if (Input.StatusBad) return;
            Point3d basePoint = Input.Point("Укажите левый нижний угол рулона ВОР"); if (Input.StatusBad) return;


            var layouts = new List<string>();
            for (int i = 1; i <= pages; i++)
            {
                string name = string.Format("ВОР_Лист_{0}", i);

                
                lm.CloneLayout("ШАБЛОН", name,i);

                layouts.Add(name);
            }
            using (var acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                var blockTable = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                for (int i = 1; i <= pages; i++)
                {
                    var l = layouts[i - 1];
                    var current = lm.CurrentLayout;
                    var btr_name = Utilities.btr_from_layout(l);


                    var btr = acTrans.GetObject(blockTable[btr_name], OpenMode.ForWrite) as BlockTableRecord;

                    var view_port = new Viewport();

                    view_port.ViewCenter = new Point2d(210 * (i - 0.5) + basePoint.X, 297 / 2d + basePoint.Y);
                    view_port.CenterPoint = new Point3d(210 * 0.5, 297 / 2, 0);
                    view_port.Width = 208;
                    view_port.Height = 295;
                    view_port.CustomScale = 1;


                    btr.AppendEntity(view_port);
                    acTrans.AddNewlyCreatedDBObject(view_port, true);

                    
                }
                acTrans.Commit();
            }


        }
    }
}
