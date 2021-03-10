using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;

namespace PKUserTools.ExportTable
{
	public class PKTable:UtilityClass
	{
		List<TablePage> pages;


		uint startrow = 1;
		uint startcolumn = 1;
		
		//нужно знать, из какой BlockTableRecord записали таблицу и писать туда в будущем текст
		ObjectId owner;
		public ObjectId Owner
		{
			get
			{
				if(owner.IsNull) Tweet("Не определено место хранения таблицы. Необходимо распознать хотя бы одну страницу");
				return owner;
			}
		}
		//стиль текста
		public MText Format
		{
			get
			{
				return pages[0].format;
			}
		}
		
		public PKTable()
		{
			Initialise();
		}
		void Initialise()
		{
			pages = new List<TablePage>();
		}

		/// <summary>
		/// Добавление в таблицу страницы (части таблицы с чертежа)
		/// </summary>
		/// <param name="data">Объекты, которые будут распознаны в часть таблицы</param>
		public void AddPage(List<Entity> data)
		{
			var tables=data.OfType<Table>().ToList();
			Console.WriteLine("Количество таблиц в выделении"+tables.Count);
			if(tables.Count!=0)//сделано чтобы выделить сразу все объемы - если они сделаны из таблиц по страницам, все сразу распознаются
			{
				//сортируем по возрастанию по Х
				tables.Sort(Comparer<Table>.Create((x,y)=>x.Position.X.CompareTo(y.Position.X)));
				
				bool first=true;
				foreach(var t in tables)
				{
					pages.Add(new TablePage(t,first));
					first=false;
				}
			}
			else
			{
				Console.WriteLine("Распознаем разбитую таблицу");
				pages.Add(new TablePage(data));
				if(owner.IsNull)
				{
					owner=data[0].OwnerId;
					using(var th=new TransactionHelper())
					{
						BlockTableRecord btr=th.ReadObject(owner) as BlockTableRecord;
						Tweet("\nТаблица находится в пространстве "+ btr.Name);
					}
				}
			}

		}

		XL.Application excelApp;
		XL.Workbook workBook;
		XL.Worksheet workSheet;//при желании можно и на разные листы

		public void Write()
		{
			// Создаём экземпляр нашего приложения
			excelApp = new XL.Application();
			workBook = excelApp.Workbooks.Add();
			workSheet = (XL.Worksheet)(workBook.Worksheets[1]);

			//для каждой страницы пишем эксель
			for (int i = 0; i < pages.Count; i++)
			{
				pages[i].PageWrite(ref workSheet, startrow, startcolumn);

				startrow += Convert.ToUInt32(pages[i].Hcount); //размещаем друг под другом, но почему бы и не сделать потом смещение вбок или настраиваемое пользователем

			}

			workSheet.Columns.AutoFit();
			workSheet.Rows.AutoFit();



			// Открываем созданный excel-файл
			excelApp.Visible = true;
			excelApp.UserControl = true;

		}
		
		public List<TextCell> GetRow(int adress)
		{
			int pagenumber=pageNumber(ref adress);
			if(pagenumber==-1)return null;
			return pages[pagenumber].GetRow(adress);
		}
		
		public Rectangle3d CellCoords(int row,int column)
		{
			int pagenumber=pageNumber(ref row);
			if(pagenumber==-1)return new Rectangle3d();//нельзя нулл, ну и пох
			return pages[pagenumber].CellCoords(row,column);
		}
		public void WriteCell(int row, int column, string content)
		{
			int pagenumber=pageNumber(ref row);
			if(pagenumber!=-1)
			{
				pages[pagenumber].WriteCell(row,column,content,owner);
			}
		}
		int pageNumber(ref int row)
		{
			int pagenumber=0;
			while(row>=pages[pagenumber].Hcount)
			{
				if(++pagenumber>pages.Count)
				{
					Tweet("\nНе найден номер строки. Номер больше, чем есть в таблице.");
					return -1;
				}
				row-=pages[pagenumber-1].Hcount;
			}
			
			return pagenumber;
		}
		public int RowCount
		{
			get
			{
				int sum=0;
				foreach(TablePage pg in pages)
				{
					sum+=pg.Hcount;
				}
				return sum;
			}
		}
		public void ResetWidth(int column,double heigth)
		{
			foreach(var p in pages)
			{
				p.ResetWidth(column,heigth,1,p.Hcount-1);
			}
		}
        public Table TableFromlastPage()
        {
            return pages.Last().MakeACTable();
        }
	}
}
