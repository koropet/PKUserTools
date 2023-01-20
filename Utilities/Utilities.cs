using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.Utilities
{
	public static class Utilities
	{
		/// <summary>
		/// Type "CANNOSCALE" to get current scale
		/// </summary>
		/// <param name="scalename"></param>
		/// <returns></returns>
		public static AnnotationScale GetAnnoScale(string scalename)
		{
			if (scalename == "CANNOSCALE")
			{
				scalename = App.GetSystemVariable("CANNOSCALE").ToString();
			}

			Document acDoc = App.DocumentManager.MdiActiveDocument;
			Database acCurDb = acDoc.Database;

			AnnotationScale asc = new AnnotationScale();

			ObjectContextManager ocm = acCurDb.ObjectContextManager;
			if (ocm != null)
			{
				ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
				if (occ != null)
				{

					asc = occ.GetContext(scalename) as AnnotationScale;


				}
			}
			return asc;
		}

		/// <summary>
		/// Середина между точками
		/// </summary>
		/// <param name="p1">Первая точка</param>
		/// <param name="p2">Вторая точка</param>
		/// <returns>Середина</returns>
		public static Point3d GetMiddle(Point3d p1, Point3d p2)
		{
			return p1.MultiplyBy(0.5).Add(p2.MultiplyBy(0.5).GetAsVector());
		}

		/// <summary>
		/// Нахождение геометрического центра по проекции на выбранное направление
		/// </summary>
		/// <param name="ent">Примитив</param>
		/// <param name="direction">Выбранное направление</param>
		/// <returns>Центр</returns>
		/// <param name="HasIntersect">Результат операции</param>
		/// <param name="DimPoint">Точка размерной линии</param>
		public static Point3d GetCenter(Entity ent, Line direction, Point3d DimPoint, out bool HasIntersect)
		{
			HasIntersect = true;

			Point3d result = Point3d.Origin;

			Vector3d vx = direction.Delta;
			Vector3d vy = vx.GetPerpendicularVector();
			Matrix3d M = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, direction.StartPoint, vx.DivideBy(vx.Length), vy.DivideBy(vy.Length), Vector3d.ZAxis);
			Entity transformed = ent.GetTransformedCopy(M);
			//далее надо найти все точки и выяснить крайние координаты

			if (transformed is Line)
			{
				Line ln = transformed as Line;
				
				Plane pl=new Plane(Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis);
				Point3dCollection pts= new Point3dCollection();
				
				ent.IntersectWith(direction,Intersect.ExtendArgument,pl,pts,IntPtr.Zero,IntPtr.Zero);
				
				if(pts.Count>0)
				{
					return pts[0];
				}
				
				Point3d DimPoint_trans = DimPoint.TransformBy(M);

				if (DimPoint_trans.DistanceTo(ln.StartPoint) > DimPoint_trans.DistanceTo(ln.EndPoint)) result = ln.EndPoint;
				else result = ln.StartPoint;
			}
			else if (transformed is Circle)
			{
				result = ((Circle)transformed).Center;
			}

			else if (transformed is Polyline)
			{
				Polyline pl = transformed as Polyline;

				double minx = double.PositiveInfinity;
				double maxx = double.NegativeInfinity;

				double miny = double.PositiveInfinity;
				double maxy = double.NegativeInfinity;

				Point3d curr_point;

				for (int i = 0; i < pl.NumberOfVertices; i++)
				{
					curr_point = pl.GetPoint3dAt(i);

					if (curr_point.X < minx) minx = curr_point.X;
					if (curr_point.X > maxx) maxx = curr_point.X;

					if (curr_point.Y < miny) miny = curr_point.Y;
					if (curr_point.Y > maxy) maxy = curr_point.Y;

				}

				result = new Point3d(minx * 0.5 + maxx * 0.5, miny * 0.5 + maxy * 0.5, 0);

			}
			else if (transformed is BlockReference)
			{
				result = ((BlockReference)transformed).Position;
			}
			else
			{
				HasIntersect = false;

			}

			Matrix3d Mt = M.Inverse();//вернем координаты точки обратно
			return result.TransformBy(Mt);

		}

		public static Point2d from3dto2d(Point3d pt)
		{
			return new Point2d(pt.X, pt.Y);
		}

		/// <summary>
		/// Флаг для выяснения, есть ли пересечение
		/// </summary>
		public static bool has_intersect = true;
		/// <summary>
		/// Пересечение прямых, заданных отрезками
		/// </summary>
		/// <param name="p1p2">1 отрезок</param>
		/// <param name="p3p4">2 отрезок</param>
		/// <returns>Точка пересечения</returns>
		public static Point3d Intersection(Line p1p2, Line p3p4)
		{
			Point3d P = Point3d.Origin;

			Point3d p1 = p1p2.StartPoint;
			Point3d p2 = p1p2.EndPoint;
			Point3d p3 = p3p4.StartPoint;
			Point3d p4 = p3p4.EndPoint;

			double x1 = p1.X, x2 = p2.X, x3 = p3.X, x4 = p4.X;
			double y1 = p1.Y, y2 = p2.Y, y3 = p3.Y, y4 = p4.Y;

			//проверяем параллельность или слишком близкие углы прямых
			if (p1p2.Angle == p3p4.Angle | Math.Abs(p1p2.Angle - p3p4.Angle) < 0.01)
			{
				has_intersect = false;
				return P;
			}

			//формула с вики, статья пересечение прямых

			double X = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));


			double Y = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
			P = new Point3d(X, Y, 0);

			has_intersect = true;
			return P;
		}
		/// <summary>
		/// Нахождение пересечения стандартными средствами автокада
		/// </summary>
		/// <param name="x">Первый элемент</param>
		/// <param name="y">Второй элемент</param>
		/// <param name="extendall">продлевать линии до пересечения</param>
		/// <returns>Коллекцию точек пересечения</returns>
		public static Point3dCollection IntersectWith(Entity x,Entity y,bool extendall=true)
		{
			Plane pl=new Plane(Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis);
			Point3dCollection pts= new Point3dCollection();
			
			if(extendall)
			{
				x.IntersectWith(y,Intersect.ExtendBoth,pl,pts,IntPtr.Zero,IntPtr.Zero);
			}
			else
			{
				x.IntersectWith(y,Intersect.OnBothOperands,pl,pts,IntPtr.Zero,IntPtr.Zero);
			}
			return pts;
		}
		public static double EntityLength(Entity ent)
		{
			double length = 0;
			if (ent is Line)
			{
				length = ((Line)ent).Length;
			}
			if (ent is Polyline)
			{
				length = ((Polyline)ent).Length;


			}
			if (ent is Circle)
			{
				length = ((Circle)ent).Radius * Math.PI * 2;
			}
			if (ent is Arc)
			{
				length = ((Arc)ent).Length;

			}
			return length;
		}
		/// <summary>
		/// text from some entity
		/// </summary>
		/// <param name="ent"></param>
		/// <returns></returns>
		public static string GetText(Entity ent)
		{
			if (ent is MText)
			{
				MText mt = (MText)ent;

				//TODO:тут можно вынуть существующий текст и обработать форматирование
				//и запихать уже подготовленную строку
				return mt.Contents;

			}
			if (ent is DBText)
			{
				DBText dt = (DBText)ent;

				return dt.TextString;
			}
			if (ent is MLeader)
			{
				MLeader ml = (MLeader)ent;
				MText aa = ml.MText.Clone() as MText;

				return aa.Contents;


			}
			if (ent is Leader)
			{
				Leader ld = (Leader)ent;

				using (TransactionHelper th = new TransactionHelper())
				{
					MText ann = th.ReadObject(ld.Annotation) as MText;

					return ann.Contents;
				}

			}
			return "";
		}
		
		/// <summary>
		/// Везвращает вектор единичной длины из 2 точек
		/// </summary>
		/// <param name="p1">Конец</param>
		/// <param name="p2">Начало</param>
		/// <returns></returns>
		public static Vector3d GetUniteVector(Point3d p1, Point3d p2)
		{
			var vp1p2=(p2.GetAsVector().Subtract(p1.GetAsVector()));
			vp1p2=vp1p2.MultiplyBy(1/vp1p2.Length);
			return vp1p2;
		}
        /// <summary>
        /// Возвращает связаную с листом запись таблицы блоков
        /// </summary>
        /// <param name="LayoutName">имя листа</param>
        /// <returns>бтр</returns>
      public  static string btr_from_layout(string LayoutName)
        {
            var acDoc = App.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;
            var acEd = acDoc.Editor;

            var lm = LayoutManager.Current;
            ObjectId layout_id = ObjectId.Null;
            try
            {
                layout_id = lm.GetLayoutId(LayoutName);
            }
            catch (Exception)
            {
                Console.WriteLine("Не найден лист с названием " + LayoutName);
            }

            using (var acTrans = acDoc.TransactionManager.StartTransaction())
            {
                var blockTable = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var layout = acTrans.GetObject(layout_id, OpenMode.ForRead) as Layout;
                var btr = acTrans.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                return btr.Name;
            }

        }
      public static void SendStringToExecute(string str)
      {
      	App.DocumentManager.CurrentDocument.SendStringToExecute(str,true,false,true);
      }
    }
}
