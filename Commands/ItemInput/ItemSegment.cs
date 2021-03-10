using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

namespace PKUserTools.Commands.ItemInput
{
    public class ItemSegment
    {
        const double tolerance = 0.01;
        const double all_tol = 1000;

        public Point3d start;
        public Point3d end;

        internal double[] length_data;
        public virtual double Length
        {
            get
            {
                return length_data[0];
            }
        }
        public virtual string DrawingString
        {
            get
            {
                return string.Format("{0:0}", Length);
            }
        }
        public double round(double length)
        {
            return Math.Round(length / tolerance) * tolerance;
        }
    }
    class StraitItemSegment : ItemSegment
    {
        public StraitItemSegment(double length)
        {
            length_data = new double[1] { length };

        }
    }
    class RadiusItemSegment : ItemSegment
    {
        double radius;
        public RadiusItemSegment(Arc arc)
        {
            length_data = new double[1]{UT.EntityLength(arc)};
            radius = arc.Radius;

            start = arc.StartPoint;
            end = arc.EndPoint;
        }
    }
    class VariableItemSegment : ItemSegment
    {
        public VariableItemSegment(double length1, double length2)
        {
            length_data = new double[2] { length1, length2 };
        }
        public VariableItemSegment(double[] length_data)
        {
            this.length_data = length_data;
        }

        public override string DrawingString
        {
            get
            {
                if (length_data.GetLength(0) == 2) return string.Format("{0:0}...{1:0}", round(length_data[0]), round(length_data[1]));
                else return VarDrawingString();
            }
        }
        public override double Length
        {
            get
            {
                double sumresult = 0;
                foreach (double l in length_data)
                {
                    sumresult += l;
                }
                sumresult = sumresult / length_data.GetLength(0);
                return sumresult;
            }
        }
        string VarDrawingString()
        {
            string result = "";

            List<double> lengts = new List<double>();
            List<uint> counts = new List<uint>();

            //рассовываем длины в стопочки
            for (int i = 0; i < length_data.GetLength(0); i++)
            {
                double l = Math.Round(length_data[i] / 10) * 10;

                int index = lengts.BinarySearch(l);
                if (index >= 0)
                {
                    counts[index]++;
                }
                else
                {
                    counts.Add(1);
                    lengts.Add(l);
                }
            }
            bool all_ones = true;
            //определяем, всех ли по одному
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] != 1)
                {
                    all_ones = false;
                    break;
                }
            }
            if (all_ones)
            {
                //случай когда всех длин по одной

                for (int i = 0; i < lengts.Count; i++)
                {
                    result += string.Format("{0:0}", lengts[i]);

                    if (i != lengts.Count - 1) result += ";";
                    else result += " ";

                }
                result += "по 1шт";
                return result;
            }


            for (int i = 0; i < counts.Count; i++)
            {
                result += string.Format("{0:0}", lengts[i]) + " " + counts[i] + "шт";


                //ставим разделители
                if (i == counts.Count - 1) result += ".";
                else result += "; ";
            }
            return result;

        }
    }
}
