/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 12.09.2018
 * Время: 9:42
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Description of MyExtensions.
	/// </summary>
	public static class MyExtensions
	{
		const double tolerance=1d;
		
		/// <summary>
		/// Функция для группировки линий исходя из горизонтальности/верикальности и длине линий
		/// возвращает 1 для горизонтальных линий полной длины 2 для коротких
		///3 вертикальные полной длины и 4 короткие
		/// -1 если ни одно условие не соблюдается
		/// </summary>
		/// <param name="ln">линия</param>
		/// <param name="b">ширина</param>
		/// <param name="h">высота</param>
		/// <returns></returns>
		public static int LineGrouping(this Line ln,double b,double h)
		{
			if(Math.Abs(ln.Delta.Y)<tolerance)
			{
				//горизонтальны
				
				return (Math.Abs(ln.Length-b)<=tolerance)?1:2;
			}
			if(Math.Abs(ln.Delta.X)<tolerance)
			{
				//вертикальные
				return (Math.Abs(ln.Length-h)<=tolerance)?3:4;
			}
			return -1;
		}
		/// <summary>
		/// Сравнение двух кривых исходя из направления сортировки
		/// </summary>
		/// <param name="x">первая кривая</param>
		/// <param name="y">вторая кривая</param>
		/// <param name="sorting_direction">кривая, по которой определяется направление сортировки</param>
		/// <returns></returns>
		/// <param name = "extend">Удлиннение</param>
		public static int CurveCompare(this Curve x,Curve y,Curve sorting_direction,bool extend)
		{
			//берем первые точки пересечения. Не забыть проверить есть ли они
			try
			{
			Point3d ptx=Utilities.IntersectWith(x,sorting_direction,extend)[0];
			Point3d pty=Utilities.IntersectWith(y,sorting_direction,extend)[0];
			
			return sorting_direction.GetParameterAtPoint(ptx).CompareTo(sorting_direction.GetParameterAtPoint(pty));
			}
			catch(Exception)
			{
				Messaging.Tweet("Не можем сравнить линии");
				return 0;
			}
		}
		
		/// <summary>
		/// Получаем 2д точку
		/// </summary>
		/// <param name="point">3д точка</param>
		/// <returns></returns>
		public static Point2d to2d(this Point3d point)
		{
			return new Point2d(point.X,point.Y);
		}
		
		public static Point2d to2d(this Point3d point,PlanarEntity pl)
		{
			return point.Convert2d(pl);
		}
	}
}
