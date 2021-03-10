/*
 * Сделано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 20.06.2018
 * Время: 17:39
 * 
 * Для изменения этого шаблона используйте Сервис | Настройка | Кодирование | Правка стандартных заголовков.
 */

using System;
using System.Collections.Generic;

using System.Drawing;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Windows;
using Autodesk.AutoCAD.Geometry;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using UT = PKUserTools.Utilities.Utilities;


using XL = Microsoft.Office.Interop.Excel;
namespace PKUserTools.Utilities
{
	/*UPD
	 *Это матерый способ из C# 1.0
	 *Гораздо более современно будет сортировать объекты "в лёт" средствами
	 * делегатов или через лямбда-выражение как удобно в конкретном месте кода
	 * например
	 * horLines.Sort((x,y)=>-1*x.StartPoint.Y.CompareTo(y.StartPoint.Y));
	 * сортирует линии по координате У начала линии в обратном порядке
	 */
	
	//компараторы для осртировки коллекции линий
	//сортируют по Х слева направо и по У сверху вниз
	/// <summary>
	/// Компаратор для сортировки горизонтальных линий
	/// </summary>
	class hlcomparer : IComparer<Line>
	{


		public int Compare(Line x, Line y)
		{
			if (x.StartPoint.Y < y.StartPoint.Y)
				return 1;
			if (x.StartPoint.Y > y.StartPoint.Y)
				return -1;
			return 0;
		}
	}
	/// <summary>
	/// Компаратор для сортировки вертикальных линий
	/// </summary>
	class vlcomparer : IComparer<Line>
	{


		public int Compare(Line x, Line y)
		{
			if (x.StartPoint.X < y.StartPoint.X)
				return -1;
			if (x.StartPoint.X > y.StartPoint.X)
				return 1;
			return 0;
		}
	}



	public class TextComparerIncell : IComparer<Entity>
	{
		Point3d positionA;
		/// <summary>
		/// Возвращает позицию текстового объекта, либо текст, либо Мтекст
		/// </summary>
		/// <param name="a">Текстовый объект</param>
		/// <returns>Позиция</returns>
		Point3d positionFromEntity(Entity a)
		{
			if (a is DBText)
			{
				positionA = ((DBText)a).Position;
			}
			else if (a is MText)
			{
				positionA = ((MText)a).Location;
			}
			else
			{
				positionA = Point3d.Origin;
				Messaging.Alert("Программа почему-то попыталась отсортировать не тексты");
			}
			return positionA;
		}
		/// <summary>
		/// Функция нахождения высоты текста
		/// </summary>
		/// <param name="a">Текст</param>
		/// <returns>Высота</returns>
		double height(Entity a)
		{
			double h = 0;
			if (a is DBText)
			{
				h = ((DBText)a).Height;
			}
			else if (a is MText)
			{
				h = ((MText)a).Height;
			}
			else
			{
				h = 0;
				Messaging.Alert("что-то не так с текстом " + a.ToString());
			}
			return h;
		}
		public int Compare(Entity a, Entity b)
		{
			Point3d positionA = positionFromEntity(a);
			Point3d positionB = positionFromEntity(b);

			if (positionA.Y - positionB.Y > Math.Max(height(a), height(b)))
			{
				return -1;
			}
			else if (positionA.Y - positionB.Y < -1 * Math.Max(height(a), height(b)))
			{
				return 1;
			}
			else
			{
				if (positionA.X - positionB.X > 0)
					return 1;
				else if (positionA.X - positionB.X < 0)
					return -1;
				else return 0;

			}

		}
	}
	/// <summary>
	/// Сортировщик отрезков вдоль другого отрезка относительно начальной точки
	/// </summary>
	public class LineSorter:IComparer<Line>
	{
		public Line Direction;
		public int Compare(Line x, Line y)
		{

			Point3d intersect_x=UT.Intersection(x,Direction);
			if (!(UT.has_intersect)) return 0;
			Point3d intersect_y=UT.Intersection(y,Direction);
			if (!(UT.has_intersect)) return 0;

			Line sx=new Line(Direction.StartPoint,intersect_x);
			Line sy=new Line(Direction.StartPoint,intersect_y);

			if (sx.Length<sy.Length)
				return 1;
			if (sx.Length>sy.Length)
				return -1;
			return 0;

		}
	}
	
	/// <summary>
	/// Сортировщик точек в сответствии с направлением
	/// </summary>
	public class PointSorter : IComparer<Point3d>
	{
		public Line direction;
		
		public int Compare(Point3d x, Point3d y)
		{
			Vector3d xy=y.GetAsVector().Subtract(x.GetAsVector());
			double xy_ang=xy.GetAngleTo(Vector3d.XAxis,Vector3d.ZAxis);
			
			double ang=Math.Abs(xy_ang-direction.Angle);
			
			double g90=Math.PI/2;
			double g270=Math.PI*1.5;
			
			double g360=Math.PI*2;
			
			if(ang>=0 && ang<g90) return 1;
			if(ang>g90 && ang<g270) return -1;
			if(ang>g270&&ang<=g360) return 1;
			if(ang==g90||ang==g270) return 0;
			
			
			return 0;
			
		}
	}
	/* пока не реализовали сегмент, это бесполезно
	/// <summary>
	/// Сортировщик сегментов.
	/// </summary>
	public class SegmentComparer : IComparer<ItemSegment>
	{
		public Line Direction;
		public int Compare(ItemSegment x, ItemSegment y)
		{
			Plane pl=new Plane(Direction.StartPoint,Direction.Delta,Vector3d.ZAxis);
			
			Point3d sx=x.start.OrthoProject(pl);
			Point3d sy=y.start.OrthoProject(pl);
			Point3d ex=x.end.OrthoProject(pl);
			Point3d ey=y.end.OrthoProject(pl);
			
			Point3d mx=sx.MultiplyBy(0.5).Add(ex.MultiplyBy(0.5).GetAsVector());
			
			Point3d my=sy.MultiplyBy(0.5).Add(ey.MultiplyBy(0.5).GetAsVector());
			
			double a1=mx.DistanceTo(Direction.StartPoint);
			double b1=my.DistanceTo(Direction.StartPoint);
			
			if(a1<b1) return 1;
			else if(a1==b1) return 0;
			else return -1;
		}
	}*/
}