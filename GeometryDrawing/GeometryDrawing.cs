/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 17.01.2019
 * Время: 10:29
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

using System.Windows.Forms;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using PKUserTools.EditorInput;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;

namespace PKUserTools.GeometryDrawing
{
	/// <summary>
	/// Description of GeometryDrawing.
	/// </summary>
	public static class GeometryDrawing
	{
		static public void DrawRectangleFromEditor()
		{
			var Loc = Input.Point("Укажите базовую точку"); if(Input.StatusBad) return;
			double w,h,a,b;
			while(true)
			{
				a=Input.Double("Введите смещение по горизонтали"); if(Input.StatusBad) break;
				b=Input.Double("Введите смещение по вертикали"); if(Input.StatusBad) break;
				w=Input.Double("Введите ширину"); if(Input.StatusBad) break;
				h=Input.Double("Введите высоту"); if(Input.StatusBad) break;
				
				DrawRectangle(Loc.Add(new Vector3d(a,b,0)),w,h);
			}
		}
		static void DrawRectangle(Point3d Location, double Width, double Heigth)
		{
			var points= new List<Point2d>();
			points.Add(new Point2d(Location.X,Location.Y));
			points.Add(new Point2d(Location.X+Width,Location.Y));
			points.Add(new Point2d(Location.X+Width,Location.Y-Heigth));
			points.Add(new Point2d(Location.X,Location.Y-Heigth));
			
			using (var th=new TransactionHelper())
			{
				var pl=FromPointList(points,true);
				pl.SetDatabaseDefaults();
				th.WriteObject(pl);
			}
			
		}
		
		/// <summary>
		/// Простая полилиния без дуг на основе массива точек
		/// </summary>
		/// <param name="points"></param>
		/// <param name="closed"></param>
		/// <returns></returns>
		static Polyline FromPointList (List<Point2d> points, bool closed=false)
		{
			var pl=new Polyline();
			int number=0;
			foreach(var pt in points)
			{
				pl.AddVertexAt(number++,pt,0,0,0);
			}
			pl.Closed=closed;
			return pl;
		}
	}
}
