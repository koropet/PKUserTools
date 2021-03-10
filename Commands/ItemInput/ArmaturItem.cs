using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;


using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using App = Autodesk.AutoCAD.ApplicationServices.Application;

using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;
using PKUserTools.Commands;

using Inp=PKUserTools.EditorInput.Input;

namespace PKUserTools.Commands.ItemInput
{
    /// <summary>
    /// Класс, общий для арматурных позиций любой формы
    /// </summary>
    public class ArmaturItem:UtilityClass, ISpecification
    {
        internal bool status = false;
        const double masstolerance = 0.01;
        public static ItemInputOptions iio=ItemInput.iio;

        public string name;
        public string GOST;
        byte diam;
        public byte diameter
        {
            get
            {
                return diam;
            }
            set
            {
                diam = value;
                metermass = ItemInput.GetMass(Convert.ToDouble(diam));
            }

        }
        public uint count;
        public double metermass = 0;
        public List<ItemSegment> segments;

        public ArmaturItem()
        {
            segments = new List<ItemSegment>();
        }

        public double Mass
        {
            get
            {
                return round(metermass / 1000 * Length);//потому что масса на метр, а длина в мм
            }
        }
        public double ALLMass
        {
            get
            {
                return Mass * count;
            }
        }
        public virtual double Length
        {
            get
            {
                double sum = 0;
                foreach (ItemSegment seg in segments)
                {
                    sum += seg.Length;
                }
                //можно и тут округлять дополнительно
                return sum;
            }
        }
        double round(double length)
        {
            return Math.Round(length / masstolerance) * masstolerance;
        }
        public bool StatusOK
        {
            get
            {
                return status;
            }
        }
        public bool StatusBad
        {
            get
            {
                return !status;
            }
        }
        /// <summary>
        /// уникальный для каждого типа стержня способ ввода
        /// </summary>
        /// <param name="firstinput"></param>
        public virtual void Input(List<Entity> firstinput)
        {
            status = false;
        }
        
        public virtual void InputLength(double length)
        {
        	segments.Add(new StraitItemSegment(length));
        }
        public string SegmentsData
        {
            get
            {
                //сортируем сегменты
                //TODO ввод данных для сортировки сегментов пока не сделан
                /*SegmentComparer sc = new SegmentComparer();
                //горизонтальное
                sc.Direction = new Line(Point3d.Origin, new Point3d(1000, 0, 0));

                segments.Sort(sc);*/

                string result = "";
                foreach (ItemSegment seg in segments)
                {
                    result += "\t" + seg.DrawingString;
                }
                return result;
            }
        }

        #region Interface
        public virtual string SPname()
        {
            return string.Format("L={0:0}", Length);
        }

        public virtual string SPposition()
        {
            return name + "*";
        }

        public virtual string SPcount()
        {
            return string.Format("{0:0}", count);
        }

        public string SPmass()
        {
            return string.Format("{0:F2}", Mass).Replace('.', ',');
        }
        #endregion
		public override string ToString()
		{
			return string.Format("\nПолученная позиция \nПоз {0}\nДлина {1}\nКол-во {2}\nМасса {3}\nКласс {4}\nДиаметр {5}\nОбщая масса {6} ",
						                   name,Length,count,Mass,GOST,diameter,ALLMass);
		}
		public string GroupName
		{
			get
			{
				return string.Format("∅{0:0} {1}",diameter,GOST);
			}
		}
		public static implicit operator SortamentItem(ArmaturItem it)
		{
			Sortament st;
			//тут нечего придумывать, в старом варианте только 2 сортамента
			if(it.GOST.Contains("500")) st=Sortament.A500();
			else st=Sortament.A240();
			
			var si=new SortamentItem()
			{
				mark=it.diameter.ToString(),
				shape=Shape.FromString(it.SPname()),
				sortament=st,
				Name=it.name,
				Count=Convert.ToInt32(it.count),
			};
			return si;
		}

    }
    class StraitArmaturItem : ArmaturItem
    {
        public StraitArmaturItem()
        {

        }
        public override void Input(List<Entity> firstinput)
        {
            base.Input(firstinput);
            segments.Add(new StraitItemSegment(UT.EntityLength(firstinput[0])));

            if (firstinput.Count == 1) count = ItemInput.ItemCount();
            else count = Convert.ToUInt32(firstinput.Count);

            status = true;
        }
        public override string SPposition()
        {
            return name;
        }
        public override string ToString()
        {
            return "Прямой";
        }
    }
    class BendArmaturItem : ArmaturItem
    {
        public BendArmaturItem()
        {

        }
        public override void Input(List<Entity> objects)
        {
            foreach (Entity ent in objects)
            {
                if (ent is Line)
                {
                    segments.Add(new StraitItemSegment(((Line)ent).Length));
                }
                else if (ent is Arc)
                {
                    segments.Add(new RadiusItemSegment(ent as Arc));
                }
                else if (ent is Polyline)
                {
                    Polyline pl = ent as Polyline;
                    int count;
                    if (pl.Closed) count = pl.NumberOfVertices;
                    else count = pl.NumberOfVertices - 1;

                    for (int j = 0; j < count; j++)
                    {
                        SegmentType st = pl.GetSegmentType(j);
                        if (st == SegmentType.Line)
                        {
                            LineSegment2d lsd = pl.GetLineSegment2dAt(j);
                            segments.Add(new StraitItemSegment(lsd.Length));

                        }
                        else if (st == SegmentType.Arc)
                        {
                            CircularArc2d arc_s = pl.GetArcSegment2dAt(j);
                            Plane pn = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
                            Arc arc = new Arc(new Point3d(pn, arc_s.Center), Vector3d.ZAxis, arc_s.Radius, arc_s.StartAngle, arc_s.EndAngle);
                            segments.Add(new RadiusItemSegment(arc));
                        }
                    }
                }
            }
            base.count = ItemInput.ItemCount();
            status = true;
        }
        public override string ToString()
        {
            return "Гнутый";
        }
    }
    class BendVarArmaturItem : ArmaturItem
    {
        //для выбора переменных длин
        List<Entity> objects_var = new List<Entity>();

        public BendVarArmaturItem()
        {

        }
        public override void Input(List<Entity> objects)
        {
            base.Input(objects);
            bool doinput = true;
            while (doinput)
            {
                Entity base_ent;
                //ввод базы


                var Ssetbase = Inp.Objects("Выберите объект на проекции, представляющий базу");
                if (Inp.StatusBad) continue;
                if (Ssetbase.Count != 1)
                {
                    Alert("Введите один объект!");
                    continue;
                }

                using (TransactionHelper th = new TransactionHelper())
                {
                    base_ent = th.ReadObject(Ssetbase[0].ObjectId) as Entity;
                }

                var Ssetvar = Inp.Objects("nВыберите переменные длины");
                if (Inp.StatusBad) return;
                
                using (TransactionHelper th = new TransactionHelper())
                {
                    objects_var = th.ReadObjects(Ssetvar);
                }

                double baselength = UT.EntityLength(base_ent) - UT.EntityLength(objects_var[0]);

                double[] lengthdata = new double[objects_var.Count];

                for (int i = 0; i < objects_var.Count; i++)
                {
                    lengthdata[i] = UT.EntityLength(objects_var[i]) + baselength;
                }

                segments.Add(new VariableItemSegment(lengthdata));
                doinput = false;

            }
            //добавляем остальные сегменты

            foreach (Entity ent in objects)
            {
                if (ent is Line)
                {
                    segments.Add(new StraitItemSegment(((Line)ent).Length));
                }
                else if (ent is Arc)
                {
                    segments.Add(new RadiusItemSegment(ent as Arc));
                }
                else if (ent is Polyline)
                {
                    Polyline pl = ent as Polyline;
                    int count;
                    if (pl.Closed) count = pl.NumberOfVertices;
                    else count = pl.NumberOfVertices - 1;

                    for (int j = 0; j < count; j++)
                    {
                        SegmentType st = pl.GetSegmentType(j);
                        if (st == SegmentType.Line)
                        {
                            LineSegment2d lsd = pl.GetLineSegment2dAt(j);
                            segments.Add(new StraitItemSegment(lsd.Length));

                        }
                        else if (st == SegmentType.Arc)
                        {
                            CircularArc2d arc_s = pl.GetArcSegment2dAt(j);
                            Plane pn = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
                            Arc arc = new Arc(new Point3d(pn, arc_s.Center), Vector3d.ZAxis, arc_s.Radius, arc_s.StartAngle, arc_s.EndAngle);
                            segments.Add(new RadiusItemSegment(arc));
                        }
                    }
                }
            }
            base.count = ItemInput.ItemCount();
            status = true;
        }
        public override string SPname()
        {
            return string.Format("Lср={0:0}", Length);
        }
        public override string ToString()
        {
            return "Гнутый с переменными сегментами";
        }
    }
    class VariableArmaturItem : ArmaturItem
    {
        public VariableArmaturItem()
        {

        }
        public override void Input(List<Entity> firstinput)
        {
            
            base.Input(firstinput);
            if (firstinput.Count == 2)
            {
                segments.Add(new VariableItemSegment(UT.EntityLength(firstinput[0]), UT.EntityLength(firstinput[1])));

            }
            else
            {
                double[] lengthdata = new double[firstinput.Count];

                for (int i = 0; i < firstinput.Count; i++)
                {
                    lengthdata[i] = UT.EntityLength(firstinput[i]);
                }
                segments.Add(new VariableItemSegment(lengthdata));

            }
            base.count = ItemInput.ItemCount();
            status = true;
        }
        public override string SPname()
        {
            return string.Format("Lср={0:0}", Length);
        }
        public override string ToString()
        {
            return "Переменный";
        }
    }
    class AllArmaturItem : ArmaturItem
    {
        public AllArmaturItem()
        {

        }
        public override void Input(List<Entity> objects)
        {
            base.Input(objects);
            double sum = 0;
            foreach (Entity ent in objects)
            {
                sum += UT.EntityLength(ent);
            }
            while (iio.SumCountInput)
            {
            	var sset=Inp.Objects("Введите дополнительный набор стержней");
                if (Inp.StatusBad) break;
                
                using(TransactionHelper th=new TransactionHelper())
                {
                    objects=th.ReadObjects(sset);
                    foreach (Entity ent in objects)
                    {
                        sum += UT.EntityLength(ent);
                    }
                }
            }

            segments.Add(new StraitItemSegment(sum));
            count = 1;

            status = true;
        }
        public override double Length
        {
            get
            {
                return Math.Round(base.Length/1000)*1000;
            }
        }
        public override string SPname()
        {
            return string.Format("Lобщ={0:0}м",Length/1000);
        }
        public override string SPcount()
        {
            return "-";
        }
        public override string ToString()
        {
            return "Общая длина";
        }
    }
}
