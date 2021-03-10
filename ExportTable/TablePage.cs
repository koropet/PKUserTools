using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;

namespace PKUserTools.ExportTable
{
    class TablePage : UtilityClass
    {
        /// <summary>
        /// Необходим чтобы сравнивать числа с заданной точностью
        /// </summary>
        class Comparer : IEqualityComparer<double>
        {
            #region IEqualityComparer implementation
            public bool Equals(double x, double y)
            {
                return Math.Abs(x - y) <= tolerance;
            }
            public int GetHashCode(double obj)
            {
                //я хз какого черта он использует для сравнения именно этот метод, хотя вроде логично, но мне нарвиться больше Equals

                return (Math.Round(obj / tolerance) * tolerance).GetHashCode();
            }
            #endregion

        }
        public MText format;

        //выходные данные о ячейках
        public TextCell[,] textcels;

        /// <summary>
        /// Хранит данные о координатах строк
        /// </summary>
        IEnumerable<double> rowCoords;

        /// <summary>
        /// Хранит данные о координатах столбцов
        /// </summary>
        IEnumerable<double> columnCoords;

        /// <summary>
        /// Данные о ширине и высоте
        /// </summary>
        double b, h;

        /// <summary>
        /// Допустимая погрешность координат
        /// </summary>
        const double tolerance = 1d;

        /// <summary>
        /// если таблица построена на основе автокадовской то тру
        /// </summary>
        bool from_table = false;

        Table table_ref;

        /// <summary>
        /// Данные об объединенных ячейках
        /// </summary>
        List<Merging> merging;


        public TablePage()
        {
            Initialise();
        }
        public TablePage(List<Entity> data)
        {
            Initialise();
            Recognise(data);
        }
        public TablePage(Table data, bool first)
        {
            Initialise();
            RecognizeFromTable(data, first);
        }
        void Initialise()
        {
            merging = new List<Merging>();
        }

        void RecognizeFromTable(Table data, bool first)
        {
            from_table = true;
            table_ref = data;
            int start_row = first ? 0 : 1;
            textcels = new TextCell[data.Rows.Count - start_row, data.Columns.Count];
            for (int i = start_row; i < data.Rows.Count; i++)
            {
                for (int j = 0; j < data.Columns.Count; j++)
                {
                    //инициализируем каждую ячейку
                    textcels[i - start_row, j] = new TextCell(Convert.ToUInt32(i), Convert.ToUInt32(j));

                    var contents = data.Cells[i, j].Contents;
                    var contents_str = "";
                    foreach (var c in contents)
                    {
                        contents_str += c.TextString;
                        var t = new MText();
                        t.Contents = contents_str;
                        textcels[i - start_row, j].AddTextObject(t);
                    }
                    if (contents_str.Contains("%%U")) textcels[i - start_row, j].UnderLine = true;


                }
            }
            for (int i = start_row; i < data.Rows.Count; i++)
            {
                for (int j = 0; j < data.Columns.Count; j++)
                {
                    var cell = data.Cells[i, j];
                    if (cell.IsMerged.HasValue && cell.IsMerged.Value)
                    {
                        var merged = cell.GetMergeRange();
                        MergeCells(Convert.ToUInt32(i), Convert.ToUInt32(j), Convert.ToUInt32(merged.BottomRow), Convert.ToUInt32(merged.LeftColumn));
                    }
                }
            }
        }

        /// <summary>
        /// Сюда вводим данные о таблице для распознавания - линии и тексты
        /// </summary>
        /// <param name="objects_to_recognise">объекты, прочитанные из выбора</param>
        void Recognise(List<Entity> objects_to_recognise)
        {

            //выбираем объекты с текстом
            var textobjects =
                from acEnt in objects_to_recognise
                where acEnt != null && (acEnt is DBText || acEnt is MText)
                select acEnt;

            //UPD 12.01.19 разделили запрос на линии и на координаты - линии нужны в распознавании объединенных ячеек

            //горизонтальные и вертикальные линии
            var hLines =
                from hl in objects_to_recognise
                where hl is Line && Math.Abs(((Line)hl).Delta.Y) <= tolerance
                orderby ((Line)hl).StartPoint.Y descending
                select ((Line)hl);

            var vLines =
                from vl in objects_to_recognise
                where vl is Line && Math.Abs(((Line)vl).Delta.X) <= tolerance
                orderby ((Line)vl).StartPoint.X
                select ((Line)vl);

            rowCoords = from hl in hLines
                        select hl.StartPoint.Y;
            columnCoords = from vl in vLines
                           select vl.StartPoint.X;

            //удаляем повторяющиеся координаты
            rowCoords = rowCoords.Distinct(new Comparer());
            columnCoords = columnCoords.Distinct(new Comparer());

            //находим ширину и высоту таблицы
            b = columnCoords.Max() - columnCoords.Min();
            h = rowCoords.Max() - rowCoords.Min();



            /*
            var short_horlines=from hl in objects_to_recognise
                where hl is Line && ((Line)hl).Length<b
                select hl as Line;

            var short_vertlines=from vl in objects_to_recognise
                where vl is Line && ((Line)vl).Length<h
                select vl as Line;*/


            //создаем массив ячеек
            textcels = new TextCell[rowCoords.Count() - 1, columnCoords.Count() - 1];

            for (int i = 0; i < rowCoords.Count() - 1; i++)
            {
                for (int j = 0; j < columnCoords.Count() - 1; j++)
                {
                    //инициализируем каждую ячейку
                    textcels[i, j] = new TextCell(Convert.ToUInt32(i), Convert.ToUInt32(j));
                }
            }


            //рассовываем текстовые объекты по ячейкам

            foreach (Entity tc in textobjects)
            {
                //при первом упоминании мтекста записываем его стиль
                if (format == null)
                {
                    if (tc is MText)
                    {
                        format = tc as MText;
                        Tweet("\nЗаписали формат");
                    }
                    if (tc is DBText)
                    {
                        DBText tct = tc as DBText;
                        format = new MText()
                        {
                            TextStyleId = tct.TextStyleId,
                            Layer = tct.Layer,
                            Linetype = tct.Linetype,
                            LineWeight = tct.LineWeight,
                            LinetypeScale = tct.LinetypeScale,
                            TextHeight = tct.Height

                        };
                    }
                }

                Point3d location;

                if (tc is DBText) location = GetCenter((DBText)tc);
                else if (tc is MText) location = GetCenter((MText)tc);

                else { location = Point3d.Origin; continue; }

                //линии для двоичного поиска чтобы использовать существующие сравнители,
                //которые могут сравнивать только линии
                //UPD
                //Здесь тоже применим лямбда-выражения
                //Конструкция Comparer<Line>.Create идет уже из .NET 4.5
                //Линии необходимо создавать чтобы сравнивать одинаковые типы,
                //главное чтобы StartPoint фейковой линии соответствовал положению текста

                //UPD2
                //ввиду общей переделки кода, поиск ведется уже по координатам
                uint row, column;

                try
                {
                    /* //было так
                    row = Convert.ToUInt32(Math.Abs(horLines.BinarySearch(hl, new hlcomparer()))) - 2;
                    column = Convert.ToUInt32(Math.Abs(vertLines.BinarySearch(vl, new vlcomparer()))) - 2;
                     */

                    row = Convert.ToUInt32(Math.Abs(rowCoords.ToList().BinarySearch(
                        location.Y,
                        Comparer<double>.Create((x, y) => -1 * (x.CompareTo(y)))
                    ))) - 2;

                    column = Convert.ToUInt32(Math.Abs(columnCoords.ToList().BinarySearch(
                        location.X,
                        Comparer<double>.Create((x, y) => (x.CompareTo(y)))
                    ))) - 2;

                    textcels[row, column].AddTextObject(tc);

                }
                catch (System.Exception)
                {
                    Messaging.Tweet("Поиск ячейки не удался");
                    continue;
                }




            }
            //распознаем объединения ячеек
            RecognizeMerging(vLines, hLines);

        }

        /// <summary>
        /// Функция поиска центра текста
        /// </summary>
        /// <param name="mt">Мультитекст</param>
        /// <returns>Центральная точка</returns>
        Point3d GetCenter(MText mt)
        {
            Point3d location = Point3d.Origin;

            Point3dCollection points = mt.GetBoundingPoints();

            Point3d tl, br;
            try
            {
                tl = points[0];
                br = points[3];

            }
            catch (System.Exception ex)
            {
                Messaging.Alert("Не удалось прочитать границы текста\n\n" + ex);
                return location;
            }

            double X = tl.X * 0.5 + br.X * 0.5;
            double Y = tl.Y * 0.5 + br.Y * 0.5;

            location = new Point3d(X, Y, 0);


            return location;
        }

        /// <summary>
        /// Функция поиска центра текста
        /// </summary>
        /// <param name="dbt">Текст</param>
        /// <returns>Центральная точка</returns>
        Point3d GetCenter(DBText dbt)
        {
            Point3d location = Point3d.Origin;


            Point3d tl, br;

            Extents3d? nex = dbt.Bounds;

            if (!nex.HasValue)
            {
                Messaging.Alert("Не удалось прочитать границы текста");
                return location;
            }

            tl = nex.Value.MinPoint;
            br = nex.Value.MaxPoint;

            double X = tl.X * 0.5 + br.X * 0.5;
            double Y = tl.Y * 0.5 + br.Y * 0.5;

            location = new Point3d(X, Y, 0);

            return location;
        }
        /// <summary>
        /// Число строк
        /// </summary>
        public int Hcount
        {
            get
            {
                return textcels.GetLength(0);
            }
        }
        /// <summary>
        /// Число столбцов
        /// </summary>
        public int Vcount
        {
            get
            {
                return textcels.GetLength(1);
            }
        }

        public void PageWrite(ref XL.Worksheet workSheet, uint row, uint column)
        {

            foreach (TextCell tc in textcels)
            {
                if (tc.UnderLine)
                {
                    //ставим форматирование текста подчеркнутое
                    //TODO форматирование только фрагмента
                    ((XL.Range)workSheet.Cells[tc.row + row, tc.column + column]).Font.Underline = true;
                }
                //выравнивание текста в ячейке
                //TODO вычислить какое выравнивание в ячейке
                ((XL.Range)workSheet.Cells[tc.row + row, tc.column + column]).HorizontalAlignment = XL.XlHAlign.xlHAlignCenter;

            }
            //на 1000 строк и 4 столбца это занимает целых 6 секунд блять, 6 секунд карл

            string[,] data = new string[Hcount, Vcount];


            for (int i = 0; i < Hcount; i++)
            {
                for (int j = 0; j < Vcount; j++)
                {
                    data[i, j] = textcels[i, j].GetCellContent();

                }
            }
            //на заполнение тратится меньше секунды


            object c1 = workSheet.Cells.Item[row, column];
            object c2 = workSheet.Cells.Item[row + Hcount - 1, column + Vcount - 1];


            var rng = (XL.Range)workSheet.Range[c1, c2];


            rng.Value2 = data;

            //на копирование всех данных 2,5 секунды

            foreach (var m in merging)
            {
                uint row1 = row + m.X1, column1 = column + m.Y1, row2 = row + m.X2, column2 = column + m.Y2;
                object mc1 = workSheet.Cells.Item[row1, column1];
                object mc2 = workSheet.Cells.Item[row2, column2];

                var rng_m = (XL.Range)workSheet.Range[mc1, mc2];
                rng_m.Merge();
            }
        }

        void offset(uint startrow, uint startcolumn)
        {
            foreach (TextCell tc in textcels)
            {
                tc.Offset(startrow, startcolumn);
            }
        }
        public List<TextCell> GetRow(int adress)
        {

            var row = new List<TextCell>();
            try
            {
                for (int i = 0; i < Vcount; i++)
                {
                    row.Add(textcels[adress, i]);
                }
            }
            catch (Exception)
            {
                Tweet(String.Format("\nИсключение вызвано в строке {0}, всего строк {1}", adress, Hcount));
            }
            return row;
        }
        public Rectangle3d CellCoords(int row, int column)
        {
            var col_list = columnCoords.ToList();
            var row_list = rowCoords.ToList();
            Rectangle3d output = new Rectangle3d();
            try
            {
                Point3d upleft = new Point3d(col_list[column], row_list[row], 0);
                Point3d uprigth = new Point3d(col_list[column + 1], row_list[row], 0);
                Point3d lowleft = new Point3d(col_list[column], row_list[row + 1], 0);
                Point3d lowright = new Point3d(col_list[column + 1], row_list[row + 1], 0);
                output = new Rectangle3d(upleft, uprigth, lowleft, lowright);
            }
            catch (ArgumentOutOfRangeException)
            {
                Tweet("\nНе удалось прочитать координаты");
                return output;
            }
            return output;

        }
        public void WriteCell(int row, int column, string content, ObjectId Owner)
        {
            if (from_table)
            {

                using (var actrans = acDoc.TransactionManager.StartTransaction())
                {
                    var opened_table = actrans.GetObject(table_ref.ObjectId, OpenMode.ForWrite) as Table;
                    opened_table.UpgradeOpen();
                    opened_table.Cells[row, column].TextString = content;
                    actrans.Commit();
                }
            }
            else
            {
                var cell = CellCoords(row, column);
                MText mt = format.Clone() as MText;

                mt.Contents = content;

                mt.Width = cell.LowerRight.X - cell.LowerLeft.X;
                mt.Height = cell.UpperLeft.Y - cell.LowerLeft.Y;

                mt.Location = cell.LowerLeft.Add(new Vector3d(mt.Width / 2, mt.Height / 2, 0));
                mt.Attachment = AttachmentPoint.MiddleCenter;

                if (mt.ActualWidth > mt.Width) mt.Contents = @"{\W0.6;" + content + @"}";
                using (var th = new TransactionHelper())
                {
                    th.BlockTablerecord = (th.ReadObject(Owner) as BlockTableRecord).Name;
                    th.WriteObject(mt);
                }
            }
        }
        public void ResetWidth(int column, double heigth, int start, int end)
        {
            using (var actrans = acDoc.TransactionManager.StartTransaction())
            {
                for (int row = start; row <= end; row++)
                {

                    var opened_table = actrans.GetObject(table_ref.ObjectId, OpenMode.ForWrite) as Table;
                    opened_table.UpgradeOpen();
                    if (opened_table.Rows[row].Height > heigth)
                    {
                        var content = opened_table.Cells[row, column].TextString;
                        //надо как-то исключать заголовки
                        if (content.Contains("Масса")) continue;
                        opened_table.Cells[row, column].TextString = @"{\W0.6;" + content + @"}";
                        opened_table.Rows[row].Height = heigth;
                    }

                }
                actrans.Commit();
            }
        }
        public void MergeCells(uint X1, uint Y1, uint X2, uint Y2)
        {
            merging.Add(new Merging() { X1 = X1, Y1 = Y1, X2 = X2, Y2 = Y2 });
            for (uint i = X1; i <= X2; i++)
            {
                for (uint j = Y1; j <= Y2; j++)
                {
                    textcels[i, j].MergeWith(X1, Y1);
                }
            }
        }
        /// <summary>
        /// Определение где не хватает границ отрезков и создание данных об объединенных ячейках
        /// </summary>
        public void RecognizeMerging(IEnumerable<Line> vLines, IEnumerable<Line> hLines)
        {
            var rowList = rowCoords.ToList();
            var columnList = columnCoords.ToList();

            var rowBorders = new byte[Hcount + 1, Vcount];
            foreach (var hl in hLines)
            {
                int rowBorderNumber = SearchInList(rowList, hl.StartPoint.Y, 1);
                int startColumn = SearchInList(columnList, hl.StartPoint.X, -1);
                int endColumn = SearchInList(columnList, hl.EndPoint.X, -1);
                if (endColumn < startColumn)
                {
                    int temp = startColumn;
                    startColumn = endColumn;
                    endColumn = temp;
                }
                for (int i = startColumn; i < endColumn; i++)
                {
                    rowBorders[rowBorderNumber, i] = 1;
                }
            }
            var columnBorders = new byte[Hcount, Vcount + 1];
            foreach (var vl in vLines)
            {
                int columnBorderNumber = SearchInList(columnList, vl.StartPoint.X, -1);
                int startRow = SearchInList(rowList, vl.StartPoint.Y, 1);
                int endRow = SearchInList(rowList, vl.EndPoint.Y, 1);
                if (endRow < startRow)
                {
                    int temp = startRow;
                    startRow = endRow;
                    endRow = temp;
                }
                for (int i = startRow; i < endRow; i++)
                {
                    columnBorders[i, columnBorderNumber] = 1;
                }
            }

            foreach (var c in textcels)
            {
                var merged_cell = FindMergedNumber(Convert.ToInt32(c.row), Convert.ToInt32(c.column), rowBorders, columnBorders);
                if (merged_cell.Item1 != c.row || merged_cell.Item2 != c.column)
                {
                    if (c.Merged()) continue;
                    MergeCells(c.row, c.column, Convert.ToUInt32(merged_cell.Item1), Convert.ToUInt32(merged_cell.Item2));
                }
            }


        }

        Tuple<int, int> FindMergedNumber(int R, int C, byte[,] rowBorders, byte[,] columnBorders)
        {
            int row = R, column = C;

            while (rowBorders[row + 1, C] != 1)
            { row++; }
            while (columnBorders[R, column + 1] != 1) { column++; }
            return new Tuple<int, int>(row, column);
        }
        int SearchInList(List<double> list, double find, int correction)
        {
            int index = list.BinarySearch(find, Comparer<double>.Create((a, b) =>
            {
                if (Math.Abs(a - b) < tolerance) return 0;
                else if (a < b) return 1 * correction;
                else return -1 * correction;
            }));
            if (Math.Abs(index) > list.Count) return list.Count - 1;
            if (index < 0)
            {
                index *= -1;
                double a = Math.Abs(find - list[index]);
                double b = Math.Abs(find - list[index - 1]);
                if (a < b) return index - 1;
            }
            return index;
        }
        public Table MakeACTable()
        {
            var rowCoordsList = rowCoords.ToList();
            var columnCoordsList = columnCoords.ToList();
            Table tbl = new Table();
            tbl.SetSize(Hcount, Vcount);
            tbl.Position = new Point3d(columnCoordsList[0], rowCoordsList[0], 0);
            for (int i = 0; i < tbl.Rows.Count; i++)
            {
                //поменяли индексы потому что получается отрицательная высота
                tbl.Rows[i].Height = rowCoordsList[i] - rowCoordsList[i + 1];
            }
            for (int j = 0; j < tbl.Columns.Count; j++)
            {
                tbl.Columns[j].Width = columnCoordsList[j + 1] - columnCoordsList[j];
            }
            for (int i = 0; i < tbl.Rows.Count; i++)
            {
                for (int j = 0; j < tbl.Columns.Count; j++)
                {
                    var cell = tbl.Cells[i, j];
                    string content = textcels[i, j].GetCellContent();

                    cell.Value = @"{\W" + textcels[i, j].WidthFactor + ";" + content + "}";
                    cell.TextStyleId = format.TextStyleId;
                    cell.TextHeight = format.TextHeight;
                    cell.Alignment = CellAlignment.MiddleCenter;
                    
                }
            }
            foreach(var m in merging)
            {
                tbl.MergeCells(CellRange.Create(tbl,Convert.ToInt32(m.X1), Convert.ToInt32(m.Y1),
                    Convert.ToInt32(m.X2), Convert.ToInt32(m.Y2)));
            }
            
            return tbl;
        }
    }
    struct Merging
    {
        public uint X1, Y1, X2, Y2;
    }
}
