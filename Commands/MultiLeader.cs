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
	class MultiLeader : FunctionClass
	{
		List<Entity> geometry;
		//точки привязки выносок
		Point3d leader_point, p1, p2;

		MText mt;
		//текст
		static string text= "хх";

		//для подавления правых или левых линий выносок
		public bool SingleLeft = false;
		public bool SingleRight = false;

		GroupLeaderSide gls = GroupLeaderSide.Left;

		//степень сужения трапеции
		public double cline = 0.4;
		//привязка текста
		Point3d mtp;

		//точки полилинии
		Point3d[] points;

		//текущий аннотативный масштаб
		AnnotationScale asc;

		/// <summary>
		/// Запоминаем в какую сторону делаем выноску
		/// </summary>
		static GroupLeaderSide group_leader_side = GroupLeaderSide.Outside;

		//уклон
		static double s_cline = 0.4;
		
		//смещение выноски относительно объекта
		static double offset=6;

		//прочитанные объекты
		List<Entity> objects = new List<Entity>();

		//центры объектов, к которым привязываем точки
		List<Point3d> centers = new List<Point3d>();

		public override void Execute()
		{
			base.Execute();

			var sset=Input.Objects("Выберите объекты для нанесения выносок или ",
			                       new string[]
			                       {
			                       	"Angle", "Уклон",
			                       	"oFFset","cМЕщение",
			                       	"Text","ТТекст"
			                       },
			                       new SelectionTextInputEventHandler(KeywordInput));
			if (Input.StatusBad) return;
			
			string[] Keywords=null;

			if (sset.Count == 1)
			{
				Keywords = new string[]
				{
					"LEft","ЛЕво",
					"RIght","ПРаво",
					"INside","внуТРи",
					"OUtside","сНАружи"
				};
			}
			
			leader_point = Input.Point("\nУкажите точку, через которую пройдет выносная линия",Keywords);
			
			if (Input.StatusKeyword)
			{
				SideInput(Input.StringResult);
				
				leader_point = Input.Point("\nУкажите точку, через которую пройдет выносная линия");
			}
			if (Input.StatusBad) return;

			//достаем объекты
			using (Utilities.TransactionHelper th = new Utilities.TransactionHelper())
			{
				objects = th.ReadObjects(sset);

				Line dir = new Line(Point3d.Origin, new Point3d(1000, 0, 0)); //для работы функции нахождения центра
				bool hascenter;
				for (int i = 0; i < objects.Count; i++)
				{
					Point3d cen = UT.GetCenter(objects[i], dir, Point3d.Origin, out hascenter);

					if (hascenter) centers.Add(cen);
				}
				if (centers.Count > 1)//если больше 1 объекта
				{
					dir = new Line(centers[0], centers[centers.Count - 1]);

					//сортируем
					PointSorter ps = new PointSorter();
					ps.direction = dir;

					//сортируем по направлению
					centers.Sort(ps);
				}
				//создаем объекты
				if (centers.Count > 1)
				{
					GroupLeader(centers[0], centers[centers.Count - 1], leader_point);
				}
				else if (centers.Count == 1)
				{
					GroupLeader(centers[0], leader_point, group_leader_side);
				}
				else return;//если объектов 0 то нам здесь нечего делать
				
				cline = s_cline;

			}
			Make();
			

			Line p1p2=new Line(p1,p2);
			bool subleader = false;
			if (p1p2.Angle > Math.PI*1/4 && p1p2.Angle <  Math.PI *3/4|| p1p2.Angle>Math.PI*5/4&&p1p2.Angle<Math.PI*7/4)
			{
				subleader = true;
			}
			else
			{
				geometry.Add(mt);
			}
			using(TransactionHelper th=new TransactionHelper())
			{
				th.WriteObjects(geometry);
			}
			//нужно 2 разных транзакции чтобы видеть результат работы предыдущей части
			if (subleader)
			{
				using (TransactionHelper th = new TransactionHelper())
				{
					PrepareSubLeader();
					th.WriteObject(geometry.Last());
				}
			}
		}
		void KeywordInput(object s, SelectionTextInputEventArgs e)
		{
			if(e.Input=="Angle")
			{
				s_cline=Input.Double("Введите степень сужения трапеции"); if(Input.StatusBad) return;
			}
			else if(e.Input=="oFFset")
			{
				offset=Input.Double("Введите смещение выноски в мм:"); if(Input.StatusBad) return;
			}
			else if(e.Input=="Text")
			{
				text=Input.Text("\nВведите текст для выносок");
			}
		}
		void SideInput(string key)
		{
			switch (key)
			{
				case "LEft":
					{
						group_leader_side = GroupLeaderSide.Left;
						break;
					}
				case "RIght":
					{
						group_leader_side = GroupLeaderSide.Right;
						break;
					}
				case "INside":
					{
						group_leader_side = GroupLeaderSide.Inside;
						break;
					}
				case "OUtside":
					{
						group_leader_side = GroupLeaderSide.Outside;
						break;
					}
				default:
					{

						//мало ли что
						return;
					}
			}
		}

		//Конструктора групповой выноски. Здесь не в роли конструктора, а в роли методов

		/// <summary>
		/// Создание объекта групповой выноски
		/// </summary>
		/// <param name="first">Начальная точка</param>
		/// <param name="second">Конечная точка</param>
		/// <param name="leader_point">Выбор, с какой стороны будет выноска</param>
		public void GroupLeader(Point3d first, Point3d second, Point3d leader_point)
		{
			p1 = first;
			p2 = second;
			this.leader_point = leader_point;

			Initialise();
		}

		/// <summary>
		/// Создание объекта выноски с одного объекта
		/// </summary>
		/// <param name="singlepoint">Привязка объекта</param>
		/// <param name="leader_point">Угол выноски</param>
		/// <param name="side">Право или лево</param>
		public void GroupLeader(Point3d singlepoint, Point3d leader_point, GroupLeaderSide side)
		{
			p1 = singlepoint;
			p2 = singlepoint;

			this.leader_point = leader_point;

			gls = side;

			Initialise();
			
		}
		void Initialise()
		{
			geometry = new List<Entity>();

			asc = UT.GetAnnoScale("CANNOSCALE");

		}

		/// <summary>
		/// Создает и настраивает мультитекст.
		/// </summary>
		void PrepareMtext(double angle)
		{
			mt = new MText();
			mt.SetDatabaseDefaults();
			mt.Contents = text;

			//междустрочный интервал
			mt.LineSpacingStyle = LineSpacingStyle.Exactly;
			mt.LineSpacingFactor = 1.0d;

			mt.Location = mtp;
			mt.TextStyleId = acCurDb.Textstyle;
			mt.TextHeight = 3 / asc.Scale;
			mt.Attachment = AttachmentPoint.BottomCenter;
			mt.Annotative = AnnotativeStates.True;
			mt.Rotation = angle;
			mt.Width = 5 / asc.Scale;
			mt.Height = 5 / asc.Scale;

			
		}

		/// <summary>
		/// Выноска, которая создается если группа объектов вертикальная
		/// </summary>
		void PrepareSubLeader()
		{
			var pm=Input.Point("Выберите точку для дополнительной выноски");
			if (Input.StatusBad) return;

			MLeader add_mleader = new MLeader();
			add_mleader.SetDatabaseDefaults();
			add_mleader.ContentType = ContentType.MTextContent;
			mt.Rotation = 0;
			mt.Location = pm;
			add_mleader.MText = mt;

			int idx = add_mleader.AddLeaderLine(pm);
			add_mleader.SetFirstVertex(idx, mtp);

			geometry.Add(add_mleader);
		}

		/// <summary>
		/// Создает и настраивает полилинию на основе рассчитаных точек
		/// </summary>
		void PreparePolyline()
		{
			Polyline pl = new Polyline();
			for (int i = 0; i < points.GetLength(0); i++)
			{
				pl.AddVertexAt(i, points[i].to2d(), 0, 0, 0);

			}
			pl.SetDatabaseDefaults();

			pl.LineWeight = LineWeight.LineWeight018;

			geometry.Add(pl);


		}

		/// <summary>
		/// по умолчанию отступ выноски от объекта (защитный слой)
		/// </summary>
		static double leader_offset = 50;

		/// <summary>
		/// самая первая реализация групповой выноски, в виде трапеции
		/// </summary>
		void MakeTaper()
		{


			double leader_offset2;


			leader_offset2 = leader_offset + offset / asc.Scale;//выносим за 6мм за объект по умолчанию (переменная offset теперь задается)
			//TODO уровни

			Vector3d vp1p2 = p2.GetAsVector().Subtract(p1.GetAsVector());
			Vector3d norm = vp1p2.GetPerpendicularVector();

			Vector3d vp2o = leader_point.GetAsVector().Subtract(p2.GetAsVector());
			Vector3d vp1o = leader_point.GetAsVector().Subtract(p1.GetAsVector());

			//корректируем нормаль в сторону выбранной точки выноски
			double ang = norm.GetAngleTo(vp1o, Vector3d.ZAxis);
			if (ang > Math.PI / 2 && ang < Math.PI * 3 / 2)
			{
				norm = norm.Negate();
			}

			Plane pl = new Plane(p1, norm);

			Point3d o = leader_point.OrthoProject(pl);

			Vector3d p2o = o.GetAsVector().Subtract(p2.GetAsVector());
			Vector3d p1o = o.GetAsVector().Subtract(p1.GetAsVector());

			double leader_offset3 = leader_offset2 * cline;

			Vector3d p1h = p1o.MultiplyBy(leader_offset3 / p1o.Length);
			Vector3d p2h = p2o.MultiplyBy(leader_offset3 / p2o.Length);




			//смещение точек привязки - получаем верхние углы выноски
			Vector3d vp1p3 = norm.MultiplyBy(leader_offset2).Add(p1h);
			Vector3d vp2p4 = norm.MultiplyBy(leader_offset2).Add(p2h);


			Point3d p3 = p1.Add(vp1p3);
			Point3d p4 = p2.Add(vp2p4);




			//случаи с подавлением
			double Offset4 = 6 / asc.Scale;//сделаем ширину выноски 6мм TODO настроить под текст

			if (SingleLeft)
			{
				Point3d p3_2 = p3.Add(vp1p2.MultiplyBy(Offset4 / vp1p2.Length));
				points = new Point3d[3] { p1, p3, p3_2 };
				mtp = UT.GetMiddle(p3_2, p4);
			}
			else if (SingleRight)
			{
				Point3d p4_2 = p4.Subtract(vp1p2.MultiplyBy(Offset4 / vp1p2.Length));
				points = new Point3d[3] { p4_2, p4, p2 };
				mtp = UT.GetMiddle(p3, p4_2);
			}
			else//общий случай
			{

				points = new Point3d[4] { p1, p3, p4, p2 };
				//средняя точка выноски - привязываем к ней мультитекст

				mtp = UT.GetMiddle(p3, p4);
			}
			//коррекция угла поворота текста
			if (p1.X > p2.X)
			{
				vp1p2 = vp1p2.Negate();
			}
			PrepareMtext(-1 * vp1p2.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis));

			PreparePolyline();
		}
		void MakeSingle()
		{
			double Offset4 = 5 / asc.Scale;
			Vector3d vp1_l = leader_point.GetAsVector().Subtract(p1.GetAsVector());
			Point3d p3;

			switch (gls)
			{
				case GroupLeaderSide.Right:
					{
						p3 = leader_point.Add(Vector3d.XAxis.MultiplyBy(Offset4));
						break;
					}
				case GroupLeaderSide.Left:
					{

						p3 = leader_point.Add(Vector3d.XAxis.MultiplyBy(Offset4 * -1));
						break;
					}
				case GroupLeaderSide.Inside:
					{
						double dx = p1.X - leader_point.X;
						p3 = leader_point.Add(Vector3d.XAxis.MultiplyBy(Offset4 * Math.Sign(dx)));
						break;
					}
				case GroupLeaderSide.Outside:
					{
						double dx = leader_point.X - p1.X;
						p3 = leader_point.Add(Vector3d.XAxis.MultiplyBy(Offset4 * Math.Sign(dx)));
						break;
					}

				default:
					{
						return;
					}
			}
			mtp = UT.GetMiddle(p3, leader_point);
			points = new Point3d[3] { p1, leader_point, p3 };

			PrepareMtext(0);
			PreparePolyline();
		}
		void Make()
		{
			if (p1 == p2) MakeSingle();
			else MakeTaper();
		}
		
		/// <summary>
		/// Мультивыноска из объектов со стрелками на одной линии
		/// </summary>
		public void MakeArrowsInline()
		{
			base.Execute();
			asc = UT.GetAnnoScale("CANNOSCALE");
			var objects=new List<Entity>();
			var points=new List<Point3d>();
			Point3d p1,p2;
			Line p1p2;
			
			Vector3d offset=new Vector3d(2,3.6,0);
			
			var sset=Input.Objects("Выберите объекты"); if(Input.StatusBad) return;
			
			
			p1=Input.Point("Выберите точку мультивыноски"); if(Input.StatusBad)return;
			p2=Input.Point("Выберите точку для задания направления"); if(Input.StatusBad)return;
			
			p1p2=new Line(p1,p2);
			Tweet("\nНачинаем транзакцию");
			using(var th=new TransactionHelper())
			{
				objects=th.ReadObjects(sset);
				
				Tweet("\nНачинаем поиск точек");
				foreach(Entity ent in objects)
				{
					var pt_arr=UT.IntersectWith(p1p2,ent);
					if(pt_arr.Count>0) points.Add(pt_arr[0]);
				}
				
				Tweet("\nНачинаем подготовку текста");
				
				mtp=p1;
				text="хх";
				PrepareMtext(0);
				
				Tweet("\nНачинаем подготовку выноски");
				MLeader mleader = new MLeader();
				mleader.SetDatabaseDefaults();
				mleader.ContentType = ContentType.MTextContent;
				
				mt.TransformBy(Matrix3d.Displacement(offset.DivideBy(asc.Scale)));
				
				mleader.MText = mt;
				
				Tweet("\nДобавляем линии");
				foreach(Point3d ptt in points)
				{
					int idx = mleader.AddLeaderLine(p1);
					mleader.SetFirstVertex(idx, ptt);
				}
				Tweet("\nЗаписываем объекты");
				th.WriteObject(mleader);
			}
		}
	}


	/// <summary>
	/// Сторона в которую делаем выноску
	/// </summary>
	public enum GroupLeaderSide
	{
		Left,
		Right,
		Inside,
		Outside,
	}
}
