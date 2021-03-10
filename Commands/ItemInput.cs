using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

namespace PKUserTools.Commands.ItemInput
{
	class ItemInput:FunctionClass
	{
		static double[] diameters = { 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40 };
		static Hashtable diamtomassa500;
		static Hashtable gost;
		static double[] massesA500 = { 0.099, 0.154, 0.222, 0.395, 0.616, 0.888, 1.208, 1.578, 1.998, 2.466, 2.984, 3.853, 4.834, 6.313, 7.99, 9.865 };
		
		static Sortament a500=Sortament.A500();
		static Sortament a240=Sortament.A240();


		const string MyItemKey = "PKpositions";

		static public ItemInputOptions iio;
		static public ItemInputMode iim = ItemInputMode.Constant;
		

		/// <summary>
		/// Пользовательские данные чертежа
		/// </summary>
		static Hashtable userdata;

		static List<ArmaturItem> items = new List<ArmaturItem>();

		List<Entity> objects = new List<Entity>();

		

		List<Line> lines = new List<Line>();

		//текущая позиция. По умолчанию при первом запуске программы прямой
		ArmaturItem pos = new StraitArmaturItem();
		

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
			if (items == null) items = new List<ArmaturItem>(); //при первом использовании заводим список позиций
			Tweet(iim.ToString());


			var sset=Input.Objects( "Выберите объекты для добавления позиции",ItemKeywords, new SelectionTextInputEventHandler(KeywordInput));
			
			if (Input.StatusBad) return;
			using (TransactionHelper th = new TransactionHelper())
			{
				objects = th.ReadObjects(sset);
			}
			CreatePos();
			
			pos.Input(objects);
			if (pos.StatusBad) return;
			pos.GOST = EnterGOST();
			pos.diameter = Convert.ToByte(EnterDiameter());

			pos.name=Input.Text("\n Введите номер позиции");

			items.Add(pos);
			
			
		}
		void KeywordInput(object s, SelectionTextInputEventArgs e)
		{
			//анализ ключевых слов
			if (e.Input == "Strait") iim = ItemInputMode.Constant;
			else if (e.Input == "BEnd") iim = ItemInputMode.Bend;
			else if (e.Input == "VBend") iim = ItemInputMode.BendVar;
			else if (e.Input == "Middle") iim = ItemInputMode.Variable;
			else if (e.Input == "ALllength") iim = ItemInputMode.All;
			else if (e.Input == "OUtput")
			{
				Output();
			}
			else if (e.Input == "CLear") ClearItems();
			else if (e.Input == "PArams") SetParams();
			else if (e.Input == "EDit") iio.EditingMode = !iio.EditingMode;

		}
		void CreatePos()
		{
			ArmaturItem.iio = iio;

			if (iim==ItemInputMode.Constant) pos = new StraitArmaturItem();
			else if (iim==ItemInputMode.Bend) pos = new BendArmaturItem();
			else if (iim==ItemInputMode.BendVar) pos = new BendVarArmaturItem();
			else if (iim==ItemInputMode.Variable) pos = new VariableArmaturItem();
			else if (iim==ItemInputMode.All) pos = new AllArmaturItem();
		}
		//а вот эти методы по сути заменяют целую кучу кода
		//если переделать код функции дополнения спецификации, эти классы вовсе не нужны
		/// <summary>
		/// Вывод через запросы
		/// </summary>
		void Output()
		{
			var spitems=items.ToLookup(i=>i.GroupName).OrderByDescending(g=>g.Key);
			
			string result="";
			foreach(var g in spitems)
			{
				result+=GroupLine(g);
				
				foreach(var item in g)
				{
					result+=item.SPposition()+"\t"+item.SPname()+"\t"+item.SPcount()+"\t"+item.SPmass()+"\n";
				}
			}
			Clipboard.SetText(result);
			
		}
		
		/// <summary>
		/// Формирование строки спецификации для группы арматурных стержней
		/// </summary>
		/// <param name="g">группы</param>
		/// <returns>строка с табуляциями и переносом для вставки в эксель</returns>
		string GroupLine(IGrouping<string,ArmaturItem> g)
		{
			var item=g.ToList()[0];
			return string.Format("\t{0}\t\t\n",item.GroupName);
			
		}
		
		void ClearItems()
		{
			items = new List<ArmaturItem>();

			if (userdata.Contains(MyItemKey))
			{
				userdata.Remove(MyItemKey);
			}
			return;
		}
		void SetParams()
		{
			string keyword=Input.Keyword(message:"Укажите опцию",
			                             keywords: new string[] {"COunt", "КОличество"});
			if (Input.StatusBad) return;
			if(Input.StringResult=="COunt")
			{
				keyword=Input.Keyword(message:"Выберите режим ввода количества",
				                      keywords: new string[]
				                      {"Manual", "ВРучную",
				                      	"SUmm", "СУммирование"});
				if (Input.StatusBad) return;
				
				if (Input.StringResult == "Manual")
				{
					iio.ManualCountInput = !iio.ManualCountInput;
					Tweet(iio.ManualModeMsg);
				}
				if (Input.StringResult == "SUmm")
				{
					iio.SumCountInput = !iio.SumCountInput;
					Tweet(iio.SumModeMsg);
				}
			}
			
			
		}
		public static void InitStaticData()
		{
			//TODO переделать также под LINQ работу с таблицами масс
			
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
				Messaging.Tweet("Найдены данные о позициях");
				items = (List<ArmaturItem>)userdata[MyItemKey];
			}
			else
			{

				Messaging.Tweet("Данные о позициях в чертеже не найдены");
			}
		}
		public static double GetMass(double diameter)
		{
			if (!diamtomassa500.ContainsKey(diameter))
			{
				Messaging.Tweet("Не найден диаметр. По умолчанию установлена масса 0");
				return 0;
			}
			return (double)diamtomassa500[diameter];
		}
		public static string EnterGOST()
		{
			string key=Input.Keyword("Выберите класс арматуры", new string[] {"A500","A500","A240","A240"});

			if (gost.ContainsKey(key))
			{
				return (string)gost[key];
			}
			else return "";
		}
		public static double EnterDiameter()
		{
			
			double diam=Input.Double("Введите диаметр");
			
			int ind = Array.IndexOf(diameters, diam);
			if (ind >= 0) return diameters[ind];
			else return 0;

		}
		/// <summary>
		/// Ввод количества объектов в позиции
		/// </summary>
		/// <returns></returns>
		public static uint ItemCount()
		{
			int count=0;
			int sum=0;
			
			if (iio.ManualCountInput)
			{
				count = Input.Integer("Укажите количество"); if(Input.StatusBad) return 0;
				
				if (iio.SumCountInput)
				{
					while (Input.StatusOK)
					{
						count = Input.Integer("Укажите количество");  if(Input.StatusBad) break;
						Messaging.Tweet(string.Format("Добавили {0} к количеству. Всего {1} шт", count, sum+=count));
					}
					return Convert.ToUInt32(sum);
				}
				else return Convert.ToUInt32(count);
			}
			else
			{
				count=Input.Objects("Выберите объекты. Количество объектов равно количеству в позиции").Count; if(Input.StatusBad) return 0;
				if (iio.SumCountInput)
				{
					while (Input.StatusOK)
					{
						var count_ss=Input.Objects("Выберите объекты. Количество объектов равно количеству в позиции"); if(Input.StatusBad) break;
						count=count_ss.Count;
						Messaging.Tweet(string.Format("Добавили {0} к количеству. Всего {1} шт", count, sum+=count));
					}
					return Convert.ToUInt32(sum);
				}
				else return Convert.ToUInt32(count);
			}
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
	public enum ItemInputMode
	{
		Constant,
		Variable,
		All,
		Bend,
		BendVar,
	}
}
