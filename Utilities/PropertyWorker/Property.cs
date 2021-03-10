/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 20.05.2019
 * Время: 15:31
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;

namespace PKUserTools.Utilities.PropertyWorker
{
	/// <summary>
	/// Description of Property.
	/// </summary>
	public class Property
	{
		public string Name;
		public string propType;
		public object pValue;
		
		public Property()
		{
		}
		public override string ToString()
		{
			return string.Format("[Property Name={0}, PropType={1}, PValue={2}]", Name, propType, pValue);
		}

	}
	public class LayerProperty:Property
	{
		public LayerProperty(string Layer)
		{
			pValue=Layer;
			Name= "LAYER";
			propType="STRING";
		}
	}
	public class AreaProperty:Property
	{
		public AreaProperty(double pValue)
		{
			this.pValue=pValue;
			Name="AREA";
			propType="DOUBLE";
		}
		public double Area
		{
			get
			{
				return (double)pValue;
			}
		}
	}
	public class VolumeProperty:Property
	{
		public VolumeProperty(double pValue)
		{
			this.pValue=pValue;
			Name = "VOLUME";
			propType = "DOUBLE";
		}
		public double Volume
		{
			get
			{
				return (double)pValue;
			}
		}
	}
}
