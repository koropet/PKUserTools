/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 29.08.2018
 * Время: 17:57
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;
using PKUserTools.Commands.ItemInput;
using System.Windows.Forms;

using System.Diagnostics;

namespace PKUserTools.ExportTable
{
	/// <summary>
	/// Производит распознавание спецификации, дополняет таблицу и составляет ведомость расхода стали
	/// </summary>
	public class TableTranslator : UtilityClass
	{
		
		
		//сделан на случай, когда в чертеже есть спецификация, но надо немного ее поменять и пересчитать
		

		PKTable table;

		
		static List<Sortament> sortaments;

		Sortament cur_sort;
		string cur_mark;
		
		List<SortamentItem> items;

		public TableTranslator(PKTable table)
		{
			this.table = table;
			
			try
			{
				Recognize();
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
			
			
		}
		void Recognize()
		{
			var sw=new Stopwatch();
			sw.Start();
			
			var row = new List<TextCell>();
			int i;
			items=new List<SortamentItem>();
			
			
			//здесь мы можем задавать все доступные типы сортаментов, которые предполагаем увидеть в спецификации
			//UPD как идея - пройти по таблице и насти все госты
			if(sortaments==null)
			{
				sortaments=new List<Sortament>()
				{
					Sortament.A240(),
					Sortament.A400(),
					Sortament.A500(),
					Sortament.Pipe10704(),
					Sortament.Pipe8732(),
					Sortament.Gost103(),
					Sortament.A240new(),
					Sortament.A400new(),
					Sortament.A500new(),
				};
			}
			

			try
			{
				int trc=table.RowCount;
				for (i = 0; i < trc; i++)
				{
					row = table.GetRow(i);
					if (row == null)
					{
						Tweet("\nТаблица пустая");
						break;
					}
					
					string n=row[1].GetCellContent();
					bool sort_row=false;
					
					foreach(var st in sortaments)
					{
						if(st.Recognized(n))
						{
							cur_sort=st;
							cur_mark=st.Mark(n);
							sort_row=true;
						}
					}
					if(sort_row) continue;
					else if(Regex.Match(n,@"(L=|Lср=|Lcp=|Lобщ=)").Success)
					{
						int cnt=0;
						if (!(int.TryParse(row[2].GetCellContent(), out cnt)))
						{
							if(row[2].GetCellContent().Contains("-"))
							{
								cnt=1;
							}
							else
								Tweet("\nНе удалось прочитать количество");
						}
						
						var it=new SortamentItem()
						{
							shape=PKUserTools.Commands.ItemInput.Shape.FromString(n),
							sortament=cur_sort,
							mark=cur_mark,
							Name=row[0].GetCellContent(),
							Count=cnt,
						};
						it.Calculate();
						items.Add(it);
						table.WriteCell(i, 3, it.MassString);
					}
				}
				try
				{
					table.ResetWidth(3,8);
				}
				catch(Exception)
				{
					Tweet("Не могу подогнать высоту столбцов. Для разбитых таблиц это пока не реализовано.");
				}
				var groups=items.GroupBy(it=>it.mark+" "+it.sortament.GOST);
				var classes=groups.GroupBy(g=>g.First().sortament);
				
				string cap="",sum="",t="\t",diam="∅";
				
				foreach(var c in classes)
				{
					foreach(var g in c)
					{
						cap+=diam+g.First().mark+t;
						sum+=string.Format("{0:F1}",Math.Round(g.Sum(it=>it.AllMass)/0.1)*0.1).Replace('.',',')+t;
					}
					cap+="Итого" + t;
					sum+=string.Format("{0:F1}",c.Sum(g=>(Math.Round(g.Sum(it=>it.AllMass)/0.1))*0.1)).Replace('.',',')+t;
				}
				cap+="Всего" + "\n";
				sum+=string.Format("{0:F1}",classes.Sum(c=>c.Sum(g=>(Math.Round(g.Sum(it=>it.AllMass)/0.1))*0.1))).Replace('.',',');
				
				Clipboard.SetText(cap+sum);
				
				Tweet("\nОткройте Excel и вставьте в свободные ячейки из буфера обмена для получения ведомости расхода стали");
				sw.Stop();
				Tweet("Затрачено времени на работу " + sw.ElapsedMilliseconds + "миллисек");
				
			}
			catch(Exception ex)
			{
				Tweet(ex);
			}
		}
	}
	
	public class TableTranslator_fromtable :UtilityClass
	{
		
	}
}
