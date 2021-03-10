/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 10.09.2018
 * Время: 16:17
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
    /// Замена команде BreakLine от ExpressTools. Надоело каждый раз вводить размеры для разных аннотативных масштабов.
    /// </summary>
    class BreakLine : FunctionClass
    {

        static Polyline DefaultPolyline()
        {
            Polyline result = new Polyline(0);
            /*Надо быть внимательным! если мы создаем объект полилинии с известным числом вершин, начинать добавлять точки надо с 1
			 * Если же мы просто в цикле добавляем новые точки, надо использовать метод AddVertexAt и начиная с нуля */
            result.AddVertexAt(0, new Point2d(-0.5, 0), 0, 0, 0);
            result.AddVertexAt(1, new Point2d(-0.25, -0.667), 0, 0, 0);
            result.AddVertexAt(2, new Point2d(0.25, 0.667), 0, 0, 0);
            result.AddVertexAt(3, new Point2d(0.5, 0), 0, 0, 0);
            return result;
        }

        //базовые точки и точки смещенные с краев
        Point3d p1, p2, p11, p21;

        //список точек для размещения брейклайнов
        List<Point2d> data;

        Line p1p2;
        Vector3d p1p2v;
        double scale = 1;

        //Размер значка и удлиннение линий
        double size = 3, overlength = 2.5;

        List<Entity> results = new List<Entity>();

        public void MakeBreakLine()
        {
            
            using (var lm = new PKLayerManager())
            {
                //находим текущий масштаб аннотаций. Не забываем что в аннотативном масштабе 1:25 масштаб равен 0,04, но нам нужно именно обратное число
                scale = 1 / (UT.GetAnnoScale("CANNOSCALE").Scale);

                size *= scale;
                overlength *= scale;
                
                lm.CommandLayer = "ПК_С_ЛинииРазрыва";

                var sset = Input.Implied();
                if (Input.StatusBad) //нет предварительно выбранных объектов. Используем старый механизм.
                {


                    p1 = Input.Point("\nВведите первую точку"); if (Input.StatusBad) return;
                    p2 = Input.Point("\nВведите вторую точку"); if (Input.StatusBad) return;
                    

                    p1p2 = new Line(p1, p2);
                    p1p2v = p1p2.Delta.MultiplyBy(1 / p1p2.Length);

                    Plane pl = new Plane(p1, p1p2.Delta.GetPerpendicularVector());

                    p11 = p1.Add(p1p2v.MultiplyBy(overlength * -1));
                    p21 = p2.Add(p1p2v.MultiplyBy(overlength));

                    //заполняем точки пока ввод корректный. Если не введено ни одной точки, ставим единственную в середину
                    data = new List<Point2d>();

                    var pt = Input.Point("\nУкажите точку вставки символа или по умолчанию в середине");

                    int cnt = 0;
                    if (Input.StatusBad)
                    {
                        if (int.TryParse(Input.StringResult, out cnt) && cnt > 0)
                        {
                            data = Divide(cnt);
                        }
                        else
                        {
                            data.Add(UT.GetMiddle(p1, p2).to2d());
                        }
                    }

                    while (Input.StatusOK)
                    {
                        data.Add(pt.OrthoProject(pl).to2d());
                        pt = Input.Point("\nУкажите следующую точку вставки символа");
                    }
                    results.Add(Prepare());
                }
                else
                {
                    using (var th = new TransactionHelper())
                    {
                        var ents = th.EditObjects(sset);
                        var lines = ents.OfType<Line>();
                        var polylines = ents.OfType<Polyline>();

                        foreach(var l in lines)
                        {
                            th.WriteObject(MakeFromLine(l));
                            l.Erase();
                        }
                        foreach (var pl in polylines)
                        {
                            th.WriteObject(MakeFromPolyLine(pl));
                            pl.Erase();
                        }
                    }
                }
                using (var th = new TransactionHelper())
                {
                    th.WriteObjects(results);
                }
            }

        }
        /// <summary>
        /// нанизываем на линию символы исходя из точек
        /// </summary>
        /// <param name="data">массив точек для вставки символов</param>
        /// <returns></returns>
        Polyline Prepare()
        {
            var result = new Polyline();
            //сортируем точки исходя из расстояния от начала
            data.Sort((x, y) => x.GetDistanceTo(p1.to2d()).CompareTo(y.GetDistanceTo(p1.to2d())));
            result.AddVertexAt(0, p11.to2d(), 0, 0, 0);

            foreach (Point2d pt in data)
            {
                Matrix3d M = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                                                          new Point3d(pt.X, pt.Y, 0), p1p2v, p1p2v.GetPerpendicularVector(), Vector3d.ZAxis);

                //переносим и масштабируем символ
                Polyline bl = DefaultPolyline();
                bl.TransformBy(M);
                M = Matrix3d.Scaling(size, new Point3d(pt.X, pt.Y, 0));
                bl.TransformBy(M);

                //добавляем символ по одной точке в результирующую полилинию в ее конец
                for (int i = 0; i < bl.NumberOfVertices; i++)
                {
                    result.AddVertexAt(result.NumberOfVertices, bl.GetPoint2dAt(i), 0, 0, 0);
                }
            }

            result.AddVertexAt(result.NumberOfVertices, p21.to2d(), 0, 0, 0);
            result.SetDatabaseDefaults();
            return result;
        }
        /// <summary>
        /// Делим отрезок
        /// </summary>
        /// <param name="count">количество промежуточных точек</param>
        /// <returns></returns>
        List<Point2d> Divide(int count)
        {
            var result = new List<Point2d>();
            double increment = p1p2.Length / ((count + 1) / p1p2v.Length);
            for (int i = 1; i <= count; i++)
            {
                result.Add(p1.Add(p1p2v.MultiplyBy(increment * i)).to2d());
            }
            return result;
        }
        Polyline MakeFromLine(Line l)
        {
            var result = new Polyline();
            p1 = l.StartPoint; p2 = l.EndPoint;

            p1p2 = new Line(p1, p2);
            p1p2v = p1p2.Delta.MultiplyBy(1 / p1p2.Length);

            p11 = p1.Add(p1p2v.MultiplyBy(overlength * -1));
            p21 = p2.Add(p1p2v.MultiplyBy(overlength));


            result.AddVertexAt(0, p11.to2d(), 0, 0, 0);

            Point2d pt = new Point2d(p1.X * 0.5 + p2.X * 0.5, p1.Y * 0.5 + p2.Y * 0.5);
            Matrix3d M = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                                                      new Point3d(pt.X, pt.Y, 0), p1p2v, p1p2v.GetPerpendicularVector(), Vector3d.ZAxis);

            //переносим и масштабируем символ
            Polyline bl = DefaultPolyline();
            bl.TransformBy(M);
            M = Matrix3d.Scaling(size, new Point3d(pt.X, pt.Y, 0));
            bl.TransformBy(M);

            //добавляем символ по одной точке в результирующую полилинию в ее конец
            for (int i = 0; i < bl.NumberOfVertices; i++)
            {
                result.AddVertexAt(result.NumberOfVertices, bl.GetPoint2dAt(i), 0, 0, 0);
            }


            result.AddVertexAt(result.NumberOfVertices, p21.to2d(), 0, 0, 0);
            result.SetDatabaseDefaults();
            return result;
        }
        Polyline MakeFromPolyLine(Polyline pl)
        {

            var result = new Polyline();

            int count = pl.NumberOfVertices - 1; //segments count
            count += pl.Closed ? 1 : 0;

            result.AddVertexAt(result.NumberOfVertices, pl.GetPoint2dAt(0), 0, 0, 0);

            for (int curr_vert = 0; curr_vert < count; curr_vert++)
            {
                if (pl.GetSegmentType(curr_vert) == SegmentType.Line)
                {
                    p1 = pl.GetPoint3dAt(curr_vert); p2 = (pl.Closed && curr_vert == count - 1) ? pl.GetPoint3dAt(0) : pl.GetPoint3dAt(curr_vert + 1);
                    p1p2 = new Line(p1, p2);
                    p1p2v = p1p2.Delta.MultiplyBy(1 / p1p2.Length);

                    Point2d pt = new Point2d(p1.X * 0.5 + p2.X * 0.5, p1.Y * 0.5 + p2.Y * 0.5);
                    Matrix3d M = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                                                              new Point3d(pt.X, pt.Y, 0), p1p2v, p1p2v.GetPerpendicularVector(), Vector3d.ZAxis);

                    //переносим и масштабируем символ
                    Polyline bl = DefaultPolyline();
                    bl.TransformBy(M);
                    M = Matrix3d.Scaling(size, new Point3d(pt.X, pt.Y, 0));
                    bl.TransformBy(M);

                    //добавляем символ по одной точке в результирующую полилинию в ее конец
                    for (int i = 0; i < bl.NumberOfVertices; i++)
                    {
                        result.AddVertexAt(result.NumberOfVertices, bl.GetPoint2dAt(i), 0, 0, 0);
                    }
                    result.AddVertexAt(result.NumberOfVertices, pl.GetPoint2dAt((pl.Closed && curr_vert == count - 1) ? 0 : curr_vert + 1), 0, 0, 0);
                }
                else
                {
                    result.AddVertexAt(result.NumberOfVertices, pl.GetPoint2dAt((pl.Closed && curr_vert == count - 1) ? 0 : curr_vert + 1), 0, 0, 0);
                    result.SetBulgeAt(result.NumberOfVertices - 2, pl.GetBulgeAt(curr_vert));
                }
            }

            if (!pl.Closed)
            {
                //making offset of tails
                result.AddVertexAt(result.NumberOfVertices, pl.GetPoint2dAt(pl.NumberOfVertices-1), 0, 0, 0);

                p1 = pl.GetPoint3dAt(0); p2 = pl.GetPoint3dAt(1);
                p1p2 = new Line(p1, p2);
                p1p2v = p1p2.Delta.MultiplyBy(1 / p1p2.Length);
                p11 = p1.Add(p1p2v.MultiplyBy(overlength * -1));

                p1 = pl.GetPoint3dAt(pl.NumberOfVertices - 2); p2 = pl.GetPoint3dAt(pl.NumberOfVertices-1);
                p1p2 = new Line(p1, p2);
                p1p2v = p1p2.Delta.MultiplyBy(1 / p1p2.Length);
                p21 = p2.Add(p1p2v.MultiplyBy(overlength));

                result.SetPointAt(0, p11.to2d());
                result.SetPointAt(result.NumberOfVertices - 1, p21.to2d());
            }
            else result.Closed = true;
            result.SetDatabaseDefaults();
            return result;
        }
    }
}
