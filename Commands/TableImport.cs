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
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;
using PKUserTools.Commands.ItemInput;
using PKUserTools.EditorInput;
using System.Windows.Forms;


namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of TableImport.
	/// </summary>
	public static class TableImport
	{
		const string TestFile = @"D:\!Users\PKorobkin\Documents\Проекты\КЖЛ\тест ВОР.xlsx";

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
			var xlapp = new XL.Application();
			var xlwb = xlapp.Workbooks.Open(file);
			var sheet = (XL.Worksheet)xlwb.Worksheets[1];
			object[,] data=null; //помни, что если что-то пошло не так, из функции вернется нулл
			try
			{
				style = GetTextStyle();
				Messaging.Tweet("достали стиль");
				data = (object[,])((XL.Range)(sheet.Range["A1", sheet.Cells.SpecialCells(XL.XlCellType.xlCellTypeLastCell)])).Value;
				Messaging.Tweet("вынули дата ");
				bool flag = false;
				for (int i = data.GetLength(0); i > 1; i--)
				{
					for (int j = 1; j < data.GetLength(1); j++)
					{
						Messaging.Tweet(data[i, j] + " " +i+ ":" + j);
						if (data[i, j] != null) flag = true;
					}
					if (flag)
					{
						endrow = i;
						break;
					}
				}

				var range = (XL.Range)(sheet.Range[sheet.Cells[1, 1], sheet.Cells[endrow, 5]]);
				Messaging.Tweet("вынули диапазон ");
				align = new List<XL.XlHAlign>();

				for (int i = 1; i <= endrow; i++)
				{
					var cell = (XL.Range)sheet.Cells[i, 2];
					var al = (XL.XlHAlign)cell.HorizontalAlignment;
					align.Add(al);
				}
				Messaging.Tweet("вынули выравнивание ");
				xlwb.Close(false, 0, 0);
				xlapp.Quit();
			}
			catch(Exception ex)
			{
				Messaging.Tweet(ex);
				Console.WriteLine(ex);
			}
			finally
			{
				Marshal.ReleaseComObject(sheet);
				Marshal.ReleaseComObject(xlwb);
				Marshal.ReleaseComObject(xlapp);
			}
			return data;
		}



		static void DrawPage(Page pg, Point3d offset, bool cap = true)
		{

			//TODO: надо как-то понятнее организовать индексы, а то бардак дикий

			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;


			var drawdata = new List<Entity>();

			int rows = pg.RowCount + 1;
			int lines = pg.RowCount + 2;

			var hls = hStyles(lines);
			var vls = vStyles(6);

			var column_coords = new double[] { 0, 15, 120, 135, 155, 185 };
			var rc = RowCoords(lines);

			for (int line = 0; line < lines; line++)
			{
				double y = -rc[line];
				double x = column_coords[5];

				var h = new Line(new Point3d(offset.X, offset.Y + y, 0), new Point3d(offset.X + x, offset.Y + y, 0));

				h.SetPropertiesFrom(hls[line]);

				drawdata.Add(h);
			}
			for (int i = 0; i <= 5; i++)
			{
				double x = column_coords[i];
				double y = -rc.Last();
				var v = new Line(new Point3d(offset.X + x, offset.Y, 0), new Point3d(offset.X + x, offset.Y + y, 0));
				v.SetPropertiesFrom(vls[i]);

				drawdata.Add(v);
			}

			//текст
			for (int i = 1; i <= 5; i++)
			{
				for (int j = pg.StartRow; j <= pg.EndRow; j++)
				{
					object content = null;
					XL.XlHAlign al;

					if (j < align.Count)
					{
						content = data[j, i];
						al = i != 2 ? XL.XlHAlign.xlHAlignCenter : align[j];
					}
					else
					{
						content = "";
						al = XL.XlHAlign.xlHAlignCenter;
					}


					int r = j - pg.StartRow + 1;

					if (content != null)
					{
						var content_string = content.ToString();
						if (content_string != "")
						{
							var rect = new Rectangle3d(
								upperLeft: new Point3d(offset.X + column_coords[i - 1], offset.Y - rc[r], 0),
								lowerRight: new Point3d(offset.X + column_coords[i], offset.Y - rc[r + 1], 0),
								upperRight: new Point3d(offset.X + column_coords[i], offset.Y - rc[r], 0),
								lowerLeft: new Point3d(offset.X + column_coords[i - 1], offset.Y - rc[r + 1], 0));

							var mt = PrepareText(rect, content_string, al);
							drawdata.Add(mt);
						}
					}
				}
			}
			//нарисовать шапку
			if (cap)
			{
				for (int i = 1; i <= 5; i++)
				{
					int j = 1;

					var al = XL.XlHAlign.xlHAlignCenter;

					var content = data[j, i];
					if (content != null)
					{
						var content_string = content.ToString();
						if (content_string != "")
						{
							var rect = new Rectangle3d(
								upperLeft: new Point3d(offset.X + column_coords[i - 1], offset.Y - rc[0], 0),
								lowerRight: new Point3d(offset.X + column_coords[i], offset.Y - rc[1], 0),
								upperRight: new Point3d(offset.X + column_coords[i], offset.Y - rc[0], 0),
								lowerLeft: new Point3d(offset.X + column_coords[i - 1], offset.Y - rc[1], 0));

							var mt = PrepareText(rect, content_string, al);
							drawdata.Add(mt);
						}
					}

				}
			}



			using (var th = new TransactionHelper())
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
			if (row_count < 1) return null;//зачем биз шапки ходищь ээ
			var arr = new double[row_count];
			arr[0] = 0;
			for (int i = 1; i < row_count; i++)
			{
				arr[i] = i * 8 + 7;
			}
			return arr;
		}

		static MText PrepareText(Rectangle3d cell, string content, XL.XlHAlign alignment)
		{
			var mt = new MText();
			mt.SetDatabaseDefaults();

			mt.Contents = content;
			mt.Layer = "ПК_С_Тексты_3.0";
			mt.TextStyleId = style.ObjectId;

			mt.Width = cell.LowerRight.X - cell.LowerLeft.X;
			mt.Height = cell.UpperLeft.Y - cell.LowerLeft.Y;

			switch (alignment)
			{
				case XL.XlHAlign.xlHAlignCenter:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(mt.Width / 2, mt.Height / 2, 0));
						mt.Attachment = AttachmentPoint.MiddleCenter;
						break;
					}
				case XL.XlHAlign.xlHAlignLeft:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(1, mt.Height / 2, 0));
						mt.Attachment = AttachmentPoint.MiddleLeft;
						break;
					}
				case XL.XlHAlign.xlHAlignRight:
					{
						mt.Location = cell.LowerLeft.Add(new Vector3d(mt.Width - 1, mt.Height / 2, 0));
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
		static void DrawFirstPage()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;

			string block_name = "Рамка_Динамическая_TNR";
			string stamp_name = "Штамп для ведомостей_TNR";


			using (var th = acDoc.TransactionManager.StartTransaction())
			{
				var bt = th.GetObject(acCurDb.BlockTableId, OpenMode.ForWrite) as BlockTable;
				var modelspace = th.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

				var btr = th.GetObject(bt[block_name], OpenMode.ForWrite) as BlockTableRecord;

				var frame = new BlockReference(Point3d.Origin, btr.ObjectId);
				frame.Layer = "Z_Рамка_Штамп";




				var s_btr = th.GetObject(bt[stamp_name], OpenMode.ForWrite) as BlockTableRecord;

				var stamp = new BlockReference(new Point3d(205, 5, 0), s_btr.ObjectId);
				stamp.Layer = "Z_Рамка_Штамп";

				modelspace.AppendEntity(frame);
				modelspace.AppendEntity(stamp);

				th.AddNewlyCreatedDBObject(frame, true);
				th.AddNewlyCreatedDBObject(stamp, true);

				AppendAttributes(th, stamp, s_btr, StampAttributes());
				SetProperty(frame, "Видимость1", "Без штампа \"Согласовано\"");
				SetProperty(stamp, "Проверил/Рук.гр./Гл.Спец/ГИП", "2_Проверил-3_Рук.гр.-4_Гл.Спец");



				th.Commit();
			}
		}
		static void DrawOtherPages(int number)
		{
			Vector3d offset = new Vector3d((number - 1) * 210, 0, 0);

			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;

			string block_name = "Рамка_Динамическая_TNR";
			string stamp_name = "штамп_мал_лист_TNR";


			using (var th = acDoc.TransactionManager.StartTransaction())
			{
				var bt = th.GetObject(acCurDb.BlockTableId, OpenMode.ForWrite) as BlockTable;
				var modelspace = th.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

				var btr = th.GetObject(bt[block_name], OpenMode.ForWrite) as BlockTableRecord;

				var frame = new BlockReference(Point3d.Origin.Add(offset), btr.ObjectId);
				frame.Layer = "Z_Рамка_Штамп";




				var s_btr = th.GetObject(bt[stamp_name], OpenMode.ForWrite) as BlockTableRecord;

				var stamp = new BlockReference(new Point3d(205, 5, 0).Add(offset), s_btr.ObjectId);
				stamp.Layer = "Z_Рамка_Штамп";

				modelspace.AppendEntity(frame);
				modelspace.AppendEntity(stamp);

				th.AddNewlyCreatedDBObject(frame, true);
				th.AddNewlyCreatedDBObject(stamp, true);

				AppendAttributes(th, stamp, s_btr, StampAttributesSmall(number));
				SetProperty(frame, "Видимость1", "Без штампа \"Согласовано\""); ;



				th.Commit();
			}
		}
		static void SetProperty(BlockReference bref, string propname, object propvalue)
		{
			if (bref.IsDynamicBlock)
			{
				var props = bref.DynamicBlockReferencePropertyCollection;



				foreach (DynamicBlockReferenceProperty prop in props)
				{
					/*
					var values=prop.GetAllowedValues();
					
					Console.WriteLine(prop.PropertyName);
					
					foreach(var v in values)
					{
						Console.WriteLine(v);
					}*/
					if (prop.PropertyName == propname)
					{
						prop.Value = propvalue;
					}
				}
			}
			else
			{
				return;
			}
		}


		static void SetAttribute(BlockReference bref, string attname, string attvalue)
		{
			var attribs = bref.AttributeCollection;
			foreach (AttributeDefinition att in attribs)
			{
				if (att.Tag == attname) att.TextString = attvalue;
			}
		}
		static void AppendAttributes(Transaction th, BlockReference blockRef, BlockTableRecord blockDef, Dictionary<string, string> dict)
		{

			foreach (ObjectId id in blockDef)
			{
				DBObject obj = th.GetObject(id, OpenMode.ForRead);

				if (obj == null) continue;
				if (!(obj is AttributeDefinition)) continue;

				var attDef = obj as AttributeDefinition;

				if ((attDef != null) && (!attDef.Constant))
				{
					using (AttributeReference attRef = new AttributeReference())
					{
						attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
						if (dict.ContainsKey(attRef.Tag))
							attRef.TextString = dict[attRef.Tag];
						else
							attRef.TextString = " ";

						blockRef.AttributeCollection.AppendAttribute(attRef);

						th.AddNewlyCreatedDBObject(attRef, true);
					}
				}
			}
		}
		static Dictionary<string, string> StampAttributes()
		{
			var dict = new Dictionary<string, string>();

			dict.Add("ЛИСТ", "1");
			dict.Add("ЛИСТОВ", string.Format("{0:0}", pages));
			dict.Add("РАЗРАБ.", "Коробкин");
			dict.Add("РУК.ГР.", "Тюшевская");
			dict.Add("РУК.ГР", "Тюшевская");
			dict.Add("ГЛ.СПЕЦ.", "Фадеева");
			dict.Add("ГЛ.СПЕЦ", "Фадеева");
			dict.Add("Н.КОНТР.", "Привалова");
			dict.Add("НАЧ.ОТД.", "Рябков");
			dict.Add("ПРОВЕРИЛ", "Тюшевская");
			dict.Add("МАРКА", "-" + mark);

			return dict;
		}

		static Dictionary<string, string> StampAttributesSmall(int page)
		{
			var dict = new Dictionary<string, string>();

			dict.Add("ЛИСТ", string.Format("{0:0}", page));
			dict.Add("№ДОГОВОРАМАРКА", string.Format("{0}-{1}", dogovor,mark));

			return dict;
		}
		static Line BoldLine()
		{
			var ln = new Line();
			ln.SetDatabaseDefaults();
			ln.Color = Color.FromColorIndex(ColorMethod.ByBlock, 7);
			ln.Layer = "ПК_С_Таблицы";
			ln.LineWeight = LineWeight.LineWeight050;
			return ln;
		}
		static Line ThinLine()
		{
			var ln = new Line();
			ln.SetDatabaseDefaults();
			ln.Color = Color.FromColorIndex(ColorMethod.ByColor, 7);
			ln.Layer = "ПК_С_Таблицы";
			ln.LineWeight = LineWeight.LineWeight025;
			return ln;
		}
		static TextStyleTableRecord GetTextStyle()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;

			using (var th = acCurDb.TransactionManager.StartTransaction())
			{
				var tst = th.GetObject(acCurDb.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
				var tstr = th.GetObject(tst["TNR_3.0"], OpenMode.ForRead) as TextStyleTableRecord;
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
			if (bl == null | tl == null) { bl = BoldLine(); tl = ThinLine(); }
			
			//первая, вторая и последние горизонтальные линии жирные, остальные тонкие

			var list = new List<Line>(count) { bl, bl };
			for (int i = 3; i < count; i++)
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
			if (bl == null | tl == null) { bl = BoldLine(); tl = ThinLine(); }

			var list = new List<Line>(count);
			for (int i = 1; i <= count; i++)
			{
				list.Add(bl);
			}

			return list;
		}
		static int pages;
		static string dogovor;
		static string mark;
		//для отладки
		public static void Sample()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "Файлы Excel (*.xlsx)|*.xlsx";

			var res = dialog.ShowDialog();
			if (res != DialogResult.OK) return;
			
			dogovor = Input.Text("Введите номер договора"); if(Input.StatusBad) return;
			mark=Input.Text("Введите комплект"); if(Input.StatusBad) return;
			
			pages = 0;
			data = Read(dialog.FileName);
			if(data==null){ Messaging.Tweet("Ошибка чтения файла"); return;}

			var pg = new Page();

			pg.data = data;
			pg.StartRow = 2;
			pg.EndRow = 24;

			var offset = new Point3d(20, 292, 0);



			while (pg.StartRow < endrow)
			{
				DrawPage(pg, offset);

				if (++pages != 1) DrawOtherPages(pages);

				pg.StartRow = pg.EndRow + 1;
				pg.EndRow = pg.StartRow + 30;
				offset = offset.Add(new Vector3d(210, 0, 0));
			}

			pg.data = data;
			pg.StartRow = 2;
			pg.EndRow = 19;

			offset = new Point3d(20, 292, 0);

			DrawFirstPage();

			pages = 0;
			endrow = 0;

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
				return EndRow - StartRow + 1;
			}
		}
	}
}
