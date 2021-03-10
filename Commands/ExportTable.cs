using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;

namespace PKUserTools.Commands
{
	class ExportTable:FunctionClass
	{
		PKTable tbl;
		bool NextPage=false;
        bool TransformToAC = false;

		public override void Execute()
		{
			base.Execute();
			tbl = new PKTable();

			ReadTable();

            if (TransformToAC) return;

			tbl.Write();

		}
		
		public void ExecuteTranslator()
		{
			base.Execute();
			tbl = new PKTable();
			
			ReadTable();
			
			var TT=new TableTranslator(tbl);
			
		}
		void ReadTable()
		{
			do
			{
				var sset=Input.Objects(
					
					keywords: new string[]
					{
						"MOrepages", "неСКолько","TRansform","ПРеобразовать"
					},
					message: "Выберите объекты для распознавания таблицы или ",
					keywordinput: new SelectionTextInputEventHandler((s, e) =>
					                                                 {
					                                                 	switch(e.Input)
                                                                         {
                                                                             case "MOrepages":
                                                                                 {
                                                                                     NextPage = true;
                                                                                     break;
                                                                                 }
                                                                             case "TRansform":
                                                                                 {
                                                                                     TransformToAC = true;
                                                                                     break;
                                                                                 }
                                                                         }
					                                                 })
				);
				if (Input.StatusOK)
				{
					Tweet("Распознаем таблицу");
				}
				else if (Input.StatusCancel) return;
				else break;
				using(TransactionHelper th=new TransactionHelper())
				{
					tbl.AddPage(th.ReadObjects(sset));
                    if (TransformToAC)
                    {
                    	th.BlockTablerecord=(th.ReadObject(tbl.Owner) as BlockTableRecord).Name;
                    	
                        th.WriteObject(tbl.TableFromlastPage());
                        th.Erase(sset);
                    }
				}
			}
			while (NextPage);
		}
		
	}
}
