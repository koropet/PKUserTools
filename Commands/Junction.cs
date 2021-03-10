/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 27.08.2019
 * Время: 15:35
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
using Autodesk.Private.InfoCenter;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using PKUserTools.EditorInput;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of Junction.
	/// </summary>
	public class Junction
	{	static Dictionary<string,Line> sourceTails = new Dictionary<string, Line>()
		{
			{"1:15", SimpleLine(0,0,3,1)},
			{"1:20", SimpleLine(0,0,3,1)},
			{"1:25" , SimpleLine(0,0,3,1)},
			{"1:40", SimpleLine(0,0,2.5,1)},
			{"1:50", SimpleLine(0,0,2,1)},
			{"1:75", SimpleLine(0,0,2,1)},
			{"1:100", SimpleLine(0,0,1.5,0.8)},
		};
		static Line SimpleLine(double x1,double y1, double x2, double y2)
		{
			return new Line(new Point3d(x1,y1,0),new Point3d(x2,y2,0));
		}
		
		static public void CommandResult()//(Entity A, Point2d place)
		{
			Point2d location = Input.Point("Enter loction").to2d(); if(Input.StatusBad) return;
			Point2d user_direction = Input.Point("Enter direction").to2d(); if(Input.StatusBad) return;
			
			Vector2d dir = user_direction-location;
			
			Vector2d perp = dir.GetPerpendicularVector();
			
			using(var th = new TransactionHelper())
			{
				var anno_scale = UT.GetAnnoScale("CANNOSCALE");
				
				var left = Tail(dir,anno_scale,location+dir, LeftRight.Left);
				var right = Tail(dir,anno_scale,location, LeftRight.Right);
				
				th.WriteObject(left);
				th.WriteObject(right);
			}
		}
		static Line Tail(Vector2d direction, AnnotationScale scale, Point2d location, LeftRight leftright)
		{
			Line source;
			try
			{
				source = sourceTails[scale.Name];
			}
			catch(KeyNotFoundException)
			{
				Messaging.Tweet("Хвосты для данного масштаба не поддерживаются");
				return null;
			}
			Matrix3d transform = Matrix3d.Scaling(scale.DrawingUnits/scale.PaperUnits,Point3d.Origin);
			Line transformed = (Line)source.GetTransformedCopy(transform);
			
			var temp_y = transformed.EndPoint.Y;
			var temp_x = transformed.EndPoint.X*(int)leftright;
			
			transformed.EndPoint=new Point3d(temp_x,temp_y,0);
			
			
			Matrix3d orientation = Matrix3d.Rotation(direction.Angle,Vector3d.ZAxis,Point3d.Origin);
			var oriented = (Line)transformed.GetTransformedCopy(orientation);
			Matrix3d offset=Matrix3d.Displacement(new Vector3d(location.X,location.Y,0));
			
			var displaced = (Line)oriented.GetTransformedCopy(offset);
			
			return displaced;
		}
	}
	enum LeftRight:int
	{
		Left = -1,
		Right = 1
	}
}
