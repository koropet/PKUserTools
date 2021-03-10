/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 17.09.2018
 * Время: 14:41
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.Windows.Data;
using Autodesk.AutoCAD.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.EditorInput
{
	/// <summary>
	/// Функции пользовательского ввода в упрощенной форме
	/// </summary>
	public static class Input
	{
		static PromptStatus status;
		
		static public string StringResult;
		
		static Document acDoc;
		static Editor acEd;
		static Database acCurDb;
		static bool ImpliedFirst=false;
		
		/// <summary>
		/// Ввод точки из редактора
		/// </summary>
		/// <param name="message">Сообщение пользователю</param>
		/// <param name="keywords">Ключевые слова</param>
		/// <returns></returns>
		public static Point3d Point(string message,string[] keywords = null,Action<string> keywordinput=null)
		{
			Init();

            var mcs = acEd.CurrentUserCoordinateSystem; //решение проблемы с местной координатной системой (уплывание новых объектов)
            //mcs = mcs.Inverse(); //инвертировать не нужно, так как это ввод точки с экрана. При вставке объектов нужно обратное преобразование.(см. PatternCopy)

            var ppo=new PromptPointOptions(message);
			if(keywords!=null)
			{
				for (int i = 0; i < keywords.GetLength(0); i++)
				{
					ppo.Keywords.Add(keywords[i], keywords[++i]);
				}
			}
			
			ppo.AllowNone=true;
			ppo.AppendKeywordsToMessage=true;
			
			var result=acEd.GetPoint(ppo);
			status=result.Status;
			StringResult=result.StringResult;
			
			if(StatusKeyword)
			{
				keywordinput(StringResult);
				return Point(message,keywords,keywordinput);
			}
			
			return result.Value.TransformBy(mcs);
			
		}
		
		public static SelectionSet Objects(string message,string[] keywords = null, SelectionTextInputEventHandler keywordinput=null)
		{
			Init();

            var pso=new PromptSelectionOptions();
			
			if(keywords!=null)
			{
				for (int i = 0; i < keywords.GetLength(0); i++)
				{
					pso.Keywords.Add(keywords[i], keywords[++i]);
				}
			}
			pso.MessageForAdding=message+pso.Keywords.GetDisplayString(true);
			
			if(keywordinput!=null) pso.KeywordInput+=keywordinput;
			
			var result = acEd.SelectImplied();
			
			if(result.Status==PromptStatus.OK&!ImpliedFirst)
			{
				
				Console.WriteLine("Зашли в имплайед");
				status=result.Status;
				ImpliedFirst=true;
				return result.Value;
			}
			else
			{
				result=acEd.GetSelection(pso);
				
				status=result.Status;
				return result.Value;
			}
			
			
		}
        public static SelectionSet Implied()
        {
            Init();

            var result = acEd.SelectImplied();

            if (result.Status == PromptStatus.OK)
            {
                Console.WriteLine("Зашли в имплайед");
            }
            status = result.Status;
            return result.Value;


        }
        public static string Keyword(string message,string[] keywords = null)
		{
			Init();
			var pko=new PromptKeywordOptions(message);
			if(keywords!=null)
			{
				for (int i = 0; i < keywords.GetLength(0); i++)
				{
					pko.Keywords.Add(keywords[i], keywords[++i]);
				}
			}
			pko.AppendKeywordsToMessage=true;
			
			var pkr=acEd.GetKeywords(pko);
			
			status=pkr.Status;
			StringResult=pkr.StringResult;
			return StringResult;
			
		}
		
		public static double Double(string message,string[] keywords = null)
		{
			Init();
			var pdo=new PromptDoubleOptions(message);
			if(keywords!=null)
			{
				for (int i = 0; i < keywords.GetLength(0); i++)
				{
					pdo.Keywords.Add(keywords[i], keywords[++i]);
				}
				pdo.AppendKeywordsToMessage=true;
			}
			
			var pdr=acEd.GetDouble(pdo);
			
			status=pdr.Status;
			StringResult=pdr.StringResult;
			return pdr.Value;
		}
		
		public static int Integer(string message,string[] keywords = null)
		{
			Init();
			var pio=new PromptIntegerOptions(message);
			if(keywords!=null)
			{
				for (int i = 0; i < keywords.GetLength(0); i++)
				{
					pio.Keywords.Add(keywords[i], keywords[++i]);
				}
				pio.AppendKeywordsToMessage=true;
			}
			var pir=acEd.GetInteger(pio);
			
			status=pir.Status;
			StringResult=pir.StringResult;
			return pir.Value;
		}
		
		public static string Text(string message,string[] keywords = null)
		{
			Init();
			var pso=new PromptStringOptions(message);
			if(keywords!=null)
			{
				for (int i = 0; i < keywords.GetLength(0); i++)
				{
					pso.Keywords.Add(keywords[i], keywords[++i]);
				}
				pso.AppendKeywordsToMessage=true;
			}
			var res=acEd.GetString(pso);
			
			status=res.Status;
			StringResult=res.StringResult;
			return StringResult;
		}
		
		static void Init()
		{
			acDoc = App.DocumentManager.MdiActiveDocument;
			acCurDb = acDoc.Database;
			acEd = acDoc.Editor;
			status=PromptStatus.Other;
			StringResult=string.Empty;
		}
		public static bool StatusBad
		{
			get
			{
				return status!=PromptStatus.OK;
			}
		}
		public static bool StatusOK
		{
			get
			{
				return status==PromptStatus.OK;
			}
		}
		public static bool StatusKeyword
		{
			get
			{
				return status==PromptStatus.Keyword;
			}
		}
		public static bool StatusCancel
		{
			get
			{
				return status==PromptStatus.Cancel;
			}
		}
		public static bool SelectedImplied
        {
            get
            {
                return ImpliedFirst;
            }
        }
	}
}
