//12.10.2020 есть много вопросов к организации кода, много чего непонятно и неэффективно. Хорошо бы обобщить источники, преобразователи и потребители текста
//а также необходимо обобщить ключевые слова и методы обработки

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

using System.Windows.Forms;

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
	class MultiEditText : FunctionClass
	{
		string[] keywords = {
			"Same", "ОДинаковые",
			"Before", "ПЕред",
			"AFter", "пОСлетекста",
			"Replace", "ЗАменить",
			"XL","эКСель",
			"COpy","КОпировать"};

		/// <summary>
		/// Строка, которая присоединяется перед текстом
		/// </summary>
		static string str_before;

		/// <summary>
		/// Строка, которая присоединяется после текста
		/// </summary>
		static string str_after;

		/// <summary>
		/// Режим ввода текста
		/// </summary>
		static TextEditMode mode = TextEditMode.Different;

		/// <summary>
		/// выбранные объекты
		/// </summary>
		List<Entity> selection = new List<Entity>();

		/// <summary>
		/// Переменная для хранения содержимого текстов
		/// </summary>
		string content = string.Empty;

		/// <summary>
		/// Строка, которую ищем в текстах
		/// </summary>
		string replace = string.Empty;
		string[] contentCollection;

		public override void Execute()
		{
			base.Execute();
			Tweet("\nРежим ввода текстов: " + mode.ToString() + string.Format("\nВвод текста вида {0}<текст>{1}", str_before, str_after));
			
			var sset=Input.Objects("Выберите текстовые объекты или мультивыноски или ",keywords,new SelectionTextInputEventHandler(KeywordInput));
			if (Input.StatusBad) return;
			
			SelectionSet copyTo;
			if(mode==TextEditMode.Copy)
			{
				copyTo = Input.Objects("Выберите объекты, в которые будет произведено копирование текста"); if(Input.StatusBad) return;
				
				
				using( TransactionHelper th = new TransactionHelper())
				{
					selection = th.ReadObjects(sset);
					contentCollection = selection.Where(CheckIsItText).Select(e=>ReadText(e)).ToArray();
					if(contentCollection.Length==0)
					{
						Tweet("Нет текстовых объектов в выборе.");
						return;
					}
					
					foreach(var s in contentCollection)
					{
						Tweet(s);
					}
				}
				var temp = sset;
				sset = copyTo;
				copyTo=temp;
				//первый набор выбора станет источником данных. Второй набор выбора пойдет дальше как обрабатываемый
			}
			

			using (TransactionHelper th = new TransactionHelper())
			{
				selection = th.EditObjects(sset);


				if (mode == TextEditMode.Same)
				{
					string txt= Input.Text("Ведите текст, одинаковый для всех объектов");
					content = string.Concat(str_before, txt, str_after);
				}

				//for (int i = 0; i < selection.Count; i++)
				int i=0, contentNumber=0;
				
				//offtop
				//честно говоря, даже неожиданно просто это выглядит!
				//изначально было так (var o in selection.Where(o=>CheckIsItText(o)))
				//спасибо шд за подсказку
				
				foreach(var o in selection.Where(CheckIsItText))
				{
					switch (mode)
					{
						case TextEditMode.Different:
							string txt = Input.Text("Введите текст номер " + (i + 1));
							content = string.Concat(str_before, txt, str_after);
							break;
						case TextEditMode.Replace:
							string source = UT.GetText(o);
							replace = FindNumber(source);
							content = Input.Text("Введите текст номер " + (i + 1));
							content = source.Replace(replace, content);
							break;
						case TextEditMode.XL:
							content = ReadClipboard(UT.GetText(o));
							if (content == "")
								continue;
							break;
						case TextEditMode.Copy:
							if(contentNumber>=contentCollection.Length) contentNumber=0;
							content=contentCollection[contentNumber];
							contentNumber++;
							
							break;
					}

					ReplaceText(o);
					i++;
				}
				
				var blocks_with_attributes=selection.OfType<BlockReference>().Where(b=>b.AttributeCollection.Count>0).ToList();
				List<string> attribute_names=new List<string>();
				foreach(var b in blocks_with_attributes)
				{
					var ac=b.AttributeCollection;
					foreach(ObjectId attr_o in ac)
					{
						var attr=th.EditObject(attr_o) as AttributeReference;
						if(!(attribute_names.Contains(attr.Tag)))
						{
							attribute_names.Add(attr.Tag);
							Tweet("Добавили тэг "+ attr.Tag);
						}
					}
				}
				
			}

		}
		void KeywordInput(object sender, SelectionTextInputEventArgs e)
		{
			Tweet("Ввели ключевое слово " + e.Input);
			if (e.Input == keywords[0])
			{
				if (mode == TextEditMode.Same) mode = TextEditMode.Different;
				else mode = TextEditMode.Same;
			}
			if (e.Input == keywords[2])
			{
				str_before=Input.Text("Одинаковый текст, присоединяемый перед вводимыми текстами ");
				
			}
			if (e.Input == keywords[4])
			{
				str_after=Input.Text("Одинаковый текст, присоединяемый после вводимых текстов ");
			}
			if (e.Input == keywords[6])
			{
				if (mode == TextEditMode.Replace) mode = TextEditMode.Different;
				else mode = TextEditMode.Replace;
			}
			if (e.Input == keywords[8])
			{
				mode=TextEditMode.XL;
			}
			if (e.Input == keywords[10])
			{
				mode=TextEditMode.Copy;
			}
			//TODO переделать это говно
			
			Tweet("\nРежим ввода текстов: " + mode.ToString() + string.Format("\nВвод текста вида {0}<текст>{1}", str_before, str_after));

		}
		void ReplaceText(Entity text)
		{
			if (text is MText)
			{
				MText mt = (MText)text;

				//TODO:тут можно вынуть существующий текст и обработать форматирование
				//и запихать уже подготовленную строку
				mt.Contents = content;

			}
			if (text is DBText)
			{
				DBText dt = (DBText)text;

				dt.TextString = content;
			}
			if (text is MLeader)
			{
				MLeader ml = (MLeader)text;
				MText aa = ml.MText.Clone() as MText;

				aa.Contents = content;
				ml.MText = aa;


			}
			if (text is Leader)
			{
				Leader ld = (Leader)text;

				using (TransactionHelper th = new TransactionHelper())
				{
					MText ann = th.EditObject(ld.Annotation) as MText;

					ann.Contents = content;
				}

			}
		}
		bool CheckIsItText(Entity ent)
		{
			//необходимо дописать поверку на заблокированный слой!
			if ((ent is MText) || (ent is DBText))
			{
				return true;
			}

			if (ent is Leader)
			{
				Leader ld = ent as Leader;
				if (ld.AnnoType == AnnotationType.MText)
				{
					return true;
				}
			}
			if (ent is MLeader)
			{
				MLeader ml = ent as MLeader;
				if (ml.ContentType == ContentType.MTextContent)
				{
					return true;
				}
			}
			return false;
		}
		string ReadText(Entity text)
		{
			text.UpgradeOpen();
			if (text is MText)
			{
				MText mt = (MText)text;

				//TODO:тут можно вынуть существующий текст и обработать форматирование
				//и запихать уже подготовленную строку
				return mt.Contents;

			}
			if (text is DBText)
			{
				DBText dt = (DBText)text;

				dt.TextString = content;
			}
			if (text is MLeader)
			{
				MLeader ml = (MLeader)text;
				return ml.MText.Contents;

			}
			if (text is Leader)
			{
				Leader ld = (Leader)text;

				using (TransactionHelper th = new TransactionHelper())
				{
					MText ann = th.EditObject(ld.Annotation) as MText;

					return ann.Contents;
				}
			}
			return "";
		}
		
		
		/*
		/// <summary>
		/// из буфера обмена
		/// </summary>
		string text;
		string[] rows;
		List<string> keys;
		List<string> values;
		string ReadClipboard(string toreplace)
		{
			int index;
			if(text==null)
			{
				text=Clipboard.GetText();
				if(text!=null)
				{
					rows=text.Split('\n');
					
					//берем первые 2 строки из экселя - старые значения и новые
					keys=rows[0].Split('\t').ToList();
					values=rows[1].Split('\t').ToList();
				}
			}
			try
			{
				index=keys.IndexOf(toreplace);
				return values[index];
			}
			catch(System.Exception)
			{
				return "";
			}
		}*/
		
		SingleKeyTableSource ts;
		string ReadClipboard(string toreplace)
		{
			try
			{
				if(ts==null) ts = new SingleKeyTableSource(Clipboard.GetText());
				return ts[toreplace];
			}
			
			catch(KeyNotFoundException)
			{
				Tweet("Пропущен текст не найденный в словаре: "+toreplace);
				return toreplace;
			}
			catch(Exception ex)
			{
				Tweet(ex);
				return "";
			}
		}

		string FindNumber(string source)
		{
			string pattern = @"\d+";

			Regex r = new Regex(pattern);
			Match m = r.Match(source);
			
			if(m.Success) return m.Value;

			return "";
		}
	}
	/// <summary>
	/// Переключение режимов ввода текста
	/// </summary>
	enum TextEditMode
	{
		Same,
		Replace,
		Different,
		XL,
		Copy,
	}
}
