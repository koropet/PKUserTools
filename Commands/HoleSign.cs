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
	/// <summary>
	/// Класс, в котором реализуется код создания обозначения проема
	/// </summary>
	class HoleSign : FunctionClass
	{
		/// <summary>
		/// Результат выполнения операции - первая линия обозначения
		/// </summary>
		Line first;
		/// <summary>
		/// 
		/// Результат выполнения операции - вторая линия обозначения
		/// </summary>
		Line second;

		/// <summary>
		/// Отсортированный ввод - линии
		/// </summary>
		List<Line> lines;

		/// <summary>
		/// Отсортированный ввод - замкнутые полилинии
		/// </summary>
		List<Polyline> polylines;

		/// <summary>
		/// Текущее значение смещения в единицах листа
		/// </summary>
		static double currscale = 3;

		/// <summary>
		/// Флаг, сигнализирующий результат операции
		/// </summary>
		bool status_ok = true;

		/// <summary>
		/// Создание линий на основе двух отрезков со смещением центральной вершины
		/// </summary>
		/// <param name="InputFirst">первый отрезок</param>
		/// <param name="InputSecond">второй отрезок</param>
		void Make(Line InputFirst, Line InputSecond)
		{
			Tweet("Делаем два отрезка из двух отрезков");

			Point3d S1;
			Point3d S2;
			Point3d E1;
			Point3d E2;

			Point3d middle;
			Point3d p1;
			Point3d p2;

			//выделяем 3 точки
			S1 = InputFirst.StartPoint;
			E1 = InputFirst.EndPoint;
			S2 = InputSecond.StartPoint;
			E2 = InputSecond.EndPoint;

			if (S1 == S2)
			{
				middle = S1;
				p1 = E1;
				p2 = E2;
			}
			else if (S1 == E2)
			{
				middle = S1;
				p1 = E1;
				p2 = S2;
			}
			else if (E1 == E2)
			{
				middle = E1;
				p1 = S1;
				p2 = S2;
			}
			else if (E1 == S2)
			{
				middle = E1;
				p1 = S1;
				p2 = E2;
			}
			else
			{
				p1 = Point3d.Origin;
				p2 = Point3d.Origin;
				middle = Point3d.Origin;
				Alert("Не совпадают концы отрезков");
				status_ok = false;
				return;
			}

			AnnotationScale asc = UT.GetAnnoScale("CANNOSCALE");

			Vector3d v1 = p1 - middle;
			Vector3d v2 = p2 - middle;
			if ((v1.Length == 0) || (v2.Length == 0))
			{
				Alert("Нулевая длина отрезков");

				return;
			}

			v1 = v1.MultiplyBy(1 / v1.Length);
			v2 = v2.MultiplyBy(1 / v2.Length);

			Vector3d v3 = v1.Add(v2);


			if (v3.Length == 0)
			{
				Alert("Отрезки параллельны");
				return;
			}

			v3 = v3.MultiplyBy(currscale / (asc.Scale * v3.Length));

			//находим длину биссектрисы
			double a, b, c, lb, p;
			Line p1p2 = new Line(p1, p2);
			a = InputFirst.Length;
			c = InputSecond.Length;
			b = p1p2.Length;

			p = (a + b + c) / 2;
			lb = 2 * Math.Sqrt(a * c * p * (p - b)) / (a + c);
			//приводим длину вектора к половине этой длины

			if (v3.Length >= lb * 0.5) v3 = v3.MultiplyBy(lb * 0.5 / v3.Length);

			middle = middle.Add(v3);

			//создаем 2 новые линии
			first = new Line(p1, middle);
			second = new Line(p2, middle);
		}

		/// <summary>
		/// Создание линий на основе полилинии
		/// </summary>
		/// <param name="Poly"></param>
		void Make(Polyline Poly)
		{
			Point3d p1 = new Point3d();
			Point3d p2 = new Point3d();
			Point3d middle = new Point3d();
			Point3d pt, ptt;

			double dist = double.NegativeInfinity;
			//находим ближайшую точку к верхнему правому углу

			for (int i = 0; i < Poly.NumberOfVertices; i++)
			{
				pt = Poly.GetPoint3dAt(i);
				ptt = pt.TransformBy(Matrix3d.Rotation(Math.PI / -4, Vector3d.ZAxis, Point3d.Origin));
				if (ptt.Y > dist)
				{
					dist = ptt.Y;
					if (i > 0) p1 = Poly.GetPoint3dAt(i - 1);
					else if (i == 0) p1 = Poly.GetPoint3dAt(Poly.NumberOfVertices - 1);

					if (i == Poly.NumberOfVertices - 1) p2 = Poly.GetPoint3dAt(0);
					else if (i < Poly.NumberOfVertices - 1) p2 = Poly.GetPoint3dAt(i + 1);

					middle = pt;
				}

			}

			//далее обрабатываем полученные 3 точки
			Make(new Line(p1, middle), new Line(middle, p2));
		}

		/// <summary>
		/// запускаем функцию
		/// </summary>
		public override void Execute()
		{
			using (var lm = new PKLayerManager())
			{
				lm.CommandLayer="ПК_С_ЛинииРазрыва";
				
				base.Execute();
				var sset=Input.Objects(
					
					keywords:  new string[] { "OFfset", "СМещение", },
					message:  "Выберите 2 отрезка на углу или полилинии проемов.",
					keywordinput: new SelectionTextInputEventHandler(LengthInput)
				);
				if (Input.StatusBad)
				{
					return;
				}
				List<Entity> input = new List<Entity>();

				using (TransactionHelper th = new TransactionHelper())
				{
					input = th.ReadObjects(sset);
				}
				if (input == null || input.Count < 1)
				{
					Tweet("Не удалось прочитать объекты");
					return;
				}

				SortLinesAndPolylines(input);
				using (TransactionHelper th = new TransactionHelper())
				{
					for(int i=0;i<lines.Count-1;i+=2)
					{
						Make(lines[i], lines[i+1]);
						if (!status_ok)
						{
							continue;
						}

						Tweet("Записываем линии в базу данных");
						th.WriteObject(first);
						th.WriteObject(second);
					}
					foreach (Polyline pl in polylines)
					{
						Make(pl);
						if (!status_ok)
						{
							continue;
						}

						Tweet("Записываем линии в базу данных");
						th.WriteObject(first);
						th.WriteObject(second);
					}
				}
			}

		}

		/// <summary>
		/// Ввод длины смещения в ответ на событие ввода ключевого слова
		/// </summary>
		void LengthInput(object sender, SelectionTextInputEventArgs e)
		{


			Document acDoc = App.DocumentManager.MdiActiveDocument;
			Database acCurDb = acDoc.Database;

			PromptDoubleResult acSSPrompt = acDoc.Editor.GetDouble("\nВведите смещение угла в единицах листа:");
			currscale = acSSPrompt.Value;
		}
		/// <summary>
		/// Вычленяем из выбора замкнутые полилинии и линии
		/// </summary>
		/// <param name="input">Набор выбора</param>
		void SortLinesAndPolylines(List<Entity> input)
		{
			lines = new List<Line>();
			polylines = new List<Polyline>();

			foreach (Entity ent in input)
			{
				if (ent is Line) lines.Add(ent as Line);
				if (ent is Polyline)
				{
					Polyline pl = ent as Polyline;
					if ((pl.Closed) && (pl.NumberOfVertices > 2))
					{
						polylines.Add(pl);
					}
				}
			}
		}
	}
}
