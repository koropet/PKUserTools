/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 19.09.2018
 * Время: 8:59
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
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

using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.EditorInput;
using PKUserTools.Utilities;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Рисование полилинии на основе двух выбранных хвостов
	/// </summary>
	class DrawTails:FunctionClass
	{
		static Polyline tail_default;
		static sbyte side=1, offsetMode =1 ;
		
		AnnotationScale asc;
		
		public DrawTails()
		{
			
			
			var sset_lines=Input.Objects("Выберите отрезки, на которых нужно нарисовать хвосты ", new string[] {"TAil","ХВост","SIde","СТорона", "OFfset","смеЩЕние"},
			                             (s,e)=>
			                             {
			                             	if(e.Input=="TAil") InputTail();
			                             	if(e.Input=="SIde") side*=-1;
			                             	if(e.Input=="OFfset") offsetMode^=1; //switching 0 and 1
			                             });
			
			if(Input.StatusBad) return;
			
			if(tail_default==null)
			{
				InputTail();
				if(Input.StatusBad)return;
			}
			
			using(var th=new TransactionHelper())
			{
				//забираем линии из выбора
				var lines=th.EditObjects(sset_lines).OfType<Line>().ToList();
				
				//в новой версии хотим добавить обработку полилиний
				var polylines=th.EditObjects(sset_lines).OfType<Polyline>().ToList();
				
				foreach(var ln in lines)
				{
					Point3d p1,p2;
					if(ln.Angle>=0 && ln.Angle<=3*Math.PI/4 ||ln.Angle>7*Math.PI/4)
					{
						p1=ln.EndPoint;p2=ln.StartPoint;
					}
					else {p2=ln.EndPoint;p1=ln.StartPoint;}
					
					var tail_scaled=tail_default.Clone() as Polyline;
					
					asc=UT.GetAnnoScale("CANNOSCALE");
					tail_scaled.TransformBy(Matrix3d.Scaling(1/asc.Scale, Point3d.Origin));
					
					var pl_new=DrawTwoTails(p1,p2,tail_scaled);
					
					pl_new.SetPropertiesFrom(ln);
					
					th.WriteObject(pl_new);
					
					ln.Erase();
					
				}
				foreach(var pl in polylines)
				{
					var tail_scaled=tail_default.Clone() as Polyline;
					
					asc=UT.GetAnnoScale("CANNOSCALE");
					tail_scaled.TransformBy(Matrix3d.Scaling(1/asc.Scale, Point3d.Origin));
					
					var pl_new=TailsFromPolyline(pl,tail_scaled);
					
					
					pl_new.SetPropertiesFrom(pl);
					
					th.WriteObject(pl_new);
					pl.Erase();
				}
			}
			
			
		}
		void InputTail()
		{
			asc=UT.GetAnnoScale("CANNOSCALE");
			var sset_poly=Input.Objects("Выберите полилинию-хвост. Прикрепление будет к начальной точке");
			if(Input.StatusOK)
			{
				using (var th=new TransactionHelper())
				{
					try
					{
						var pl=th.EditObjects(sset_poly)[0] as Polyline;
						
						tail_default=pl.Clone() as Polyline;
					}
					catch(System.Exception)
					{
						InputTail();
						return;
					}
					var p1=Input.Point("Укажите базовую точку");
					var p2=Input.Point("Укажите направление Х");
					var p1p2v=p1.Subtract(p2.GetAsVector()).GetAsVector();
					
					tail_default.TransformBy(Matrix3d.AlignCoordinateSystem(p1,p1p2v.DivideBy(p1p2v.Length),p1p2v.GetPerpendicularVector(),Vector3d.ZAxis,
					                                                        Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis,Vector3d.ZAxis));
					tail_default.TransformBy(Matrix3d.Scaling(asc.Scale,Point3d.Origin));
				}
			}
		}
		
		
		
		Polyline DrawTwoTails(Point3d p1, Point3d p2, Polyline tail)
		{
			Vector3d p1p2v=p1.Subtract(p2.GetAsVector()).GetAsVector();
			Vector3d p2p1v=p1p2v.Negate();
			
			Polyline tail1=tail.Clone() as Polyline;
			Polyline tail2=tail.Clone() as Polyline;
			Polyline result=new Polyline();
			
			
			//first tail
			double bulge=0; //у отрезков балдж будет ноль, лол
			
			double offset=0; //заглушка чтобы работала функция. При прикреплении к отрезку, смещение получится автоматически.
			
			TransformTail(p1,p2,bulge,1,ref tail1, out offset);
			
			for(int i=0;i<tail1.NumberOfVertices;i++)
			{
				if(i>0)result.AddVertexAt(0,tail1.GetPoint2dAt(i),tail1.GetBulgeAt(i-1)*-1,0,0);
				else result.AddVertexAt(0,tail1.GetPoint2dAt(i),0,0,0);
			}
			
			//second tail
			TransformTail(p2,p1,bulge,-1,ref tail2, out offset);
			
			for(int i=0;i<tail2.NumberOfVertices;i++)
			{
				result.AddVertexAt(result.NumberOfVertices,tail2.GetPoint2dAt(i),tail2.GetBulgeAt(i),0,0);
			}
			return result;
		}
		Polyline TailsFromPolyline(Polyline pl,Polyline tail)
		{
			int count = pl.NumberOfVertices;
			if(count<2)
			{
				Tweet("Полилиния с меньше, чем двумя вершинами");
				return null;
			}
			
			Polyline tail1=tail.Clone() as Polyline;
			Polyline tail2=tail.Clone() as Polyline;
			
			Polyline result=new Polyline();
			
			double offset=0;
			
			//first tail
			
			double bulge=pl.GetBulgeAt(0);
			Point3d p1 = pl.GetPoint3dAt(0);
			Point3d p2 = pl.GetPoint3dAt(1);
			TransformTail(p1,p2,bulge,1,ref tail1, out offset);
			
			//second tail
			bulge=pl.GetBulgeAt(count-2);
			p1 = pl.GetPoint3dAt(count-2);
			p2 = pl.GetPoint3dAt(count-1);
			TransformTail(p2,p1,bulge,-1,ref tail2, out offset);
			
			//body of polyline
			
			var ofsetted = pl.GetOffsetCurves(offset);
			foreach(DBObject o in ofsetted)
			{
				var poly_o = o as Polyline;
				if(poly_o==null) 
				{
					Tweet(o.ObjectId + " null");
					continue;
				}
				pl=poly_o;
			}
			
			//add first tail points
			for(int i=0;i<tail1.NumberOfVertices;i++)
			{
				if(i>0)result.AddVertexAt(0,tail1.GetPoint2dAt(i),tail1.GetBulgeAt(i-1)*-1,0,0);
				else result.AddVertexAt(0,tail1.GetPoint2dAt(i),pl.GetBulgeAt(0),0,0);
			}
			//add offsetted polyline points
			for(int i=1;i<pl.NumberOfVertices-1;i++)
			{
				result.AddVertexAt(result.NumberOfVertices,pl.GetPoint2dAt(i),pl.GetBulgeAt(i),0,0);
			}
			//add second tail points
			for(int i=0;i<tail2.NumberOfVertices;i++)
			{
				result.AddVertexAt(result.NumberOfVertices,tail2.GetPoint2dAt(i),tail2.GetBulgeAt(i),0,0);
			}
			return result;
		}
		
		
		void TransformTail(Point3d p1,Point3d p11, double bulge, sbyte flip, ref Polyline tail, out double offset)
		{
			double angle = 2*Math.Atan(bulge);
			/*угол касательной аналитически получен как 2 вписанных угла. Например, если bulge равно 1, то
			 * имеем дугу с хордой равной двум выпуклостям. Это соответствует углу между хордой и касательной 90 градусов, а угол между хордой
			 * и хордой к середине дуги 45 градусов
			 */
			angle*=flip; //чтобы было в нужную сторону на обоих концах
			
			Vector3d p1p2v=p1.Subtract(p11.GetAsVector()).GetAsVector();
			p1p2v = p1p2v.RotateBy(angle,Vector3d.ZAxis*-1); //-1здесь служит для корректировки направления (по результатам отладки)
			
			Point2d first = tail.GetPoint2dAt(0);
			offset=first.Y*offsetMode*side;
			
			//смысл смещения заключается в том, что иногда требуется нарисовать шпильки со смещением от базовой линии
			//смещение управляется начальной координатой хвоста
			
			Matrix3d M=Matrix3d.AlignCoordinateSystem(Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis,Vector3d.ZAxis,
			                                          p1,p1p2v.DivideBy(p1p2v.Length),p1p2v.GetPerpendicularVector().MultiplyBy(side*flip),Vector3d.ZAxis);
			M*=Matrix3d.Displacement(Vector3d.YAxis*first.Y*-1*(offsetMode^1));
			
			
			tail.TransformBy(M);
		}
	}
}
