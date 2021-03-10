/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 25.10.2018
 * Время: 15:20
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Diagnostics;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using App=Autodesk.AutoCAD.ApplicationServices.Application;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;
using PKUserTools.Commands.ItemInput;
using System.Windows.Forms;


namespace PKUserTools.Utilities
{
	/// <summary>
	/// Description of TableImport.
	/// </summary>
	public static class TableImport
	{
		const string TestFile=@"D:\!Users\PKorobkin\Documents\Проекты\КЖЛ\тест ВОР.xlsx";
		
		static object[,] data;
		
		static int endrow;
		
		static List<XL.XlHAlign> align;
		
		/// <summary>
		/// Читает первый лист из файла
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static object[,] Read(string file)
		{
			var xlapp= new XL.Application();
			var xlwb=xlapp.Workbooks.Open(file);
			var sheet=(XL.Worksheet)xlwb.Worksheets[1];
			
			style=GetTextStyle();
			
			var data= (object[,])((XL.Range)(sheet.Range["A1",sheet.Cells.SpecialCells(XL.XlCellType.xlCellTypeLastCell)])).Value;
			
			bool flag=false;
			for(int i=data.GetLength(0);i>1;i--)
			{
				for(int j=1;j<data.GetLength(1);j++)
				{
					if(data[i,j]!=null) flag=true;
				}
				if(flag)
				{
					endrow=i;
					break;
				}
			}
			
			var range=(XL.Range)(sheet.Range[sheet.Cells[1,1],sheet.Cells[endrow,5]]);
			
			align=new List<XL.XlHAlign>();
			
			for(int i=1;i<=endrow;i++)
			{
				var cell=(XL.Range)sheet.Cells[i,2];
				var al=(XL.XlHAlign)cell.HorizontalAlignment;
				align.Add(al);
				Console.Write(al);
			}
			
			xlwb.Close(false,0,0);
			xlapp.Quit();
			
			Marshal.ReleaseComObject(sheet);
			Marshal.ReleaseComObject(xlwb);
			Marshal.ReleaseComObject(xlapp);
			
			return data;
		}
		
		
		
		static void DrawPage(Page pg,Point3d offset,bool cap=true)
		{
			
			//TODO: надо как-то понятнее организовать индексы, а то бардак дикий
			
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			
			var drawdata=new List<Entity>();
			
			int rows=pg.RowCount+1;
			int lines=pg.RowCount+2;
			
			var hls=hStyles(lines);
			var vls=vStyles(6);
			
			var column_coords =new double[] {0,15,120,135,155,185};
			var rc=RowCoords(lines);
			
			for(int line=0;line<lines;line++)
			{
				double y=-rc[line];
				double x=column_coords[5];
				
				var h=new Line(new Point3d(offset.X,offset.Y+y,0),new Point3d(offset.X+x,offset.Y+y,0));
				
				h.SetPropertiesFrom(hls[line]);
				
				drawdata.Add(h);
			}
			for(int i=0;i<=5;i++)
			{
				double x=column_coords[i];
				double y=-rc.Last();
				var v=new Line(new Point3d(offset.X+x,offset.Y,0),new Point3d(offset.X+x,offset.Y+y,0));
				v.SetPropertiesFrom(vls[i]);
				
				drawdata.Add(v);
			}
			
			//текст
			for(int i=1;i<=5;i++)
			{
				for(int j=pg.StartRow;j<=pg.EndRow;j++)
				{
					var content=data[j,i];
					
					var al=i==2?XL.XlHAlign.xlHAlignCenter:align[j];
					
					int r=j-pg.StartRow+1;
					
					if(content!=null)
					{
						var content_string=content.ToString();
						if(content_string!="")
						{
							var rect=new Rectangle3d(
								upperLeft: new Point3d(offset.X+column_coords[i-1],offset.Y-rc[r],0),
								lowerRight: new Point3d(offset.X+column_coords[i],offset.Y-rc[r+1],0),
								upperRight: new Point3d(offset.X+column_coords[i],offset.Y-rc[r],0),
								lowerLeft: new Point3d(offset.X+column_coords[i-1],offset.Y-rc[r+1],0));
							
							var mt=PrepareText(rect,content_string,al);
							drawdata.Add(mt);
						}
					}
				}
			}
			//нарисовать шапку
			if(cap)
			{
				for(int i=1;i<=5;i++)
				{
					int j=1;
					
					var al=i==2?XL.XlHAlign.xlHAlignCenter:align[j];
					
					var content=data[j,i];
					if(content!=null)
					{
						var content_string=content.ToString();
						if(content_string!="")
						{
							var rect=new Rectangle3d(
								upperLeft: new Point3d(offset.X+column_coords[i-1],offset.Y-rc[0],0),
								lowerRight: new Point3d(offset.X+column_coords[i],offset.Y-rc[1],0),
								upperRight: new Point3d(offset.X+column_coords[i],offset.Y-rc[0],0),
								lowerLeft: new Point3d(offset.X+column_coords[i-1],offset.Y-rc[1],0));
							
							var mt=PrepareText(rect,content_string,al);
							drawdata.Add(mt);
						}
					}
					
				}
			}
			
			
			
			using(var th=new TransactionHelper())
			{
				th.WriteObjects(drawdata);
			}
		}
		
		/// <summary>
		/// Координаты таблицы
		/// </summary>
		/// <param name="row_count"></param>
		/// <returns></returns>
		static double[] RowCoords(int row_count)
		{
			if(row_count<1) return null;//зачем биз шапки ходищь ээ
			var arr=new double[row_count];
			arr[0]=0;
			for(int i=1;i<row_count;i++)
			{
				arr[i]=i*8+7;
			}
			return arr;
		}
		
		static MText PrepareText(Rectangle3d cell, string content, XL.XlHAlign alignment)
		{
			var mt=new MText();
			mt.SetDatabaseDefaults();

			mt.Contents = content;
			mt.Layer="ПК_С_Тексты_3.0";
			mt.TextStyleId=style.ObjectId;

			mt.Width = cell.LowerRight.X - cell.LowerLeft.X;
			mt.Height = cell.UpperLeft.Y - cell.LowerLeft.Y;
			
			switch(alignment)
			{
				case XL.XlHAlign.xlHAlignCenter:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(mt.Width / 2, mt.Height / 2, 0));
						mt.Attachment = AttachmentPoint.MiddleCenter;
						break;
					}
				case XL.XlHAlign.xlHAlignLeft:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(0, mt.Height / 2, 0));
						mt.Attachment = AttachmentPoint.MiddleLeft;
						break;
					}
				case XL.XlHAlign.xlHAlignRight:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(mt.Width, mt.Height / 2, 0));
						mt.Attachment = AttachmentPoint.MiddleRight;
						break;
					}
				default:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(mt.Width / 2, mt.Height / 2, 0));
						mt.Attachment = AttachmentPoint.MiddleCenter;
						break;
					}
			}

			

			if (mt.ActualWidth > mt.Width) mt.Contents = @"{\W0.6;" + content + @"}";
			return mt;
		}
		
		static Line BoldLine()
		{
			var ln=new Line();
			ln.SetDatabaseDefaults();
			ln.Color=Color.FromColorIndex(ColorMethod.ByColor,7);
			ln.Layer="ПК_С_Таблицы";
			ln.LineWeight=LineWeight.LineWeight050;
			return ln;
		}
		static Line ThinLine()
		{
			var ln=new Line();
			ln.SetDatabaseDefaults();
			ln.Color=Color.FromColorIndex(ColorMethod.ByColor,7);
			ln.Layer="ПК_С_Таблицы";
			ln.LineWeight=LineWeight.LineWeight025;
			return ln;
		}
		static TextStyleTableRecord GetTextStyle()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			using(var th=acCurDb.TransactionManager.StartTransaction())
			{
				var tst=th.GetObject(acCurDb.TextStyleTableId,OpenMode.ForRead) as TextStyleTable;
				var tstr=th.GetObject(tst["TNR_3.0"],OpenMode.ForRead) as TextStyleTableRecord;
				return tstr;
			}
		}
		
		static Line bl;
		static Line tl;
		static TextStyleTableRecord style;
		
		/// <summary>
		/// Таблица стилей линий строк
		/// </summary>
		/// <param name="count">количество линий включая границы</param>
		/// <returns></returns>
		static List<Line> hStyles(int count)
		{
			if(bl==null|tl==null) {bl=BoldLine();tl=ThinLine();}
			
			var list=new List<Line>(count) {bl,bl};
			for(int i=3;i<count;i++)
			{
				list.Add(tl);
			}
			list.Add(bl);
			
			return list;
		}
		
		/// <summary>
		/// Таблица стилей линий столбцов
		/// </summary>
		/// <param name="count">количество линий включая границы</param>
		/// <returns></returns>
		static List<Line> vStyles(int count)
		{
			if(bl==null|tl==null) {bl=BoldLine();tl=ThinLine();}
			
			var list=new List<Line>(count);
			for(int i=1;i<=count;i++)
			{
				list.Add(bl);
			}
			
			return list;
		}
		
		//для отладки
		public static void Sample()
		{
			data=Read(TestFile);
			
			var pg=new Page();
			
			pg.data=data;
			pg.StartRow=2;
			pg.EndRow=19;
			
			var offset=new Point3d(20,292,0);
			
			
			while(pg.EndRow<endrow)
			{
				DrawPage(pg,offset);
				
				pg.StartRow=pg.EndRow+1;
				pg.EndRow=pg.StartRow+30;
				offset=offset.Add(new Vector3d(210,0,0));
			}
		}
	}
	struct Page
	{
		public object[,] data;
		public int StartRow;
		public int EndRow;
		
		public int RowCount
		{
			get
			{
				return EndRow-StartRow+1;
			}
		}
	}
}
