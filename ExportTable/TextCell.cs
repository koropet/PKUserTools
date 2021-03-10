using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;

namespace PKUserTools.ExportTable
{
    /// <summary>
    /// Форма данных для хранения данных о тексте и ячейке, в которой он содержится
    /// </summary>
    public class TextCell
    {
        public uint row;
        public uint column;

        Tuple<uint,uint> merge_data;

        List<Entity> TextObjects;

        //формат с подчеркиванием временно храним тут, позже можно сделать более культурно
        public bool UnderLine = false;

        public double WidthFactor;

        /// <summary>
        /// Инициализирует новый объект, содержащий данные о ячейке
        /// </summary>
        /// <param name="row">Строка, в которой содержится ячейка</param>
        /// <param name="column">Столбец, в котром содержится ячейка</param>
        public TextCell(uint row, uint column)
        {
            this.row = row;
            this.column = column;
            TextObjects = new List<Entity>();
        }
        /// <summary>
        /// Добавляем текстовый объект в набор
        /// </summary>
        /// <param name="text">Текстовый объект, который следует добавить</param>
        public void AddTextObject(Entity text)
        {
            TextObjects.Add(text);
        }
        /// <summary>
        /// Возвращает строку, собранную из текстовых объектов. Если объектов нет, на выходе пустая строка.
        /// </summary>
        /// <returns>Результат</returns>
        public string GetCellContent()
        {
            //здесь можно обрабатывать данные

            string result = String.Empty;
            if (TextObjects.Count == 0) return result;



            TextObjects.Sort(new TextComparerIncell());

            foreach (Entity ent in TextObjects)
            {
                string text = String.Empty;
                if (ent is DBText) text = ((DBText)ent).TextString;
                if (ent is MText)
                {
                    text = ParceMText((MText)ent);
                }

                if (result == string.Empty) result = text;
                else result = string.Concat(result, " ", text);
            }

            //попробуем отсеять специальные символы%%
            //TODO: нужно преобразовывать не только символы, но и форматирование %%U

            string[] for_replace = new string[] { "%%C", "  " };
            string[] replace = new string[] { "∅", " " };

            //пока что работает просто в лоб, но можно сделать и быстрее
            for (int i = 0; i < for_replace.GetLength(0); i++)
            {
                result = result.Replace(for_replace[i], replace[i]);
            }
            if (result.Contains("%%U"))
            {
                //отслеживаем подчеркнутый текст

                result = result.Replace("%%U", "");
                UnderLine = true;
            }

            WidthFactor = TextWidthFactor(TextObjects);

            return result;
        }

        double TextWidthFactor(List<Entity> text_objects)
        {
            double default_w = 0.8;
            return text_objects.Min(t =>
            {
                var DT = t as DBText;
                if (DT != null) return DT.WidthFactor;
                var MT = t as MText;
                if (MT != null)
                {
                    var width_str = Regex.Match(MT.Contents, @"\\[W]\d\.\d+").Value;
                    width_str = Regex.Match(width_str, @"\d\.\d+").Value;
                    double w;
                    if (double.TryParse(width_str, out w)) return w;

                    return default_w;
                }
                return default_w;
            });
        }

        /// <summary>
        /// Проверка, является ли ячейка пустой
        /// </summary>
        /// <returns>Истинно, если пустая</returns>
        public bool IsEmpty()
        {
            if (TextObjects.Count == 0) return true;
            return false;
        }
        /// <summary>
        /// Сдвигает номер ячейки
        /// </summary>
        /// <param name="StartRow">Первая строка</param>
        /// <param name="StartColumn">Первый столбец</param>
        public void Offset(uint StartRow, uint StartColumn)
        {
            this.row += StartRow;
            this.column += StartColumn;
        }
        /// <summary>
        /// Сюда пишем результат обработки МТекста
        /// </summary>
        static string result = String.Empty;
        /// <summary>
        /// Выделение из МТекста фрагментов и объединение их в строку
        /// </summary>
        /// <param name="mt"></param>
        /// <returns></returns>
        string ParceMText(MText mt)
        {

            result = String.Empty;
            mt.ExplodeFragments(Fragments);


            return result;
        }

        static public MTextFragmentCallbackStatus
            Fragments(MTextFragment Param, object data)
        {
            if (result == string.Empty) result = Param.Text;
            else result = string.Concat(result, " ", Param.Text);

            return MTextFragmentCallbackStatus.Continue;
        }

        /// <summary>
        /// Соединяем с ячейкой по адресу
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void MergeWith(uint row,uint column)
        {
            merge_data = new Tuple<uint,uint>(row, column);
        }

        /// <summary>
        /// Возврат соединения
        /// </summary>
        public void Unmerge()
        {
            merge_data = new Tuple<uint, uint>(row, column);
        }

        public bool Merged()
        {
            if (merge_data == null) return false;
            else if (merge_data.Item1 == row && merge_data.Item2 == column) return false;
            return true;
        }
    }
}
