/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 01.09.2020
 * Время: 9:22
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.EditorInput;
using PKUserTools.Utilities;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of BendLine.
	/// </summary>
	class BendLine:FunctionClass
	{
		public BendLine()
		{
		}
		public override void Execute()
		{
			Point3d o,s,e;
			Polyline pl;
			Line ln;
			
			var sset = Input.Objects("Выберите полилинию"); if(Input.StatusBad) return;
			if(sset.Count!=1)
			{
				Tweet("Выберите один объект");
				return;
			}
			using(var th = new TransactionHelper())
			{
				pl=th.ReadObject(sset[0].ObjectId) as Polyline;
				ln=th.ReadObject(sset[0].ObjectId) as Line;
				if(pl==null)
				{
					if(ln==null)
					{
						Tweet("Выберите полилинию или отрезок!");
						return;
					}
				}
			}
			o = Input.Point("Точка изгиба на линии"); if(Input.StatusBad) return;
			s = Input.Point("Точка на полилинии"); if(Input.StatusBad) return;
			e = Input.Point("Точка угла изгиба"); if(Input.StatusBad) return;
			
			
			using(var th = new TransactionHelper())
			{
				pl=th.EditObject(sset[0].ObjectId) as Polyline;
				
				if(pl!=null)
				{
					BendPolyline(o,s,e,ref pl);
				}
				else
				{
					if(ln==null)
					{
						Tweet("Выберите полилинию или отрезок!");
						return;
					}
					ln=th.EditObject(sset[0].ObjectId) as Line;
					th.WriteObject(BendSingleLine(o,s,e,ln));
					ln.Erase();
				}
			}
		}
		void BendPolyline(Point3d origin, Point3d fromp, Point3d to, ref Polyline pl)
		{
			Matrix3d M= BendMatrix(origin,fromp,to);
			
			bool forward = true;
			int segment=CheckSegment(origin,fromp,ref pl,out forward);
			
			int start,end;
			Point3d temp_p; //временная точка для чтения вершины и преобразования ее
			
			if(forward)
			{
				start=segment+1; end=pl.NumberOfVertices-1; //номера изменяемых вершин включительно
			}
			else
			{
				start=0; end = segment;
			}
			
			for(int i=start;i<=end;i++)
			{
				temp_p=pl.GetPoint3dAt(i);
				temp_p = temp_p.TransformBy(M);
				pl.SetPointAt(i,UT.from3dto2d(temp_p));
			}
			pl.AddVertexAt(segment+1,UT.from3dto2d(origin),pl.GetBulgeAt(segment),pl.GetStartWidthAt(segment),pl.GetEndWidthAt(segment));
			
		}
		Polyline BendSingleLine(Point3d origin, Point3d fromp, Point3d to, Line ln)
		{
			
			Polyline result = new Polyline();
			result.SetPropertiesFrom(ln);
			result.AddVertexAt(0,UT.from3dto2d(ln.StartPoint),0,0,0);
			result.AddVertexAt(1,UT.from3dto2d(ln.EndPoint),0,0,0);
			
			BendPolyline(origin,fromp,to, ref result);
			
			return result;
			
		}
		int CheckSegment(Point3d origin, Point3d fromp, ref Polyline pl, out bool forward)
		{
			forward=true;
			int segment=0;
			
			Point3d back,forw;
			Vector3d back_v,forw_v,from_v;
			double angle_forw_backw, angle_from_back;
			
			for(int i=0;i<pl.NumberOfVertices;i++)
			{
				back = pl.GetPoint3dAt(i);
				forw = pl.GetPoint3dAt(i+1);
				
				back_v = back.GetAsVector().Subtract(origin.GetAsVector());
				forw_v = forw.GetAsVector().Subtract(origin.GetAsVector());
				from_v = fromp.GetAsVector().Subtract(origin.GetAsVector());
				
				angle_forw_backw = forw_v.GetAngleTo(back_v);
				angle_from_back = from_v.GetAngleTo(back_v);
				
				if(angle_forw_backw>Math.PI*0.5)
				{
					segment=i;
					if(angle_from_back>=Math.PI*0.5&&angle_from_back<=Math.PI*1.5)
					{
						forward=true;
					}
					else
					{
						if(angle_from_back<Math.PI*0.5||angle_from_back>Math.PI*1.5)
						{
							forward=false;
						}
						else
						{
							Tweet("Что-то не так с углами "+ angle_forw_backw + " " + angle_from_back);
						}
					}
					
					break;
				}
				
			}
			return segment;
		}
		
		Matrix3d BendMatrix(Point3d origin, Point3d fromp, Point3d to)
		{
			Vector3d oldx = fromp.GetAsVector().Subtract(origin.GetAsVector());
			Vector3d newx = to.GetAsVector().Subtract(origin.GetAsVector());
			
			Plane xoy = new Plane(Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis);
			
			double angle = newx.AngleOnPlane(xoy)-oldx.AngleOnPlane(xoy);
			
			return Matrix3d.Rotation(angle,Vector3d.ZAxis,origin);
		}
	}
}
