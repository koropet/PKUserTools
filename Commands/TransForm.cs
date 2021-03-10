/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 10.06.2020
 * Время: 16:56
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 * 
 * Класс реализует преобразования координат
 */

using System;
using System.Collections.Generic;
using System.Linq;

using System.Numerics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.EditorInput;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of TransForm.
	/// </summary>
	public static class TransForm
	{
		static TransactionHelper sth;
		public static void InCline()
		{
			var sset = Input.Objects("Выберите объекты для преобразования"); if(Input.StatusBad) return;
			Point3d basePnt = Input.Point("Укажите базовую точку преобразования."); if(Input.StatusBad) return;
			//elevation divided by horisontal longtitude или по-русски уклон, тангенс угла наклона
			double tangent = Input.Double("Введите величину уклона. Положительный уклон повышается вправо (Х+ У+)"); if(Input.StatusBad) return;
			
			
			var matrix = Matrix3d.AlignCoordinateSystem(
				basePnt,Vector3d.XAxis,Vector3d.YAxis,Vector3d.ZAxis,
				basePnt,Vector3d.XAxis+Vector3d.YAxis*tangent,Vector3d.YAxis, Vector3d.ZAxis);
			
			
			using( var th = new TransactionHelper())
			{
				sth=th;
				
				var objects = th.EditObjects(sset);
				TransformObjects(objects,matrix);
				
				sth=null;
			}
			
			
		}
		static void TransformObjects(IEnumerable<Entity> objects, Matrix3d transform)
		{
			var actions = new Dictionary<Type,Action<Entity,Matrix3d>>();
			
			actions.Add(typeof(Line),
			            (l, m) =>TransFormLine((Line)l,m));
			actions.Add(typeof(Polyline),
			            (poly, m) =>TransformPolyline((Polyline)poly,m));
			actions.Add(typeof(Circle),
			            (c, m) =>
			            ((Circle)c).Center = ((Circle)c).Center.TransformBy(m));
			actions.Add(typeof(BlockReference),
			            (b, m) =>
			            TransformBlock((BlockReference)b,m));
			actions.Add(typeof(Spline), (s,m)=>
			            s.TransformBy(m));
			actions.Add(typeof(Xline),
			            (x, m) =>
			            TransformXline((Xline)x,m));
			actions.Add(typeof(Ray),
			            (r, m) =>
			            TransformRay((Ray)r,m));
			
			foreach(var ent in objects)
			{
				if(!actions.ContainsKey(ent.GetType())) continue;
				actions[ent.GetType()](ent,transform);
			}
			
			
		}
		static void TransFormLine(Line l, Matrix3d transform)
		{
			l.StartPoint=l.StartPoint.TransformBy(transform);
			l.EndPoint=l.EndPoint.TransformBy(transform);
		}
		static void TransformPolyline(Polyline poly, Matrix3d transform)
		{
			int count = poly.NumberOfVertices;
			
			for(int i=0;i<count;i++)
			{
				var pt = poly.GetPoint2dAt(i);
				Point3d pt3 = new Point3d(pt.X,pt.Y,0);
				pt3=pt3.TransformBy(transform);
				pt=pt3.to2d();
				poly.SetPointAt(i,pt);
				
			}
		}
		static void TransformBlock(BlockReference b, Matrix3d transform)
		{
			b.Position = b.Position.TransformBy(transform);
			
			
			foreach(ObjectId objectId in b.AttributeCollection)
			{
				var attref = sth.EditObject(objectId) as AttributeReference; //carefull!! if called without transaction it will crash
				attref.Position=attref.Position.TransformBy(transform);
			
			}
			
		}
		static void TransformXline(Xline x, Matrix3d transform)
		{
			x.StartPoint=x.StartPoint.TransformBy(transform);
			x.SecondPoint=x.SecondPoint.TransformBy(transform);
		}
		static void TransformRay(Ray r, Matrix3d transform)
		{
			r.StartPoint=r.StartPoint.TransformBy(transform);
			r.SecondPoint=r.SecondPoint.TransformBy(transform);
		}
	}
}
