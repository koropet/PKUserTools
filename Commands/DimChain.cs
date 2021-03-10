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

using PKUserTools.EditorInput;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;

namespace PKUserTools.Commands
{
	class DimChain:FunctionClass
	{
		static double Dim_tolerance = 1d;
		static Line DimDirection;
		bool Auto_direction = true;
		static bool UniteDimms=true;
		static bool tails=true;//если крайние размеры на линии, то истинно

		Point3d dim_line_point;

		Line Dim_line;

		Plane pl;

		AnnotationScale asc;

		List<Entity> dimensions = new List<Entity>();

		double current_dimm = 0;
		double next_dim = 0;
		int start_dim = 0;

		bool hascenter;

		//прочитанные объекты
		List<Entity> objects = new List<Entity>();

		//центры объектов, к которым привязываем точки
		List<Point3d> centers = new List<Point3d>();
		List<Point3d> centers_projected = new List<Point3d>();

		public override void Execute()
		{
			base.Execute();

			DimDirection = new Line(Point3d.Origin, new Point3d(1000, 0, 0));

			var oi_sset = Input.Objects(
				
				keywords: new string[]
				{
					"HOrisontal", "ГОризонтальные",
					"VErtical", "ВЕртикальные",
					"DIrection", "НАправление",
					"TOLerance", "тоЧНость",
					"STeps","Шаги",
					"TAils","Хвостовые"
				},
				message: "Выберите объекты для нанесения размеров",
				keywordinput: new SelectionTextInputEventHandler(KeywordInput))
				;
			if(Input.StatusBad)return;
			
			dim_line_point = Input.Point("Укажите точку, через которую пройдет размерная линия"); if(Input.StatusBad)return;

			using (TransactionHelper th = new TransactionHelper())
			{
				objects = th.ReadObjects(oi_sset);
				FindDirection();
				PrepareDimline();
				FindCenters();
				FindProjectedCenters();
				SortCenters();
				CreateDimms();

				th.WriteObjects(dimensions);
			}
		}
		void KeywordInput(object sender, SelectionTextInputEventArgs e)
		{
			//надо обработать ключевые слова
			if (e.Input == "HOrisontal")//горизонтальные
			{
				DimDirection = new Line(Point3d.Origin, new Point3d(1000, 0, 0));//хорошо бы регулировать Х
				Auto_direction = false;
			}
			if (e.Input == "VErtical")//вертикальные
			{
				DimDirection = new Line(Point3d.Origin, new Point3d(0, 1000, 0));
				Auto_direction = false;
			}
			if (e.Input == "DIrection")//направление
			{
				var li_sset=Input.Objects("Выберите линию, указывающую направление");
				if(Input.StatusBad)return;

				using (TransactionHelper th=new TransactionHelper())
				{
					Entity ent=th.ReadObjects(li_sset)[0];
					if(!(ent is Line))
					{
						Alert("Ввели не линию!");
						return;
					}
					DimDirection = ent as Line;
				}
				Auto_direction = false;
			}
			if (e.Input == "TOLerance")
			{
				Dim_tolerance=Input.Double("Введите точность измерения размеров"); if(Input.StatusBad) return;
			}
			if (e.Input == "STeps")
			{
				//Смена режима объединения шагов
				UniteDimms=!UniteDimms;
			}
			if (e.Input == "TAils")
			{
				PromptKeywordOptions pko=new PromptKeywordOptions("\nВведите режим размещения хвостовых размеров");
				pko.Keywords.Add("Leader","ВЫноска");
				pko.Keywords.Add("oNLine","Налинии");
				PromptResult res=acEd.GetKeywords(pko);
				if(res.Status!=PromptStatus.OK) return;
				
				if(res.StringResult=="Leader")
				{
					tails=false;
				}
				else if(res.StringResult=="oNLine")
				{
					tails=true;
				}
			}
			
		}
		void FindDirection()
		{
			//автоопределение направления (горизонтально/вертикально)
			if (Auto_direction)
			{
				double minx = double.PositiveInfinity;
				double maxx = double.NegativeInfinity;

				double miny = double.PositiveInfinity;
				double maxy = double.NegativeInfinity;
				//надо найти центры по дефолтному направлению и вычислить границы
				for (int i = 0; i < objects.Count; i++)
				{
					Point3d cen = UT.GetCenter(objects[i], DimDirection, dim_line_point, out hascenter);

					if (hascenter)
					{

						if (cen.X < minx) minx = cen.X;
						if (cen.X > maxx) maxx = cen.X;

						if (cen.Y < miny) miny = cen.Y;
						if (cen.Y > maxy) maxy = cen.Y;
					}
				}
				Point3d dd = dim_line_point;//простое название

				if ((dd.X < minx || dd.X > maxx) && (dd.Y < maxy) && (dd.Y > miny)) DimDirection = new Line(Point3d.Origin, new Point3d(0,1000, 0));
				else DimDirection = new Line(Point3d.Origin, new Point3d(1000, 0, 0));

			}
			Auto_direction = true;//обнуляем чтоб на следующий раз можно было авто
		}
		void PrepareDimline()
		{

			Dim_line = new Line(dim_line_point, dim_line_point.Add(DimDirection.Delta));
			//нашли размерную линию, прибавили вектор направления

			//проекция уже на размерную линию
			pl = new Plane(Dim_line.StartPoint, Dim_line.Delta.GetPerpendicularVector());
		}
		void FindCenters()
		{
			//находим центры
			for (int i = 0; i < objects.Count; i++)
			{
				Point3d cen = UT.GetCenter(objects[i], Dim_line, dim_line_point, out hascenter);

				if (hascenter) centers.Add(cen);
			}
		}
		void FindProjectedCenters()
		{
			for (int i = 0; i < centers.Count; i++)
			{
				Point3d pc = centers[i].OrthoProject(pl);
				centers_projected.Add(pc);
			}
		}
		void SortCenters()
		{
			PointSorter ps = new PointSorter();
			ps.direction = DimDirection;
			//сортируем по направлению
			centers.Sort(ps);
			centers_projected.Sort(ps);
		}
		void CreateDimms()
		{
			asc = UT.GetAnnoScale("CANNOSCALE");

			for (int i = 1; i < centers_projected.Count; i++)
			{
				RotatedDimension dim = new RotatedDimension();
				
				
				if (i == 1)
				{
					current_dimm = centers_projected[i].DistanceTo(centers_projected[i - 1]);
				}

				if (i < centers_projected.Count - 1)
				{
					next_dim = centers_projected[i].DistanceTo(centers_projected[i + 1]);
				}
				else next_dim = 0;

				if (Math.Abs(next_dim - current_dimm) < Dim_tolerance&&UniteDimms)//точность определения размеров
				{
					continue;//пропускаем выносные линии одинаковых размеров, если выбран режим объединения
				}

				//создние размера
				if (i - start_dim > 2)
				{
					//делаем корректировку, чтобы цифры шага и общего размера соответствовали,
					//потому что из-за низкой точности чертежа
					//может возникнуть ситуация типа 5х199=1000
					int count=i-start_dim;
					double dim_max=centers_projected[i].DistanceTo(centers_projected[start_dim]);
					double dim_corrected=dim_max/count;
					
					dim.DimensionText =string.Format("{0}х{1:0}=<>",count, dim_corrected);
					
				}
				if (i - start_dim == 2)
				{
					//случай когда 2 одинаковых

					i--;
				}
				

				if (i == 1)
				{
					dim.XLine2Point = centers[start_dim];
					dim.XLine1Point = centers[i];
					if(tails)
					{
						dim.Dimtmove = 0;
					}
					else
						dim.Dimtmove=1;
				}
				else if (i == centers_projected.Count - 1)
				{
					dim.XLine1Point = centers[start_dim];
					dim.XLine2Point = centers[i];
					if(tails)
					{
						dim.Dimtmove = 0;
					}
					else
						dim.Dimtmove=1;
				}
				else
				{
					dim.XLine2Point = centers[start_dim];
					dim.XLine1Point = centers[i];
					dim.Dimtmove = 1;
				}
				

				dim.DimLinePoint = centers_projected[i];
				dim.Rotation = DimDirection.Angle;
				dim.DimensionStyle = acCurDb.Dimstyle;
				dim.Annotative = AnnotativeStates.True;


				//сужаем тексты
				if (dim.Measurement < (asc.DrawingUnits / asc.PaperUnits) * 9 && dim.Measurement > (asc.DrawingUnits / asc.PaperUnits) * 4)
				{
					if(dim.DimensionText==null | dim.DimensionText=="")
					{
						dim.DimensionText = @"{\W0.6;<>}";
					}
					else
					{
						dim.DimensionText=@"{\W0.6;" + dim.DimensionText + @"}";
					}
				}
				

				//если размер маленький, сужаем


				dimensions.Add(dim);

				if (i < centers_projected.Count - 1)
				{
					current_dimm = centers_projected[i + 1].DistanceTo(centers_projected[i]);
				}
				start_dim = i;

			}
		}
	}
}
