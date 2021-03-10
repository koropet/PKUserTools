/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 20.05.2019
 * Время: 15:27
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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

namespace PKUserTools.Utilities.PropertyWorker
{
	/// <summary>
	/// оболочка над объектом Entyty для обобщенного доступа к его свойствам.
	/// Реализует чтение/запись свойств и возвращает исключения, если свойство не найдено или не соответствует типу
	/// Класс не абстрактный, так как он сам реализует доступ к обобщенному объекту Entyty. Наследуемые от него классы будут расширять его доступ на классы,
	/// наследуемые от класса Entity
	/// </summary>
	public class PropertyWrapper
	{
		protected Entity source;
		protected Dictionary<string, PropertyGetSet> propList;
		
		static Dictionary<string, PropertyGetSet> staticPropList =new Dictionary<string, PropertyGetSet>()
			
		{
			{"LAYER", new PropertyGetSet()
				{
					prop=new LayerProperty(""),
					setAction=(e,p)=>e.Layer=p.pValue.ToString(),
					getAction=(e)=>new LayerProperty(e.Layer),
				}
			}
		};
		
		
		public PropertyWrapper(Entity ent)
		{
			source=ent;
			propList=staticPropList;
		}
		
		public void SetProperty(Property pValue)
		{
			if(!propList.ContainsKey(pValue.Name)) throw new KeyNotFoundException("Свойство не найдень для данного типа объекта");
			if(propList[pValue.Name].prop.propType!=pValue.propType) throw new InvalidCastException("Тип свойства неверен");
			propList[pValue.Name].setAction(source,pValue);
			
		}
		public Property GetProperty(string pName)
		{
			if(!propList.ContainsKey(pName)) throw new KeyNotFoundException("Свойство не найдень для данного типа объекта");
			return propList[pName].getAction(source);
		}
	}
	public class PolylinePropertyWrapper:PropertyWrapper
	{
		static Dictionary<string, PropertyGetSet> staticPropList=new Dictionary<string, PropertyGetSet>()
			
		{
			{
				"AREA", new PropertyGetSet()
					
				{
					prop=new AreaProperty(0),
					setAction= (e, p) => Messaging.Tweet("Свойство только для чтения"),
					getAction=(e)=>new AreaProperty(((Polyline)e).Area),
				}
			},
			{
				"LAYER", new PropertyGetSet()
				{
					prop=new LayerProperty(""),
					setAction=(e,p)=>e.Layer=p.pValue.ToString(),
					getAction=(e)=>new LayerProperty(e.Layer),
				}
			}
		};
		
		public PolylinePropertyWrapper(Polyline ent):base(ent)
		{
			propList=staticPropList;
		}
	}
	public class BodyPropertyWrapper:PropertyWrapper
	{
		static Dictionary<string, PropertyGetSet> staticPropList=new Dictionary<string, PropertyGetSet>()
			
		{
			/*{
				"VOLUME", new PropertyGetSet()
					
				{
					prop=new AreaProperty(0),
					setAction= (e, p) => Messaging.Tweet("Свойство только для чтения"),
					getAction=(e)=>new VolumeProperty(((Autodesk.AutoCAD.DatabaseServices.Body)e)),
				}
			},*/
			{
				"LAYER", new PropertyGetSet()
				{
					prop=new LayerProperty(""),
					setAction=(e,p)=>e.Layer=p.pValue.ToString(),
					getAction=(e)=>new LayerProperty(e.Layer),
				}
			}
		};
		public BodyPropertyWrapper(Polyline ent):base(ent)
		{
			propList=staticPropList;
		}
	}
	public struct PropertyGetSet
	{
		public Property prop;
		public Action<Entity,Property> setAction;
		public Func<Entity,Property> getAction;
	}

}
