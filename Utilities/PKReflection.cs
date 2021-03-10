/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 12.09.2019
 * Время: 13:00
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
using Autodesk.Private.InfoCenter;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using PKUserTools.EditorInput;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;

using System.Reflection;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Description of PKReflection.
	/// </summary>
	public static class PKReflection
	{
		public static void ShowFields()
		{
			var Selection = Input.Objects("Выделите объекты для просмотра полей"); if(Input.StatusBad) return;
			
			using (var th = new TransactionHelper())
			{
				var AcadObjects = th.ReadObjects(Selection);
				foreach(var AcadObject in AcadObjects)
				{
					var AcadObjectType = AcadObject.GetType();
					Messaging.Tweet("Объект типа "+ AcadObjectType.Name);
					BaseTypeInfo(AcadObjectType, AcadObject);
				}
			}
		}
		static void BaseTypeInfo(Type children, object source)
		{
			var ChildrenType =children.GetTypeInfo();
			
			
			
			var TypeInfo = ChildrenType.GetTypeInfo();
			
			IEnumerable<PropertyInfo> pList = TypeInfo.DeclaredProperties;
			foreach(var p in pList)
			{
				Messaging.Tweet("Свойство: "+p.Name + " типа:" + p.PropertyType);
				try
				{
				Messaging.Tweet("Значение: "+ p.GetValue(source));
				}
				catch(Exception ex)
				{
					Messaging.Tweet(ex);
				}
			}
			
			IEnumerable<MethodInfo> mList = TypeInfo.DeclaredMethods;
			foreach(var m in mList)
			{
				Messaging.Tweet("Метод: "+m.Name + " типа:" + m.ReturnType);
			}
			
			var ParentType =ChildrenType.BaseType;
			Messaging.Tweet("Базовый тип: "+ ParentType.Name);
			if(ParentType==null) return;
			BaseTypeInfo(ParentType,source);
		}
	}
}
