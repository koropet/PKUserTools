/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 15.10.2018
 * Время: 16:08
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
using Autodesk.AutoCAD.Colors;

using XL=Microsoft.Office.Interop.Excel;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;

using System.Windows.Forms;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of RebarDrawing.
	/// </summary>
	class RebarDrawing:FunctionClass
	{
		static LinetypeTableRecord dashed;
		static LayerTableRecord[] layers;
		static string[] layer_names=
		{"ПК_О_АIII","ПК_О_АI","ПК_О_АI","ПК_О_КонстрОснов","ПК_О_ВыноскиПозиции","ПК_О_Размеры"};
		
		static double thickness=500;
		static double offset_up=45;
		static double offset_down=45;
		static double dots_up=65;
		static double dots_down=65;
		
		static List<Tuple<double,Color>> dot_data1;
		static List<Tuple<double,Color>> dot_data2;
		
		/// <summary>
		/// Неужели, так можно было
		/// </summary>
		static RebarDrawing()
		{
			Messaging.Tweet("Вызвали статический конструктор");
			
		}
		
		public RebarDrawing()
		{
			Tweet("Начинаем распознавать слои");
			
			if(!InitLayers())
			{
				Tweet("Не хватает слоев! Программа не будет продолжена. Для корректной работы необходим полный шаблон ПК");
				return;
			}
			if(!InitLinetype()) return;
			
			Tweet("Пробуем нарисовать плиту");
			using(var th=new TransactionHelper())
			{
				var sset_line1 = Input.Objects("Выберите линию сечения нижней арматуры",new string[]
				                               {"SEttings","ПАраметры", "LAyers","cЛОи"},
				                               SetParams
				                              ); if(Input.StatusBad) return;
				var sset_lines1 = Input.Objects("Выберите нижние арматурные стержни"); if(Input.StatusBad) return;
				var sset_line2 = Input.Objects("Выберите линию сечения верхней арматуры"); if(Input.StatusBad) return;
				var sset_lines2 = Input.Objects("Выберите верхние арматурные стержни"); if(Input.StatusBad) return;
				
				var ln1=th.ReadObject(sset_line1[0].ObjectId) as Line;
				var ln2=th.ReadObject(sset_line2[0].ObjectId) as Line;
				var lines1=th.ReadObjects(sset_lines1);
				var lines2=th.ReadObjects(sset_lines2);
				
				var colors1=lines1.Select((ln)=>ln.Color).ToList();
				var colors2=lines2.Select((ln)=>ln.Color).ToList();
				
				dot_data1=lines1.Select((ln)=>{
				                        	return new Tuple<double,Color>(UT.IntersectWith(ln,ln1)[0].X,ln.Color);}
				                       ).ToList();
				dot_data2=lines2.Select((ln)=>{
				                        	return new Tuple<double,Color>(UT.IntersectWith(ln,ln2)[0].X,ln.Color);}
				                       ).ToList();
				
				th.WriteObjects(Slab(thickness,ln1.StartPoint.X,ln1.EndPoint.X,offset_up,offset_down,dots_up,dots_down));
			}
		}
		
		/// <summary>
		/// Загрузка слоев если они есть в чертеже. Имена слоев рассчитаны на стандартный шаблон отдела ПК (в будущем можно более гибко настроить).
		/// </summary>
		bool InitLayers()
		{
			using(var tr=acDoc.TransactionManager.StartTransaction())
			{
				var lt=tr.GetObject(acCurDb.LayerTableId,OpenMode.ForRead) as LayerTable;
				layers=new LayerTableRecord[layer_names.GetLength(0)];
				for(int i=0;i<layer_names.GetLength(0);i++)
				{
					try
					{
						layers[i]=tr.GetObject(lt[layer_names[i]],OpenMode.ForRead) as LayerTableRecord;
					}
					catch(Exception)
					{
						Tweet("Не удалось получить слой");
						return false;
					}
				}
				
			}
			return true;
		}
		
		bool InitLinetype()
		{
			using(var tr=acDoc.TransactionManager.StartTransaction())
			{
				var ltt=tr.GetObject(acCurDb.LinetypeTableId,OpenMode.ForRead) as LinetypeTable;
				try
				{
					dashed=tr.GetObject(ltt["DASHED"], OpenMode.ForRead) as LinetypeTableRecord;
				}
				catch(Exception)
				{
					Tweet("Не удалось загрузить типы линий");
					return false;
				}
			}
			return true;
		}
		
		/// <summary>
		/// Рисование точки из полилинии
		/// </summary>
		/// <param name="diam">диаметр точки в мм</param>
		/// <returns></returns>
		static Polyline Donut(double diam)
		{
			
			var donut=new Polyline();
			donut.SetDatabaseDefaults();
			donut.AddVertexAt(0, new Point2d(diam/4,0),1,diam/2,diam/2);
			donut.AddVertexAt(1, new Point2d(diam/-4,0),1,diam/2,diam/2);
			donut.Closed=true;
			
			return donut;
		}
		
		/// <summary>
		/// Рисуем параметрическую шпильку
		/// </summary>
		/// <param name="h1">отступ снизу</param>
		/// <param name="h2">отступ сверху</param>
		/// <param name="t">хвост</param>
		/// <param name="b">ширина</param>
		/// <param name="h">высота жб элемента</param>
		/// <returns></returns>
		static Polyline Pin(double h,double h1, double h2,double t, double b)
		{
			var pn=new Polyline();
			pn.AddVertexAt(0,new Point2d(b/2,h1+t),0,0,0);
			pn.AddVertexAt(1,new Point2d(b/2,h1),-1,0,0);
			pn.AddVertexAt(2,new Point2d(b/-2,h1),0,0,0);
			pn.AddVertexAt(3,new Point2d(b/-2,h-h2),-1,0,0);
			pn.AddVertexAt(4,new Point2d(b/2,h-h2),0,0,0);
			pn.AddVertexAt(5,new Point2d(b/2,h-h2-t),0,0,0);
			pn.SetDatabaseDefaults();
			return pn;
		}
		
		static List<Entity> Slab(double thickness, double X0,double X1,
		                         double offset1,double offset2,
		                         double arm_1,double arm_2
		                        )
		{
			var g=new List<Entity>();
			var Upline=new Line(new Point3d(X0,thickness,0),new Point3d(X1,thickness,0));
			var Downline=new Line(new Point3d(X0,0,0),new Point3d(X1,0,0));
			var Uparm=new Line(new Point3d(X0,thickness-offset1,0),new Point3d(X1,thickness-offset1,0));
			var Downarm=new Line(new Point3d(X0,offset2,0),new Point3d(X1,offset2,0));
			
			Upline.SetDatabaseDefaults();
			Downline.SetDatabaseDefaults();
			Uparm.SetDatabaseDefaults();
			Downarm.SetDatabaseDefaults();
			
			Upline.Layer=layers[3].Name;
			Downline.Layer=layers[3].Name;
			
			Uparm.Layer=layers[2].Name;
			Downarm.Layer=layers[2].Name;
			
			g.Add(Upline); g.Add(Downline); g.Add(Uparm); g.Add(Downarm);
			
			
			
			var M=new Matrix3d();
			
			for(int i=0;i<dot_data1.Count;i++)
			{
				var d1=Donut(30);
				var d2=Donut(30);
				
				
				var p=Pin(thickness,arm_1-15,arm_2-15,30,50);
				
				
				M=Matrix3d.Displacement(new Vector3d(dot_data1[i].Item1,0,0));
				
				p = p.GetTransformedCopy(M) as Polyline;
				
				M=Matrix3d.Displacement(new Vector3d(dot_data1[i].Item1,arm_1,0));
				d1 = d1.GetTransformedCopy(M) as Polyline;
				
				M=Matrix3d.Displacement(new Vector3d(dot_data2[i].Item1,thickness-arm_2,0));
				d2 = d2.GetTransformedCopy(M) as Polyline;
				
				p.Layer=layers[1].Name;
				if(i%2==0)
				{
					p.Linetype=dashed.Name;
				}
				
				d1.Layer=layers[0].Name;
				d2.Layer=layers[0].Name;
				
				d1.Color=dot_data1[i].Item2;
				d2.Color=dot_data2[i].Item2;
				
				g.Add(p); g.Add(d1); g.Add(d2);
			}
			
			return g;
			
		}
		
		void SetParams(object s,SelectionTextInputEventArgs e)
		{
			if(e.Input=="SEttings")
			{
				try
				{
					thickness=Input.Double("Толщина плиты"); if(Input.StatusBad) return;
					offset_up=Input.Double("Отступ арматуры сверху"); if(Input.StatusBad) return;
					offset_down=Input.Double("Отступ арматуры снизу"); if(Input.StatusBad) return;
					dots_up=Input.Double("Арматурные точки сверху"); if(Input.StatusBad) return;
					dots_down=Input.Double("Арматурные точки снизу"); if(Input.StatusBad) return;
				}
				catch(Exception)
				{
					Tweet("Не удалось установить параметры. Продолжаем со старыми.");
				}
			}
			else
			{
				Tweet("Введите слои для арматуры");
				var sset_pins=Input.Objects("Выберите объект для слоя шпилек"); if(Input.StatusBad) return;
				var sset_arm=Input.Objects("Выберите объект для слоя поперечной арматуры"); if(Input.StatusBad) return;
				
				using(var th=new TransactionHelper())
				{
					layer_names[1]=(th.ReadObject(sset_pins[0].ObjectId) as Entity).Layer;
					layer_names[2]=(th.ReadObject(sset_arm[0].ObjectId) as Entity).Layer;
					if(!InitLayers()) return;
				}
			}
		}
		
	}
}
