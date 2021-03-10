using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.EditorInput;

namespace PKUserTools.Commands
{
	class PatternCopy : FunctionClass
	{
		Point3d base_point = Point3d.Origin;
		Point3d base_point_alternative = Point3d.Origin;

		List<Entity> objects_tocopy = new List<Entity>();
		List<Entity> objects_tocopy_alternative = new List<Entity>();

		List<Point3d> points = new List<Point3d>();
		List<Point3d> points_alternative = new List<Point3d>();

		List<Entity> lines_first_ent = new List<Entity>();
		List<Entity> lines_second_ent = new List<Entity>();

		List<Curve> lines_first = new List<Curve>();
		List<Curve> lines_second = new List<Curve>();

		Point3d offset;
		Matrix3d M;
		
		//удлиннять линии по умолчанию
		//если в случае сложных форм не удается шахматный порядок, отключить удлиннение
		static bool extend=true;

		static bool copy_chess = false;
		

		public override void Execute()
		{
			base.Execute();
			Tweet(string.Format("Режим шахматного порядка: {0}", copy_chess));
			Tweet(string.Format("Режим удлиннения: {0}", extend));

			
			var objects_tocopy_sset=Input.Objects(
				message: "Выберите объекты для копирования",
				keywordinput: new SelectionTextInputEventHandler((s, e) =>
				                                                 {
				                                                 	if(e.Input=="CHess")
				                                                 	{
				                                                 		copy_chess = !copy_chess;
				                                                 		Tweet(string.Format("Режим шахматного порядка: {0}", copy_chess));
				                                                 	}
				                                                 	else if (e.Input=="EXtend")
				                                                 	{
				                                                 		extend=!extend;
				                                                 		Tweet(string.Format("Режим удлиннения: {0}", extend));
				                                                 	}
				                                                 }),
				keywords: new string[] { "CHess", "Шахматный","EXtend","Удлиннять" });
			if(Input.StatusBad) return;
			
			base_point=Input.Point("Выберите базовую точку"); if(Input.StatusBad) return;
			
			
			SelectionSet objects_tocopy_alt_sset=null;
			
			if (copy_chess)
			{
				objects_tocopy_alt_sset=Input.Objects("\nВыберите альтернативные объекты для копирования или оставьте пустым");
				if(Input.StatusCancel) return;
				
				if (objects_tocopy_alt_sset== null) Tweet("Выбор оставлен пустым. Копируем в шахматном порядке с пропуском");
				else
				{
					base_point_alternative = Input.Point("Выберите базовую точку"); if(Input.StatusBad) return;
				}
			}
			using (TransactionHelper th = new TransactionHelper())
			{
				objects_tocopy = th.ReadObjects(objects_tocopy_sset);
				if (copy_chess && objects_tocopy_alt_sset != null) objects_tocopy_alternative = th.ReadObjects(objects_tocopy_alt_sset);
				
				var lines_first_sset=Input.Objects("Выберите отрезки первого уровня сетки"); if(Input.StatusBad) return;
				var lines_second_sset=Input.Objects("Выберите отрезки второго уровня сетки"); if(Input.StatusBad) return;
				lines_first_ent = th.ReadObjects(lines_first_sset);
				lines_second_ent = th.ReadObjects(lines_second_sset);

				//lines_first = FilterCurve(lines_first_ent);
				//lines_second = FilterCurve(lines_second_ent);
				//а теперь можно сделать так
				
				lines_first=lines_first_ent.OfType<Curve>().ToList();
				lines_second=lines_second_ent.OfType<Curve>().ToList();

				//берем 1 кривую из 2 набора для сортировки 1 линий
				lines_first.Sort((x,y)=>x.CurveCompare(y,lines_second[0],extend));
				
				FindPoints();

				Copy(points, objects_tocopy, base_point);
				if (copy_chess&&objects_tocopy_alternative!=null) Copy(points_alternative, objects_tocopy_alternative,base_point_alternative);
			}
		}

		void Copy(List<Point3d> points, List<Entity> to_clone, Point3d basept)
		{
			using (TransactionHelper th = new TransactionHelper())
			{
				foreach (Point3d pt in points)
				{
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

		void FindPoints()
		{
			
			for (int i = 0; i < lines_first_ent.Count; i++)
			{
				List<Point3d> lineintersections=new List<Point3d>();
				
				for (int j = 0; j < lines_second_ent.Count; j++)
				{
					var pts=UT.IntersectWith(lines_first_ent[i],lines_second_ent[j],extend);
					
					foreach(Point3d pt in pts)
					{
						lineintersections.Add(pt);
					}
				}
				
				if(lines_first_ent[i] is Curve)
				{
					var crv = lines_first_ent[i] as Curve;
					try
					{
						lineintersections.Sort((x,y)=>crv.GetParameterAtPoint(x).CompareTo(crv.GetParameterAtPoint(y)));
					}
					catch(Exception)
					{
						Tweet("Не удалась сортировка исходя из параметров");
						try
						{
							lineintersections.Sort((x,y)=>crv.StartPoint.DistanceTo(x).CompareTo(crv.StartPoint.DistanceTo(y)));
						}
						catch(Exception)
						{
							Tweet("Сортировка по дистанции от начала кривой не удалась");
						}
					}
					
				}
				
				for (int j = 0; j < lineintersections.Count; j++)
				{
					if(copy_chess)
					{
						if(ChessOrder(i,j)) points.Add(lineintersections[j]);
						else points_alternative.Add(lineintersections[j]);
					}
					else
					{
						points.Add(lineintersections[j]);
					}
				}
				
			}
			
		}

		/// <summary>
		/// Шахматный порядок. Переключение да/нет
		/// </summary>
		/// <param name="i">1 порядок</param>
		/// <param name="j">2 порядок</param>
		/// <returns>true если выблор альтернативного или пустого</returns>
		static bool ChessOrder(int i, int j)
		{
			bool row = false;
			bool column = false;
			if (i % 2 == 0)
			{
				//четный ряд
				row = true;
			}
			if (j % 2 == 0)
			{
				//четный столбец
				column = true;
			}
			return row ^ column;

		}
	}
}
