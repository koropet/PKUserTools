/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 15.07.2020
 * Время: 11:05
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using PKUserTools.EditorInput;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Description of ReadToVor.
	/// </summary>
	public static class ReadToVor
	{
		/// <summary>
		/// Сделано временно для быстрого ввода данных о диаметрах с чертежей и вывод их в упорядоченную таблицу
		/// Здесь же добавлен объем бетона
		/// </summary>
		public static void MakeRebarString()
		{
			string heading = "БЕТОН\tД40\tД36\tД32\tД28\tД25\tД20\tД16\tД12\tД10\tД8";
			var dict = new Dictionary<string, string>();
			var splitted = heading.Split('\t');
			foreach(var s in splitted)
			{
				dict.Add(s,"");
			}
			
			var sset = Input.Objects("Выберите сначала объем бетона затем текстовые элементы ведомости расхода стали"); if(Input.StatusBad) return;
			string output="";
			
			List<Entity> entities;
			var strings = new List<string>();
			string concrete="";
			
			using(var th = new TransactionHelper())
			{
				entities=th.ReadObjects(sset);
				concrete = Utilities.GetText(entities[0]);
				entities=entities.Skip(1).ToList();
				
				entities.Sort(Comparer<Entity>.Create(MyCompare));
				
				foreach(var ent in entities)
				{
					string s = Utilities.GetText(ent);
					if(String.IsNullOrEmpty(s)) continue;
					
					strings.Add(s);
				}
				
			}
			for(int i=1;i<strings.Count;i+=2)
			{
				var pair = DiameterPair(new Tuple<string, string>(strings[i-1],strings[i]));
				dict[pair.Item1]=pair.Item2;
			}
			
			var match_number = new Regex(@"\d+\.\d+м");
			concrete=match_number.Match(concrete.Replace(",",".")).Value.Replace("м","");
			dict["БЕТОН"]=concrete.Replace(".",",");
			
			output=dict.Values.Aggregate((a,b)=>a+'\t'+b);
			
			Clipboard.SetText(output);
		}
		static Tuple<string,string> DiameterPair(Tuple<string,string> inp)
		{
			var match_number = new Regex(@"\d+");
			string first = inp.Item1;
			string second = inp.Item2;
			string diam = "Д",value;
			
			if(first.Contains(@"%%C")||first.Contains(@"∅"))
			{
				diam+=match_number.Match(first).Value;
				value=second;
			}
			else
			{
				diam+=match_number.Match(second).Value;
				value=first;
			}
			return new Tuple<string, string>(diam,value);
		}
		/// <summary>
		/// Свой компаратор текста для сортировки
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		static int MyCompare(Entity a, Entity b)
		{
			return Center(a).X.CompareTo(Center(b).X);
		}
		static Point3d Center(Entity ent)
		{
			var dbt = ent as DBText;
			if(dbt!=null)
			{
				return GetCenter(dbt);
			}
			var mt = ent as MText;
			if(mt!=null)
			{
				return GetCenter(mt);
			}
			return Point3d.Origin;
		}
		/// <summary>
		/// Функция поиска центра текста
		/// </summary>
		/// <param name="mt">Мультитекст</param>
		/// <returns>Центральная точка</returns>
		static Point3d GetCenter(MText mt)
		{
			Point3d location = Point3d.Origin;

			Point3dCollection points = mt.GetBoundingPoints();

			Point3d tl, br;
			try
			{
				tl = points[0];
				br = points[3];

			}
			catch (System.Exception ex)
			{
				Messaging.Alert("Не удалось прочитать границы текста\n\n" + ex);
				return location;
			}

			double X = tl.X * 0.5 + br.X * 0.5;
			double Y = tl.Y * 0.5 + br.Y * 0.5;

			location = new Point3d(X, Y, 0);


			return location;
		}

		/// <summary>
		/// Функция поиска центра текста
		/// </summary>
		/// <param name="dbt">Текст</param>
		/// <returns>Центральная точка</returns>
		static Point3d GetCenter(DBText dbt)
		{
			Point3d location = Point3d.Origin;


			Point3d tl, br;

			Extents3d? nex = dbt.Bounds;

			if (!nex.HasValue)
			{
				Messaging.Alert("Не удалось прочитать границы текста");
				return location;
			}

			tl = nex.Value.MinPoint;
			br = nex.Value.MaxPoint;

			double X = tl.X * 0.5 + br.X * 0.5;
			double Y = tl.Y * 0.5 + br.Y * 0.5;

			location = new Point3d(X, Y, 0);

			return location;
		}
	}
}
