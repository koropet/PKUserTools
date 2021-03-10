/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 17.10.2018
 * Время: 13:01
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using XL=Microsoft.Office.Interop.Excel;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;

using System.Windows.Forms;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of XdataTools.
	/// </summary>
	class XdataTools:FunctionClass
	{
		const string AppName = "PKUSERTOOLS";
		const string PosKey="POS";
		
		static int HandleCounter=1;
		
		static List<ObjectId> modified_id;
		
		public XdataTools()
		{
			AddRegApp(AppName);
			
			modified_id=new List<ObjectId>();
			
			acCurDb.ObjectOpenedForModify+=EventHandle;
			
			acCurDb.ObjectErased+=(s,e)=>
			{
				if(modified_id.Contains(e.DBObject.ObjectId))
				{
					modified_id.Remove(e.DBObject.ObjectId);
					Tweet("Объект был стерт "+ e.DBObject.ObjectId);
					Tweet("Свойство erased= "+e.Erased);
				}
				
			};
			
			acEd.EnteringQuiescentState += (s, e) => ShowPosFromList();
			
			acCurDb.ObjectModified+=(s, e) =>
			{
				var key=e.DBObject.ObjectId;
				if(links.ContainsKey(key)) { Tweet("Модифицировали объект " + key);ReactorAction(key,links[key]);}
			};
			
		}
		/// <summary>
		/// Записываем в XData пару ключ-значение для позиции. Внимание! При повторной записи старое значение теряется!
		/// </summary>
		public void AttachPosition()
		{
			var pos=Input.Text("Введите наименование позиции"); if(Input.StatusBad) return;
			
			var sset=Input.Objects("Выберите объекты для добавления XData"); if(Input.StatusBad) return;
			var rb=new ResultBuffer(new TypedValue(1001, AppName),
			                        new TypedValue(1000, PosKey),
			                        new TypedValue(1000, pos),
			                        new TypedValue((int)DxfCode.ExtendedDataInteger32, HandleCounter++)
			                       );
			using(var th=new TransactionHelper())
			{
				var objects=th.EditObjects(sset);
				foreach(var o in objects)
				{
					o.XData=rb;
				}
				rb.Dispose();
			}
			
		}
		
		/// <summary>
		/// Прочитать, если есть значение по ключу позиции
		/// </summary>
		public void ReadPosition()
		{
			var sset=Input.Objects("Выберите объекты для чтения XData"); if(Input.StatusBad) return;
			using(var th=new TransactionHelper())
			{
				var objects=th.EditObjects(sset);
				foreach(var o in objects)
				{
					var rbuf=o.GetXDataForApplication(AppName);
					if(rbuf==null) continue;
					var rb=rbuf.AsArray();
					for(int i=0;i<rb.GetLength(0);i++)
					{
						if(rb[i].Value.ToString()==PosKey)
						{
							Tweet("Позиция объекта "+ rb[++i].Value);
						}
					}
				}
			}
			
		}
		
		void EventHandle(object s, ObjectEventArgs e)
		{
			if(!modified_id.Contains(e.DBObject.ObjectId)) modified_id.Add(e.DBObject.ObjectId);
		}
		void ShowPosFromList()
		{
			if(modified_id.Count==0) return;
			
			foreach(var e in modified_id)
			{
				using(var th=new TransactionHelper())
				{
					var o=th.ReadObject(e);
					var rbuf=o.GetXDataForApplication(AppName);
					if(rbuf==null) return;
					var rb=rbuf.AsArray();
					for(int i=0;i<rb.GetLength(0);i++)
					{
						if(rb[i].Value.ToString()==PosKey)
						{
							Tweet("Позиция объекта "+ rb[++i].Value);
						}
						if(rb[i].TypeCode==(int)DxfCode.ExtendedDataInteger32) Tweet("Нашли интежер ручку номер "+(int)rb[i].Value);
					}
					
					
				}
			}
			Tweet("Список обработали, можно очистить");
			modified_id.Clear();
		}
		void AddRegApp(string RegAppName)
		{
			using (var tr=new TransactionHelper())
			{
				var rat = tr.ReadObject(acCurDb.RegAppTableId) as RegAppTable;
				rat.UpgradeOpen();
				if(!rat.Has(RegAppName))
				{
					var ratr=new RegAppTableRecord();
					ratr.Name=RegAppName;
					rat.Add(ratr);
					tr.AddNewlyCreatedDBObject(ratr,true);
				}
				
			}
		}
		
		Dictionary<ObjectId,ObjectIdCollection> links;
		
		void CreateLink(ObjectId one, ObjectIdCollection many)
		{
			if(links==null) links=new Dictionary<ObjectId, ObjectIdCollection>();
			
			if(!links.ContainsKey(one)) links.Add(one,many);
			
			
		}
		void ReactorAction(ObjectId one, ObjectIdCollection many)
		{
			var many_l=many.OfType<ObjectId>().ToList();
			
			using (var th=new TransactionHelper())
			{
				var one_o=th.ReadObject(one);
				var one_l = one_o as Line;
				if(one_l == null) return;
				
				var many_o=th.EditObjects(many_l).OfType<Line>().ToList();
				
				var offset=new Vector3d(100,100,0);
				
				int i=1;
				foreach(var l in many_o)
				{
					l.StartPoint=one_l.StartPoint.Add(offset.MultiplyBy(i));
					l.EndPoint=one_l.EndPoint.Add(offset.MultiplyBy(i));
					i++;
				}
			}
		}
		public void CreateLinkCommand()
		{
			var sset=Input.Objects("Выберите объекты для связи. Первый объект мастер.");
			
			var first=sset[0];
			var other=sset.GetObjectIds().Skip(1).ToArray();
			CreateLink(first.ObjectId, new ObjectIdCollection(other));
		}
	}
}
