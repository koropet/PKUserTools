/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 26.09.2018
 * Время: 17:16
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
using System.Data;
using System.Text;
using System.Diagnostics;

using XL= Microsoft.Office.Interop.Excel;

namespace PKUserTools.Commands.ItemInput
{
	/// <summary>
	/// Описывает марку элемента, типоразмер и гост
	/// </summary>
	public class Sortament
	{
		
		public string GOST {get;private set;}
		public string aclass {get;private set;}
		Func<string,string> mark_pattern;
		Dictionary<string,double> mark_to_mass;
		
		public Sortament(string GOST,string aclass, Func<string,string> mark_pattern, string[] marks, double[] masses)
		{
			mark_to_mass=new Dictionary<string, double>();
			
			if(marks.Count()!=masses.Count()) throw new ArgumentException("Не соответствуют диаметры и массы");
			
			for(int i=0;i<masses.GetLength(0);i++)
			{
				mark_to_mass.Add(marks[i],masses[i]);
			}
			this.GOST=GOST;
			this.aclass=aclass;
			this.mark_pattern=mark_pattern;
		}
		
		public double Mass(string inp)
		{
			return MassFromMark(mark_pattern(inp));
		}
		public double MassFromMark(string mark)
		{
			if(mark_to_mass.ContainsKey(mark))
				return mark_to_mass[mark];
			else return 0;
		}
		public string Mark(string inp)
		{
			return mark_pattern(inp);
		}
		/// <summary>
		/// Проверяем, для этого ли сортамента строка в спецификации
		/// </summary>
		/// <param name="inp"></param>
		/// <returns></returns>
		public bool Recognized(string inp)
		{
			if(inp.Contains(Regex.Match(GOST,@"\d+-\d+").Value)&&inp.Contains(aclass))
			{
				//TODO надо попробовать подгружать именно здесь
				return true;
			}
			return false;
		}
		public static Sortament A500()
		{
			return new Sortament("ГОСТ Р 52544-2006","А500С",FindDiameter , new string[]
			                     {"4", "5", "6", "8", "10", "12", "14", "16", "18", "20", "22", "25", "28", "32", "36", "40"},
			                     new double[]
			                     {0.099, 0.154, 0.222, 0.395, 0.616, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.853, 4.834, 6.313, 7.99, 9.865 });
		}
		public static Sortament A240new()
		{
			return new Sortament("ГОСТ 34028-2016","А240",FindDiameter , new string[]
			                     {"4", "5", "6", "8", "10", "12", "14", "16", "18", "20", "22", "25", "28", "32", "36", "40"},
			                     new double[]
			                     {0.099, 0.154, 0.222, 0.395, 0.616, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.853, 4.834, 6.313, 7.99, 9.865 });
		}
		public static Sortament A400new()
		{
			return new Sortament("ГОСТ 34028-2016","А400",FindDiameter , new string[]
			                     {"4", "5", "6", "8", "10", "12", "14", "16", "18", "20", "22", "25", "28", "32", "36", "40"},
			                     new double[]
			                     {0.099, 0.154, 0.222, 0.395, 0.616, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.853, 4.834, 6.313, 7.99, 9.865 });
		}
		public static Sortament A500new()
		{
			return new Sortament("ГОСТ 34028-2016","А500С",FindDiameter , new string[]
			                     {"4", "5", "6", "8", "10", "12", "14", "16", "18", "20", "22", "25", "28", "32", "36", "40"},
			                     new double[]
			                     {0.099, 0.154, 0.222, 0.395, 0.616, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.853, 4.834, 6.313, 7.99, 9.865 });
		}
		public static Sortament A240()
		{
			return new Sortament("ГОСТ 5781-82","А240", FindDiameter, new string[]
			                     {"4", "5", "6", "8", "10", "12", "14", "16", "18", "20", "22", "25", "28", "32", "36", "40"},
			                     new double[]
			                     {0.092, 0.144, 0.222, 0.395, 0.617, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.84, 4.83, 6.31, 7.99, 9.865 });
		}
		public static Sortament A400()
		{
			return new Sortament("ГОСТ 5781-82","А400", FindDiameter, new string[]
			                     {"4", "5", "6", "8", "10", "12", "14", "16", "18", "20", "22", "25", "28", "32", "36", "40"},
			                     new double[]
			                     {0.092, 0.144, 0.222, 0.395, 0.617, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.84, 4.83, 6.31, 7.99, 9.865 });
		}
		public static Sortament Pipe10704()
		{
			return Sortament.FromExcel("ГОСТы.xlsx","ГОСТ 10704-91","Таблица","Труба",PipeMark);
		}
		
		public static Sortament Pipe8732()
		{
			return Sortament.FromExcel("ГОСТы.xlsx","ГОСТ 8732-78","Таблица2","Труба",PipeMark);
		}
		public static Sortament Gost103()
		{
			return Sortament.FromExcel("ГОСТы.xlsx","ГОСТ 103-2006","Таблица3","Полоса",PipeMark);
		}
		
		
		/// <summary>
		/// функция будет вычленять из строки марку диаметра. Можно сделать и анонимными методами
		/// </summary>
		/// <param name="inp">строка спецификации</param>
		/// <returns>диаметр</returns>
		static string FindDiameter(string inp)
		{
			var mt=Regex.Match(inp,@"(∅\d+|%%c\d+)").Value;
			return Regex.Replace(mt,@"(∅\.?|%%c\.?)","");
		}
		
		/// <summary>
		/// поиск марки трубы вида 108х4 или 57х3,0
		/// </summary>
		/// <param name="inp"></param>
		/// <returns></returns>
		static string PipeMark(string inp)
		{
			string val= Regex.Match(inp,@"\d+.?.?[xх]\d+?\d?.?\d?").Value.Replace('x','х').Replace(',','.');
			
			double diam=Convert.ToDouble(val.Split('х')[0]);
			double thick=Convert.ToDouble(val.Split('х')[1]);
			
			
			return string.Format("{0:F1}х{1:F1}",diam,thick);
		}
		
		static string dir_bundle=@"C:\ProgramData\Autodesk\ApplicationPlugins\PKUserTools.bundle\";
		
		/// <summary>
		/// Загрузка данных о сортаменте и таблице масс из файла эксель.
		/// Для корректного чтения в листе должны быть данные о диапазонах для чтения марки и о наименовании ГОСТа
		/// </summary>
		/// <param name="filename">файл</param>
		/// <param name="page">название листа</param>
		/// <returns></returns>
		public static Sortament FromExcel(string filename, string page,string tblname, string aclass, Func<string,string> mark_pattern)
		{
			var sw=new Stopwatch();
			sw.Start();
			
			var xlapp= new XL.Application();
			var xlwb=xlapp.Workbooks.Open(dir_bundle+filename);
			var sheet=(XL.Worksheet)xlwb.Worksheets[page];
			
			//метаданные
			string Gost=((XL.Range)(sheet.Cells[1,1])).Value2.ToString();
			
			//попробуем ускорить процесс заранее заданной емкостью памяти
			int capacity=Convert.ToInt32(((XL.Range)(sheet.Cells[1,2])).Value2);
			
			/*
			//чрезвычайно медленно!!!!
			//при первой загрузке программы требуется около 17сек на загрузку данных
			for(int i=start_col;i<start_col+end_col;i++)
			{
				for(int j=start_r;j<start_r+end_r;j++)
				{
					double val=0;
					string key=string.Format("{0:F1}х{1:F1}",((XL.Range)(sheet.Cells[j,start_col-1])).Value2,((XL.Range)(sheet.Cells[start_r-1,i])).Value2);
					
					val=Convert.ToDouble(((XL.Range)(sheet.Cells[j,i])).Value2);
					if(val!=0)
					{
						keys.Add(key);
						values.Add(val);
					}
				}
			}
			 */
			
			
			var keys=new List<string>(capacity);
			var values=new List<double>(capacity);
			
			var data=(object[,])((XL.Range)(sheet.Range[tblname])).Value;
			
			for(int i=2;i<=data.GetLength(0);i++)
			{
				for(int j=2;j<=data.GetLength(1);j++)
				{
					double val=0;
					string key=string.Format("{0:F1}х{1:F1}",data[i,1],data[1,j]);
					
					if(data[i,j]==null) continue;
					if(double.TryParse(data[i,j].ToString(),out val))
					{
						keys.Add(key);
						values.Add(val);
					}
				}
			}
			
			
			
			//чистим память
			xlwb.Close(false,0,0);
			xlapp.Quit();
			
			Marshal.ReleaseComObject(sheet);
			Marshal.ReleaseComObject(xlwb);
			Marshal.ReleaseComObject(xlapp);
			
			sw.Stop();
			PKUserTools.Utilities.Messaging.Tweet("Затрачено времени на открытие "+ Gost+ " " + sw.ElapsedMilliseconds + "миллисек");
			
			return new Sortament(Gost,aclass,mark_pattern,keys.ToArray(),values.ToArray());
		}
		
	}
}
