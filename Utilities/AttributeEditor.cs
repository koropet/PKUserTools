/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 09.01.2019
 * Время: 9:32
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using XL=Microsoft.Office.Interop.Excel;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;

using System.Windows.Forms;
using App=Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Description of AttributeEditor.
	/// </summary>
	public static class AttributeEditor
	{
		static Dictionary<string, string[]> dict;
		#region MainFunctions

		static void AppendAttributesInBlockTableRecord(string space)
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			
			using(var acTrans=acDoc.TransactionManager.StartTransaction())
			{
				var blockTable=acTrans.GetObject(acCurDb.BlockTableId,OpenMode.ForWrite) as BlockTable;
				
				var blockTableRecord=acTrans.GetObject(blockTable[space],OpenMode.ForWrite) as BlockTableRecord;
				
				//все блоки с листа (или из блока)
				int number=0;
				foreach(ObjectId id in blockTableRecord)
				{
					var ent=acTrans.GetObject(id,OpenMode.ForRead) as BlockReference;
					if(ent==null) continue;
					ent.UpgradeOpen();
					AppendAttributes(acTrans,ent,number++);
				}
				acTrans.Commit();
			}
		}
		
		static void AppendAttributesInSelection()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var sset=Input.Objects("Выберите блоки для редактирования аттрибутов"); if(Input.StatusBad) return;
			
			using(var acTrans=acDoc.TransactionManager.StartTransaction())
			{
				var blockTable=acTrans.GetObject(acCurDb.BlockTableId,OpenMode.ForWrite) as BlockTable;
				var block_refs=new List<Entity>();
				int number=0;
				for(int i=0;i<sset.Count;i++)
				{
					var id=sset[i].ObjectId;
					var br=acTrans.GetObject(id,OpenMode.ForRead) as BlockReference;
					if(br==null) continue;
					br.UpgradeOpen();
					block_refs.Add(br);
					
					AppendAttributes(acTrans,br,number++);
				}
				acTrans.Commit();
			}
		}
		
		static void AppendAttributesByKey()
		{
			string key=dict.Keys.First();
			
			//сопоставление значений ключевых аттрибутов и номеров в массиве значений
			var key_to_number=new Dictionary<string, int>();
			
			//заполняем сопоставление
			var key_strings = dict[key];
			int count=key_strings.GetLength(0);
			for(int i=0; i<count;i++)
			{
				key_to_number.Add(key_strings[i],i);
			}
			
			//дальше то же самое, что и при выборе, только номер не по порядку, а в зависимости от ключа
			
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var sset=Input.Objects("Выберите блоки для редактирования аттрибутов"); if(Input.StatusBad) return;
			
			using(var acTrans=acDoc.TransactionManager.StartTransaction())
			{
				var blockTable=acTrans.GetObject(acCurDb.BlockTableId,OpenMode.ForWrite) as BlockTable;
				var block_refs=new List<Entity>();
				
				for(int i=0;i<sset.Count;i++)
				{
					var id=sset[i].ObjectId;
					var br=acTrans.GetObject(id,OpenMode.ForRead) as BlockReference;
					if(br==null) continue;
					br.UpgradeOpen();
					
					string key_from_blockref=ReadAttribute(acTrans,br,key);
					
					block_refs.Add(br);
					
					if(key_to_number.ContainsKey(key_from_blockref))
					{
						AppendAttributes(acTrans,br,key_to_number[key_from_blockref]);
					}
					else
					{
						Messaging.Tweet("Не найден ключ в словаре. Блок оставлен без изменений");
					}
				}
				acTrans.Commit();
			}
		}
		
		static void AppendAttributesByKeyInBlockTableRecord(string space)
		{
			string key=dict.Keys.First();
			Console.WriteLine(key);
			//сопоставление значений ключевых аттрибутов и номеров в массиве значений
			var key_to_number=new Dictionary<string, int>();
			
			//заполняем сопоставление
			var key_strings = dict[key];
			int count=key_strings.GetLength(0);
			for(int i=0; i<count;i++)
			{
				
				Console.WriteLine(key_strings[i]);
				key_to_number.Add(key_strings[i],i);
			}
			
			//дальше то же самое, что и при выборе, только номер не по порядку, а в зависимости от ключа
			
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			using(var acTrans=acDoc.TransactionManager.StartTransaction())
			{
				var blockTable=acTrans.GetObject(acCurDb.BlockTableId,OpenMode.ForWrite) as BlockTable;
				
				var blockTableRecord=acTrans.GetObject(blockTable[space],OpenMode.ForWrite) as BlockTableRecord;
				
				//все блоки с листа (или из блока)
				foreach(ObjectId id in blockTableRecord)
				{
					var ent=acTrans.GetObject(id,OpenMode.ForRead) as BlockReference;
					if(ent==null) continue;
					ent.UpgradeOpen();
					
					string key_from_blockref=ReadAttribute(acTrans,ent,key);
					if(key_to_number.ContainsKey(key_from_blockref))
					{
						AppendAttributes(acTrans,ent,key_to_number[key_from_blockref]);
					}
					else
					{
						Messaging.Tweet("Не найден ключ в словаре. Блок оставлен без изменений");
					}
				}
				acTrans.Commit();
			}
		}
		#endregion
		/// <summary>
		/// Читаем из блока значение аттрибута по ключу
		/// </summary>
		/// <param name = "th">текущая транзакция, блокреф должен быть открыт для чтения</param>
		/// <param name="blockRef">блок</param>
		/// <param name="key">имя ключа</param>
		/// <returns>Значение аттрибута. Пустая строка если не найден атрибут по улючу</returns>
		static string ReadAttribute(Transaction th, BlockReference blockRef, string key)
		{
			try
			{
				foreach(ObjectId attRef_id in blockRef.AttributeCollection)
				{
					var attRef =th.GetObject(attRef_id,OpenMode.ForWrite) as AttributeReference;
					
					if (attRef.Tag==key)
					{
						return attRef.TextString;
					}
				}
				return "";
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				return "";
			}
		}
		
		/// <summary>
		/// Строим словарь из таблицы в буфере обмена. Для построения верной таблицы в 1 столбце помещаются наименования аттрибутов, затем массивы значений для редактирования
		/// </summary>
		static void MakeDictionaryFromExcel()
		{
			dict=new Dictionary<string, string[]>();
			
			string text=Clipboard.GetText();
			
			//TODO add check for input
			
			if(!settings.vertical)
			{
				var splitted=text.Split('\n');
				
				int count=splitted.GetLength(0);
				foreach(var str in splitted)
				{
					int len=str.Length;
					if(len<1) continue;
					
					var str_rem=str.Remove(len-1);
					
					var arr=str_rem.Split('\t');
					dict.Add(arr[0],arr.Skip(1).ToArray());
				}
			}
			else
			{
				var splitted=text.Split('\n');
				var splitted_table =
					(from s in splitted
					 select s.Split('\t')).ToArray();
				
				int width = splitted_table[0].GetLength(0);
				int heigth = splitted_table.GetLength(0)-1; //лютый костыль, да
				Console.WriteLine("w {0} h {1}",width,heigth);
				
				for(int i=0;i<width;i++)
				{
					var list = new List<string>(heigth);
					for(int j=1;j<heigth;j++)
					{
						list.Add(splitted_table[j][i]);
					}
					dict.Add(splitted_table[0][i],list.ToArray());
				}
				
			}
		}

		static List<string> my_layouts;
		static AttributeEditorSettings settings=new AttributeEditorSettings();
		
		public static void EditAttributes()
		{
			try
			{
				//создае  словарь из буфера обмена
				MakeDictionaryFromExcel();
			}
			catch(Exception ex)

			{
				Console.WriteLine(ex);
				Messaging.Tweet("Не удалось создать словарь. Проверьте, не пустой ли буфер обмена");
			}
			my_layouts = GetLayouts();
			try
			{
				//надо все-таки будет сделать через перечисление
				if(settings.byKey)
				{
					if(settings.bySelection)
					{
						AppendAttributesByKey();
					}
					else
					{
						AppendAttributesByKeyInBlockTableRecord("*Model_Space");
						foreach(var str in my_layouts)
						{
							AppendAttributesByKeyInBlockTableRecord(btr_from_layout(str));
						}
					}
				}
				else
				{
					if (settings.bySelection)
					{
						AppendAttributesInSelection();
					}
					else
					{
						AppendAttributesInBlockTableRecord("*Model_Space");
						foreach (var str in my_layouts)
						{
							AppendAttributesInBlockTableRecord(btr_from_layout(str));
						}
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
		static List<string> GetLayouts()
		{
			var layouts = new List<string>();
			//Код с автодеска с некоторыми поправками - пишем не в эдитор, а добавляем в список

			// Get the current document and database
			Document acDoc = App.DocumentManager.MdiActiveDocument;
			Database acCurDb = acDoc.Database;

			// Get the layout dictionary of the current database
			using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
			{
				DBDictionary lays =
					acTrans.GetObject(acCurDb.LayoutDictionaryId,
					                  OpenMode.ForRead) as DBDictionary;
				

				// Step through and list each named layout and Model
				foreach (DBDictionaryEntry item in lays)
				{
					layouts.Add(item.Key);
				}

				// Abort the changes to the database
				acTrans.Abort();
			}
			
			return layouts;
		}
		public static void AttributeEditorSettingsSet()
		{
			settings.SetFromEditor();
		}
		
		static string btr_from_layout(string LayoutName)
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var lm=LayoutManager.Current;
			ObjectId layout_id=ObjectId.Null;
			try
			{
				layout_id=lm.GetLayoutId(LayoutName);
			}
			catch(Exception)
			{
				Console.WriteLine("Не найден лист с названием "+ LayoutName);
			}
			
			using(var acTrans=acDoc.TransactionManager.StartTransaction())
			{
				var blockTable=acTrans.GetObject(acCurDb.BlockTableId,OpenMode.ForRead) as BlockTable;
				
				var layout=acTrans.GetObject(layout_id,OpenMode.ForRead) as Layout;
				var btr=acTrans.GetObject(layout.BlockTableRecordId,OpenMode.ForRead) as BlockTableRecord;
				return btr.Name;
			}
			
		}
		
		/// <summary>
		/// Редактирует аттрибуты используя словарь. Ключ словаря=имя атрибута. Значение словаря массив строк, в котором значения выбираются по номеру (номер блока в выборе)
		/// </summary>
		/// <param name="th"></param>
		/// <param name="blockRef"></param>
		/// <param name="number"></param>
		static void AppendAttributes(Transaction th, BlockReference blockRef, int number)
		{
			var blockDef=th.GetObject(blockRef.BlockTableRecord,OpenMode.ForRead) as BlockTableRecord;
			
			foreach(ObjectId attRef_id in blockRef.AttributeCollection)
			{
				var attRef =th.GetObject(attRef_id,OpenMode.ForWrite) as AttributeReference;
				
				if (dict.ContainsKey(attRef.Tag))
				{
					var arr=dict[attRef.Tag];
					//при переполнении номера используем остаток от деления.
					//Таким образом, если у нас 20 блоков и 5 записей в таблице, то аттрибуты запишутся циклически по 4 раза
					attRef.TextString=arr[number%arr.Length];
				}
			}
		}
		
		public static void ListAttributes()
		{
			var contents = new List<string>();
			var tags = new List<string>();
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var sset=Input.Objects("Выберите блоки, из которых нужно прочитать имена аттрибутов"); if(Input.StatusBad) return;
			using( var th=new TransactionHelper())
			{
				var blockrefs=th.ReadObjects(sset).OfType<BlockReference>();
				foreach(var br in blockrefs)
				{
					foreach(ObjectId attRef_id in br.AttributeCollection)
					{
						var attRef =th.EditObject(attRef_id) as AttributeReference;
						if(tags.Contains(attRef.Tag)) continue;
						else
						{
							tags.Add(attRef.Tag);
							contents.Add(attRef.TextString);
						}
					}
				}
			}
			
			string output="";
			
			for(int i=0;i<tags.Count;i++)
			{
				output+=tags[i]+"\t"+contents[i]+"\n";
			}
			
			Console.WriteLine(output);
			
			Clipboard.SetText(output);
		}
		public static void MoveAttributes()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var sset=Input.Objects("Выберите блоки"); if(Input.StatusBad) return;
			using( var th=new TransactionHelper())
			{
				var blockrefs=th.EditObjects(sset).OfType<BlockReference>();
				foreach(var br in blockrefs)
				{
					foreach(ObjectId attRef_id in br.AttributeCollection)
					{
						var attRef = th.EditObject(attRef_id) as AttributeReference;
						Messaging.Tweet("Position before: " + attRef.Position);
						attRef.Position = attRef.Position.Add(new Vector3d(50,0,0));
						Messaging.Tweet("Position after: " + attRef.Position);
					}
				}
			}
		}
	}
	internal class AttributeEditorSettings
	{
		public bool byKey, bySelection, vertical;
		public AttributeEditorSettings(bool byKey,bool bySelection, bool vertical)
		{
			this.byKey = byKey;
			this.bySelection = bySelection;
			this.vertical=vertical;
		}
		public AttributeEditorSettings() { }
		/// <summary>
		/// Метод задания режима
		/// </summary>
		/// <returns>0 если не удалось, 1 если ок</returns>
		public bool SetFromEditor()
		{
			var key = Input.Keyword("Выберите режим ", new[] { "КЛюч", "KEy","ВЫбор","SElection","ВЕртикально","VErtical" }); if (Input.StatusBad) return false;
			switch (key)
			{
				case "КЛюч":
					{
						byKey = !byKey;
						Messaging.Tweet("Режим ввода с ключом " + byKey);
						return true;
					}
				case "ВЫбор":
					{
						bySelection = !bySelection;
						Messaging.Tweet("Режим ввода с выбором " + bySelection);
						return true;
					}
				case "ВЕртикально":
					{
						vertical = !vertical;
						Messaging.Tweet("Режим ориентации таблицы " + vertical);
						return true;
					}
					default: return false;
			}
		}
	}
	
}
