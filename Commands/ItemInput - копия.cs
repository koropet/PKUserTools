
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

namespace PKUserTools.Commands
{
    class ItemInput : FunctionClass
    {
        static double[] diameters = { 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40 };
        static Hashtable diamtomassa500;
        static double[] massesA500 = { 0.099, 0.154, 0.222, 0.395, 0.616, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.853, 4.834, 6.313, 7.99, 9.865 };

        static ItemInputMode iim = ItemInputMode.Constant;

        const string MyItemKey = "PKpositions";

        static ItemInputOptions iio;

        /// <summary>
        /// Пользовательские данные чертежа
        /// </summary>
        static Hashtable userdata;

        static List<Item> items = new List<Item>();

        List<Entity> objects = new List<Entity>();

        //для выбора переменных длин
        List<Entity> objects_var = new List<Entity>();

        List<Line> lines = new List<Line>();

        //текущая позиция
        Item pos = new Item();

        //Метод ввода
        delegate void InputItem();
        InputItem ii;

        string[] ItemKeywords = new string[]
        {
            "Strait","ПРямой",
			"BEnd","ГНутый",
			"VBend","ПЕременныйгнутый",
			"Middle","СРеднее",
			"ALllength","ОБщая",
		    "OUtput","ВЫвод",
			"CLear","ОЧистить",
            "PArams", "ПАаметры",
            "EDit", "РЕдактирование",
        };
        public override void Execute()
        {
            base.Execute();
            if (diamtomassa500 == null) InitStaticData();
            
            //настройки ввода
            if (iio == null) iio = new ItemInputOptions();
            Tweet(iio.ToString());
            if (items == null) items = new List<Item>(); //при первом использовании заводим список позиций
            Tweet(iim.ToString());

            ObjectsInput OI = new ObjectsInput()
            {
                Keywords = ItemKeywords,
                Message = "\nВыберите объекты для добавления позиции",
                KeywordInput=new SelectionTextInputEventHandler(KeywordInput),
            };
            if (OI.StatusBad) return;
            using (TransactionHelper th = new TransactionHelper())
            {
                objects = th.ReadObjects(OI.SSet);
            }
            ii += EnterParams;
            ii.Invoke();
            items.Add(pos);

        }

        void Output()
        {
            //пробуем создать группу позиций
            SpecificationGroup details = new SpecificationGroup("Детали");

            //здесь происходит сортировка по диаметрам

            ItemGrouper ig = new ItemGrouper(items);

            foreach (ArmaturGroup ag in ig.groups)
            {
                details.childs.Add(ag);
            }

            //копируем в буфер обмена
            Clipboard.SetText(details.Row());
        }
        void KeywordInput(object s, SelectionTextInputEventArgs e)
        {
            //анализ ключевых слов
            if (e.Input == "Strait") ii += InputConstant;
            else if (e.Input == "BEnd") ii += InputBend;
            else if (e.Input == "VBend") ii += InputBendVar;
            else if (e.Input == "Middle") ii += InputVariable;
            else if (e.Input == "ALllength") ii += InputAll;
            else if (e.Input == "OUtput") Output();
            else if (e.Input == "CLear") ClearItems();
            else if (e.Input == "PArams") SetParams();
            else if (e.Input == "EDit") iio.EditingMode = !iio.EditingMode;
            
        }
        void InputConstant()
        {
            iim = ItemInputMode.Constant;
            pos.segments.Add(new ItemSegment(UT.EntityLength(objects[0])));

            if (objects.Count == 1) pos.count = ItemCount();
            else pos.count = Convert.ToUInt32(objects.Count);
        }
        void InputBend()
        {
            iim = ItemInputMode.Bend;
            if (objects.Count == 2)
            {
                pos.segments.Add(new ItemSegment(UT.EntityLength(objects[0]), UT.EntityLength(objects[1])));

            }
            else
            {
                double[] lengthdata = new double[objects.Count];

                for (int i = 0; i < objects.Count; i++)
                {
                    lengthdata[i] = UT.EntityLength(objects[i]);
                }
                pos.segments.Add(new ItemSegment(lengthdata));

            }

            
        }
        void InputBendVar()
        {
            iim = ItemInputMode.BendVar;


            bool doinput = true;
            while (doinput)
            {
                Entity base_ent;
                //ввод базы


                ObjectsInput OIbase = new ObjectsInput()
                {
                    Message = "\nВыберите объект на проекции, представляющий базу",
                };
                OIbase.Input();
                if (OIbase.StatusBad) continue;
                if (OIbase.SSet.Count != 1)
                {
                    Alert("Введите один объект!");
                    continue;
                }

                using (TransactionHelper th = new TransactionHelper())
                {
                    base_ent = th.ReadObject(OIbase.SSet[0].ObjectId) as Entity ;
                }

                ObjectsInput OIvar = new ObjectsInput()
                {
                    Message = "\nВыберите переменные длины",
                };
                OIvar.Input();
                if (OIvar.StatusBad) return;
                using (TransactionHelper th = new TransactionHelper())
                {
                    objects_var = th.ReadObjects(OIvar.SSet);
                }

                double baselength = UT.EntityLength(base_ent) - UT.EntityLength(objects_var[0]);

                double[] lengthdata = new double[objects_var.Count];

                for (int i = 0; i < objects_var.Count; i++)
                {
                    lengthdata[i] = UT.EntityLength(objects_var[i]) + baselength;
                }

                pos.segments.Add(new ItemSegment(lengthdata));
                doinput = false;

            }
            //добавляем остальные сегменты

            foreach (Entity ent in objects)
            {
                if (ent is Line)
                {
                    pos.segments.Add(new ItemSegment(((Line)ent).Length));
                }
                else if (ent is Arc)
                {
                    pos.segments.Add(new ItemSegment(ent as Arc));
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
                            pos.segments.Add(new ItemSegment(lsd.Length));

                        }
                        else if (st == SegmentType.Arc)
                        {
                            CircularArc2d arc_s = pl.GetArcSegment2dAt(j);
                            Plane pn = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
                            Arc arc = new Arc(new Point3d(pn, arc_s.Center), Vector3d.ZAxis, arc_s.Radius, arc_s.StartAngle, arc_s.EndAngle);
                            pos.segments.Add(new ItemSegment(arc));
                        }
                    }
                }
            }

            pos.count = ItemCount();
        }
        void InputVariable()
        {
            iim = ItemInputMode.Variable;
        }
        void InputAll()
        {
            iim = ItemInputMode.All;
        }
        void ClearItems()
        {
            items = new List<Item>();

            if (userdata.Contains(MyItemKey))
            {
                userdata.Remove(MyItemKey);
            }
            return;
        }
        void SetParams()
        {
            //сделать объект ввода ключевых слов
            PromptKeywordOptions pko;
            PromptResult pkr;
            //настройка параметров
            pko = new PromptKeywordOptions("Укажите опцию");
            pko.Keywords.Add("COunt", "КОличество");

            pkr = acEd.GetKeywords(pko);

            if (pkr.Status != PromptStatus.OK) return;

            if (pkr.StringResult == "COunt")
            {
                //сода вводим опцию ввода количества
                pko = new PromptKeywordOptions("Выберите режим ввода количества");
                pko.Keywords.Add("Manual", "ВРучную");
                pko.Keywords.Add("SUmm", "СУммирование");

                pkr = acEd.GetKeywords(pko);

                if (pkr.Status != PromptStatus.OK) return;

                if (pkr.StringResult == "Manual")
                {
                    iio.ManualCountInput = !iio.ManualCountInput;
                    Tweet(iio.ManualModeMsg);
                }
                if (pkr.StringResult == "SUmm")
                {
                    iio.SumCountInput = !iio.SumCountInput;
                    Tweet(iio.SumModeMsg);
                }
            }
        }
        void InitStaticData()
        {
            //загоняем данные в хэштаблицу
            diamtomassa500 = new Hashtable();
            for (int i = 0; i < diameters.GetLength(0); i++)
            {
                diamtomassa500.Add(diameters[i], massesA500[i]);
            }

            gost = new Hashtable();
            gost.Add("A500", "А500С ГОСТ Р 52544-2006");
            gost.Add("A240", "А240 ГОСТ 5781-82");

            userdata = App.DocumentManager.MdiActiveDocument.UserData;
            if (userdata.Contains(MyItemKey))
            {
                Tweet("Найдены данные о позициях");
                items = (List<Item>)userdata[MyItemKey];
            }
            else
            {

                Tweet("Данные о позициях в чертеже не найдены");
            }
        }
        /// <summary>
        /// Ввод количества объектов в позиции
        /// </summary>
        /// <returns></returns>
        public static uint ItemCount()
        {
            Editor acEd = App.DocumentManager.MdiActiveDocument.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            PromptSelectionResult acSSPrompt;

            PromptIntegerOptions pio;
            PromptIntegerResult pir;

            if (iio.ManualCountInput)
            {
                pio = new PromptIntegerOptions("Укажите количество");


                if (iio.SumCountInput)
                {
                    pir = acEd.GetInteger(pio);
                    int sum = 0;
                    while (pir.Status == PromptStatus.OK)
                    {
                        sum += pir.Value;
                        acEd.WriteMessage(string.Format("Добавили {0} к количеству. Всего {1} шт", pir.Value, sum));

                        pir = acEd.GetInteger(pio);
                    }
                    return Convert.ToUInt32(sum);

                }
                else
                {
                    pir = acEd.GetInteger(pio);
                    if (pir.Status != PromptStatus.OK) return 0;
                }

                return Convert.ToUInt32(pir.Value);
            }
            else
            {
                pso.MessageForAdding = "Выберите объекты. Количество объектов равно количеству в позиции";

                if (iio.SumCountInput)
                {
                    acSSPrompt = acEd.GetSelection(pso);
                    int sum = 0;
                    while (acSSPrompt.Status == PromptStatus.OK)
                    {
                        sum += acSSPrompt.Value.Count;
                        acEd.WriteMessage(string.Format("Добавили {0} к количеству. Всего {1} шт", acSSPrompt.Value.Count, sum));
                        acSSPrompt = acEd.GetSelection(pso);
                    }
                    return Convert.ToUInt32(sum);
                }
                else
                {
                    acSSPrompt = acEd.GetSelection(pso);
                    if (acSSPrompt.Status != PromptStatus.OK) return 0;
                }
                return Convert.ToUInt32(acSSPrompt.Value.Count);

            }


        }
        static Hashtable gost;
        public static string EnterGOST()
        {


            Editor acEd = App.DocumentManager.MdiActiveDocument.Editor;

            PromptKeywordOptions pko = new PromptKeywordOptions("Выберите класс арматуры");

            pko.Keywords.Add("A500");
            pko.Keywords.Add("A240");

            PromptResult pkr = acEd.GetKeywords(pko);

            if (gost.ContainsKey(pkr.StringResult))
            {
                return (string)gost[pkr.StringResult];
            }

            else return "";
        }

        public static double GetMass(double diameter)
        {
            if (!diamtomassa500.ContainsKey(diameter))
            {
                App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Не найден диаметр. По умолчанию установлена масса 0");
                return 0;
            }
            return (double)diamtomassa500[diameter];
        }

        public static double EnterDiameter()
        {
            Editor acEd = App.DocumentManager.MdiActiveDocument.Editor;

            PromptDoubleOptions pdo = new PromptDoubleOptions("Введите диаметр");
            PromptDoubleResult pdr = acEd.GetDouble(pdo);

            if (pdr.Status != PromptStatus.OK)
            {
                return 0;
            }
            int ind = Array.IndexOf(diameters, pdr.Value);
            if (ind >= 0) return diameters[ind];
            else return 0;

        }

        void EnterParams()
        {
            pos.count = ItemCount();
            pos.diameter = Convert.ToByte(EnterDiameter());
            pos.GOST = EnterGOST();
        }
    }

    public class ItemInputOptions
    {
        public bool ManualCountInput = false;
        public bool SumCountInput = false;

        public bool EditingMode = false;

        public ItemInputOptions()
        {
        }

        public string ManualModeMsg
        {
            get
            {
                if (ManualCountInput) return "Режим ввода количества вручную\n";
                else return "Режим ввода количества с помощью числа выбранных объектов\n";
            }
        }
        public string SumModeMsg
        {
            get
            {
                if (SumCountInput) return "Режим суммирования количества\n";
                else return "Режим одиночного ввода количества\n";
            }
        }
        public string EditModeMsg
        {
            get
            {
                if (EditingMode) return "Редактирование позиции вкл\n";
                else return "Редактирование позиции выкл\n";
            }
        }
        public new string ToString()
        {
            return "Опции ввода количества:\n" + ManualModeMsg + SumModeMsg + EditModeMsg;
        }
    }

    public class ItemGrouper
    {
        List<Item> items;
        public List<ArmaturGroup> groups;
        public ItemGrouper(List<Item> items)
        {

            this.items = items;
            groups = new List<ArmaturGroup>();
            sort_items();

        }
        void sort_items()
        {

            foreach (Item it in items)
            {
                bool found = false;
                for (int i = 0; i < groups.Count; i++)
                {

                    if (groups[i].IsOwnItem(it))
                    {
                        groups[i].AddItem(it);
                        found = true;

                    }
                }
                if (!found)
                {
                    List<Item> newlist = new List<Item>();
                    newlist.Add(it);
                    ArmaturGroup ag = new ArmaturGroup();
                    ag.rods = newlist;
                    groups.Add(ag);
                }
            }
        }
    }
    
    public class ItemSegment
    {
        //точность округлений 10мм
        const double tolerance = 10;
        //общая длина с точностью до 1м
        const double all_tol = 1000;

        public double kef = 1.1; //учет нахлестки для распределительной

        public double[] length_data;

        //границы для сортировки
        public Point3d start;
        public Point3d end;


        double radius;
        public ItemSegmentType i_s_type = ItemSegmentType.Strait;

        /// <summary>
        /// создание прямого сегмента
        /// </summary>
        /// <param name="length">длина</param>
        public ItemSegment(double length)
        {
            length_data = new double[1] { length };
            i_s_type = ItemSegmentType.Strait;

        }

        /// <summary>
        /// Создание сегмента переменной длины с конечным и начальным значением
        /// </summary>
        /// <param name="length1"></param>
        /// <param name="length2"></param>
        public ItemSegment(double length1, double length2)
        {
            length_data = new double[2] { length1, length2 };
            i_s_type = ItemSegmentType.Variable;

        }

        /// <summary>
        /// Создние сегмента переменной длины с точным указанием длины каждого стержня
        /// </summary>
        /// <param name="length_data">Массив длин</param>
        public ItemSegment(double[] length_data)
        {
            this.length_data = length_data;
            i_s_type = ItemSegmentType.Variable_precise;

        }

        /// <summary>
        /// Создание сегмента-загиба по радиусу
        /// </summary>
        /// <param name="arc"></param>
        public ItemSegment(Arc arc)
        {
            length_data = new double[1];
            length_data[0] = UT.EntityLength(arc);
            i_s_type = ItemSegmentType.Radius;
            radius = arc.Radius;

            start = arc.StartPoint;
            end = arc.EndPoint;
        }

        /// <summary>
        /// сегмент-общая длина
        /// </summary>
        /// <param name="sum">Общая длина</param>
        /// <param name="issum">Поставить тру</param>
        public ItemSegment(double sum, bool issum)
        {
            length_data = new double[1] { sum };
            i_s_type = ItemSegmentType.Sum;

        }
        /// <summary>
        /// Вывод строки для отображения сегмента на эскизе
        /// </summary>
        public string DrawingString
        {
            get
            {
                switch (i_s_type)
                {
                    case ItemSegmentType.Strait:
                        {
                            return string.Format("{0:0}", Length);
                        }
                    case ItemSegmentType.Variable:
                        {
                            return string.Format("{0:0}...{1:0}", round(length_data[0]), round(length_data[1]));
                        }
                    case ItemSegmentType.Variable_precise:
                        {
                            return VarDrawingString();
                        }
                    case ItemSegmentType.Sum:
                        {
                            return "Распределительная арматура";
                        }
                    case ItemSegmentType.Radius:
                        {
                            return string.Format("{0:0}", Length);
                        }
                    default:
                        {
                            return "";
                        }
                }
            }

        }
        /// <summary>
        /// Вывод длины или средней длины
        /// </summary>
        public double Length
        {
            get
            {
                double result = 0;

                switch (i_s_type)
                {
                    case ItemSegmentType.Strait:
                        {
                            result = length_data[0];
                            break;
                        }
                    case ItemSegmentType.Radius:
                        {
                            result = length_data[0];
                            break;
                        }
                    case ItemSegmentType.Variable:
                        {
                            result = length_data[0] * 0.5 + length_data[1] * 0.5;
                            break;
                        }
                    case ItemSegmentType.Variable_precise:
                        {
                            double sumresult = 0;
                            foreach (double l in length_data)
                            {
                                sumresult += l;
                            }
                            result = sumresult / length_data.GetLength(0);
                            break;
                        }
                    case ItemSegmentType.Sum:
                        {
                            result = length_data[0] * kef;
                            break;
                        }

                    default:
                        {
                            result = 0;
                            break;
                        }
                }
                return round(result);
            }
        }
        double round(double length)
        {
            if (i_s_type == ItemSegmentType.Sum)
            {
                return Math.Round(length / all_tol) * all_tol;
            }
            return Math.Round(length / tolerance) * tolerance;
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

    public class Item : ISpecification
    {
        const double masstolerance = 0.01;

        public List<ItemSegment> segments;
        public uint count;
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
                metermass =ItemInput.GetMass(Convert.ToDouble(diam));
            }

        }
        public double metermass = 0;

        /// <summary>
        /// Суммарная длина
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="alllength"></param>
        public Item(List<Entity> data, bool alllength)
        {
            segments = new List<ItemSegment>();

            double sum = 0;
            foreach (Entity ent in data)
            {
                sum += UT.EntityLength(ent);
            }

            segments.Add(new ItemSegment(sum, true));
        }

        /// <summary>
        /// Создание новго экземпляра
        /// </summary>
        public Item()
        {
            segments = new List<ItemSegment>();
        }
        public double Length
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

        bool HasVariable
        {
            get
            {
                foreach (ItemSegment seg in segments)
                {
                    if (seg.i_s_type == ItemSegmentType.Variable || seg.i_s_type == ItemSegmentType.Variable_precise) return true;
                }
                return false;
            }
        }
        public bool HasAll
        {
            get
            {
                foreach (ItemSegment seg in segments)
                {
                    if (seg.i_s_type == ItemSegmentType.Sum) return true;

                }
                return false;
            }
        }
        public string SpecString
        {
            get
            {
                if (HasVariable) return string.Format("Lср={0:0}", Length);
                else if (HasAll) return string.Format("Lобщ={0:0}м", Length / 1000);
                else return string.Format("L={0:0}", Length);

            }

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

        double round(double length)
        {
            return Math.Round(length / masstolerance) * masstolerance;
        }
        #region Interface to spec table
        public string SPposition()
        {
            if (segments[0].i_s_type == ItemSegmentType.Strait)
            {
                return name;
            }
            else if (HasAll)
            {
                return name;
            }
            else
            {
                return name + "*";
            }
        }

        public string SPname()
        {
            return SpecString;
        }

        public string SPcount()
        {
            if (HasAll)
            {
                return "-";
            }
            else
            {
                return string.Format("{0:0}", count);
            }
        }

        public string SPmass()
        {
            return string.Format("{0}", Mass).Replace('.', ',');
        }
        #endregion
    }

    public enum ItemSegmentType
    {
        Tail,
        Strait,
        Variable,
        Variable_precise,
        Radius,
        Sum,
    }
    public enum ItemInputMode
    {
        Constant,
        Variable,
        All,
        Bend,
        BendVar,
    }
}