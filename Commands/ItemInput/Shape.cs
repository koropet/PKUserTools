/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 27.09.2018
 * Время: 12:07
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using PKUserTools.Utilities;

namespace PKUserTools.Commands.ItemInput
{
	/// <summary>
	/// Описвает форму гнутого сортамента
	/// </summary>
	public class Shape
	{
		ShapeType stype;

		public ShapeType Stype {
			get {
				return stype;
			}
		}

		double length;
		
		public Shape()
		{
			
		}
		
		public double Length
		{
			get
			{
				return length;
			}
		}
		
		public static Shape FromString(string spec_name)
		{
			Shape s=new Shape();
			if(Regex.Match(spec_name,@"(L=)").Success) 
			{
				s.length=double.Parse(Regex.Match(spec_name.Replace(',','.'),@"(\d+\.\d+|\d+)").Value);
				s.stype=ShapeType.Constant;
				return s;
			}
			else if(Regex.Match(spec_name,@"(Lcp=|Lср=)").Success)
			{
				s.length=double.Parse(Regex.Match(spec_name.Replace(',','.'),@"(\d+\.\d+|\d+)").Value);
				s.stype=ShapeType.Variable;
				return s;
			}
			else if(Regex.Match(spec_name,@"(Lобщ=)").Success)
			{
				s.length=double.Parse(Regex.Match(spec_name.Replace(',','.'),@"(\d+\.\d+|\d+)").Value)*1000;
				s.stype=ShapeType.All;
				return s;
			}
			return s;
		}
	}
	public enum ShapeType
	{
		Constant,
		Variable,
		All
	}
}
