/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 23.04.2019
 * Время: 11:22
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using PKUserTools.EditorInput;
using PKUserTools.Utilities;
using Autodesk.AutoCAD.DatabaseServices;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Вычисляет точи для копирования исходя из различных данных (строка или объекты)
	/// формула цепочки размеров должна быть вида 100+200+50*250+50 либо х или x (лат) без учета регистра
	/// </summary>
	internal class StepsParser
	{
		List<double> steps = new List<double>();
		
		public StepsParser(string input)
		{
			var words = input.Split(new [] {'+'},StringSplitOptions.RemoveEmptyEntries);
			double sum=0;
			foreach(var w in words)
			{
				double parsed=0;
				int step=0;
				double multiplier=0;
				
				if(!double.TryParse(w,out parsed))
					//при неверном вводе сначала проверяем на предмет соответствия слова виду 10*250,
					//если и этому не соответствует, продолжаем дальше, не выбрасывая исключения
				{
					Regex r = new Regex(@"\d+[хx*]\d+");
					if(!r.IsMatch(w)) continue;
					Regex r_step = new Regex(@"\d+"); //выбираем первое число шагов и второе величина шага
					
					var m = r_step.Matches(w);
					
					multiplier=double.Parse(m[0].Value);
					step=int.Parse(m[1].Value);
					
					for(int i=1;i<=multiplier;i++)
					{
						sum+=step;
						steps.Add(sum);
					}
				}
				else
				{
					sum+=parsed;
					steps.Add(sum);
				}
			}
		}
		
		public List<Point3d> Points (Point3d start, Point3d dir)
		{
			Vector3d delta = dir.GetAsVector().Subtract(start.GetAsVector());
			delta=delta.DivideBy(delta.Length);
			
			return steps.Select(d=>start.Add(delta*d)).ToList();
		}
		public static void TestCommand()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var sset = Input.Objects("Выберите объекты для копирования"); if(Input.StatusBad) return;
			var basept = Input.Point("Базовая точка"); if(Input.StatusBad) return;
			
			var pt1 = Input.Point("Первая точка"); if(Input.StatusBad) return;
			var pt2 = Input.Point("Вторая точка"); if(Input.StatusBad) return;
			var str = Input.Text("Строка"); if(Input.StatusBad) return;
			
			try
			{
				var sp = new StepsParser(str);
				Point3d offset;
				Matrix3d M;
				
				foreach(var pt in sp.Points(pt1,pt2))
				{
					using(var th = new TransactionHelper())
					{
						var to_clone = th.ReadObjects(sset);
						
						offset = pt.Subtract(basept.GetAsVector());
						if (offset == Point3d.Origin) continue;//исключаем копирование в ту же точку
						M = Matrix3d.Displacement(offset.GetAsVector());
						foreach (Entity ent in to_clone)
						{
							Entity ent_clone = ent.GetTransformedCopy(M);
							th.WriteObject(ent_clone);
						}
					}
				}
			}
			catch(Exception ex)
			{
				Messaging.Tweet(ex);
			}
		}
	}
}
