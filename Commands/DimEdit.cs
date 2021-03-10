/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 10.09.2018
 * Время: 12:50
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
	/// <summary>
	///  Класс для функций реадктирования текста
	/// </summary>
	class DimEdit:FunctionClass
	{
		List<Entity> objects;
		public DimEdit()
		{
		}
		/// <summary>
		/// Подчеркивание размеров
		/// </summary>
		public void UnderlineDim()
		{
			var sset=Input.Objects("Выберите размеры, которые необхоидмо подчеркнуть"); if(Input.StatusBad) return;
			
			using(var th=new TransactionHelper())
			{
				objects=th.EditObjects(sset);
				
				foreach(Entity ent in objects)
				{
					if(ent is Dimension)
					{
						var dim=ent as Dimension;
						
						if(dim.DimensionText==null | dim.DimensionText=="")
						{
							//Когда в размер не вставлен текст, текстовая строка размера пустая
							
							dim.DimensionText=@"{\L<>}";
							
							//чтобы не продолжать ненужную работу дальше
							continue;
						}
						/* Используем механизм регулярных выражений для подстановки в строку управляющего кода подчеркивания
						 * Шаблон ищет последнее число в строке или знак измеренного размера <> (для случая 5х200=1000 подчеркивается только 1000)
						 * или если размер не перебит, в строке 5х200=<> подчеркивается <>, в редакторе это видно как подчеркнутое 1000
						 * Шаблон подстановки обрамляет число в управляющие символы
						 */
						dim.DimensionText=Regex.Replace(dim.DimensionText,
						                                @"(<>)|(\d+)$",
						                                @"{\L$&}");
					}
				}
			}
		}
		
		/// <summary>
		/// Перебить размер на текущее измерение
		/// </summary>
		public void RewriteDim()
		{
			var sset=Input.Objects("Выберите размеры, которые необхоидмо подчеркнуть"); if(Input.StatusBad) return;
			
			using(var th=new TransactionHelper())
			{
				objects=th.EditObjects(sset);
				
				foreach(Entity ent in objects)
				{
					if(ent is Dimension)
					{
						var dim=ent as Dimension;
						
						dim.DimensionText=string.Format("{0:F0}",dim.Measurement);
					}
				}
			}
		}
		
		/// <summary>
		/// Разделить размер на 2 на той же линии с общей выбранной точкой
		/// </summary>
		public void SplitDim()
		{
			var sset=Input.Objects("Выберите размеры, которые необхоидмо разделить"); if(Input.StatusBad) return;
			
			using(var th=new TransactionHelper())
			{
				objects=th.EditObjects(sset);
				
				foreach(Entity ent in objects)
				{
					if(ent is RotatedDimension)
					{
						var pt=Input.Point("Введите промежуточную точку"); if(Input.StatusCancel) return; if(Input.StatusBad) continue;
						
						var dim=ent as RotatedDimension;
						
						var dim_new=dim.Clone() as RotatedDimension;
						
						dim_new.XLine2Point=pt;
						dim_new.XLine1Point=dim.XLine1Point;
						dim.XLine1Point=pt;
						
						dim_new.DimLinePoint=dim.DimLinePoint;
						
						th.WriteObject(dim_new);
					}
				}
			}
		}
		
		/// <summary>
		/// Объединить размеры
		/// </summary>
		public void MergeDim()
		{
			var sset=Input.Objects("Выберите размеры, которые необхоидмо разделить"); if(Input.StatusBad) return;
			
			using(var th=new TransactionHelper())
			{
				objects=th.EditObjects(sset);
				
				var dims=from o in objects
					where o is RotatedDimension
					select (RotatedDimension)o;
				
				var basedim=dims.First();
				var basevector=Vector3d.YAxis.RotateBy(basedim.Rotation,Vector3d.ZAxis);
				var baseplane=new Plane(basedim.DimLinePoint,basevector);
				
				var points1=dims.Select(d=>d.XLine1Point);
				var points2=dims.Select(d=>d.XLine2Point);
				
				var points=points1.Concat(points2).ToList();
				
				//сортируем точки по проекции на размерную линию
				points.Sort((x,y)=>x.OrthoProject(baseplane).GetAsVector().GetAngleTo(basevector).CompareTo(
					y.OrthoProject(baseplane).GetAsVector().GetAngleTo(basevector)));
				
				basedim.XLine1Point=points.First();
				basedim.XLine2Point=points.Last();
				
				foreach(var d in dims.Skip(1))
				{
					d.Erase(true);
				}
			}
			
		}
		
		static double text_width=0.7;
		
		/// <summary>
		/// Сузить размерный текст
		/// </summary>
		public void DimTextWidth()
		{
			//вводим объекты. По ключевому слову "Ширина" вводим значение коэффициента
			Tweet(string.Format("Текущее значение установлено {0:F}",text_width));
			var sset=Input.Objects("Выберите размеры, для которых необходимо изменить ширину текста ", new string[]
			                       {"WIdth","Ширина"},
			                       (s,e)=>
			                       {
			                       	if(e.Input=="WIdth")
			                       	{
			                       		double tw=Input.Double("Введите значение коэффициента сжатия-растяжения (по умолчанию 0.7)");
			                       		if(Input.StatusBad) return;
			                       		text_width=tw;
			                       		Tweet(string.Format("Текущее значение установлено {0:F}",text_width));
			                       	}
			                       }
			                      ); if(Input.StatusBad) return;
			
			using(var th=new TransactionHelper())
			{
				objects=th.EditObjects(sset);
				
				foreach(var Dim in objects.OfType<Dimension>())
				{
					string OldText=Dim.DimensionText;
					if(Regex.Match(OldText,@"\\W\d+\.\d+").Success)
					{
						Dim.DimensionText=Regex.Replace(OldText,@"\\W\d+\.\d+",string.Format(@"\W{0:F}",text_width));
					}
					else
					{
						//все размеры без форматирования с пустым текстом! поэтому надо добавить спецсимволы
						if(OldText=="") OldText="<>";
						
						Dim.DimensionText="{"+string.Format(@"\W{0:F}",text_width)+";"+OldText+"}";
					}
				}
			}
		}
		
		#region DimEvents
		
		List<ObjectId> DimsToEdit;
		
		public void EnableDimEvents()
		{
			var Docs=App.DocumentManager;
			foreach (Document doc in Docs)
			{
				var db=doc.Database;
				
				acCurDb.ObjectModified+=AddToEditList;
				acDoc.CommandEnded+=EditDimsAfterEditorEvent;
				
			}
		}
		public void AddToEditList(object sender, ObjectEventArgs e)
		{
			if(DimsToEdit==null) DimsToEdit=new List<ObjectId>();
			if(e.DBObject is Dimension)
			{
				if(!DimsToEdit.Contains(e.DBObject.ObjectId))
					DimsToEdit.Add(e.DBObject.ObjectId);
			}
		}
		public void EditDimsAfterEditorEvent(object s,EventArgs e)
		{
			foreach(var d in DimsToEdit)
			{
				Console.WriteLine(d);
			}
			
			if(DimsToEdit.Count==0) return;
			Console.WriteLine("Начали редактировать размеры");
			
			using(var th=new TransactionHelper())
			{
				Console.WriteLine("Вошли в транзакцию");
				
				try
				{
					var opened_objects=th.EditObjects(DimsToEdit);
					
					Console.WriteLine("Открыли объекты для записи");
					foreach( var dim in opened_objects.OfType<Dimension>())
					{
						Console.WriteLine(dim.ObjectId);
						EditDimFromEvent(dim);
					}
					
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex);
				}
				
				
			}
			DimsToEdit.Clear();
		}
		
		void EditDimFromEvent(Dimension dim)
		{
			if(Regex.Match(dim.DimensionText,@"\d+[xх]\d+=<>").Success)
			{
				var regex=new Regex(@"\d+[xх]\d+");
				var count_step=regex.Match(dim.DimensionText).Value;
				
				double step=double.Parse(Regex.Match(count_step,@"\d+").NextMatch().Value);
				double count=Math.Round(dim.Measurement/step);
				dim.DimensionText=Regex.Replace(dim.DimensionText,@"\d+[xх]",string.Format("{0:F0}х",count));
			}
			
		}
		
		public void DisableDimEvents()
		{
			var Docs=App.DocumentManager;
			foreach (Document doc in Docs)
			{
				var db=doc.Database;
				acCurDb.ObjectModified-=AddToEditList;
				acDoc.CommandEnded-=EditDimsAfterEditorEvent;
				
			}
		}
		#endregion
	}
}
