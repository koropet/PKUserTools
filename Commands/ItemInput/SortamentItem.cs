/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 27.09.2018
 * Время: 12:10
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;

namespace PKUserTools.Commands.ItemInput
{
	/// <summary>
	/// Общий класс для позиций проката
	/// </summary>
	public class SortamentItem
	{
		public Shape shape;
		public Sortament sortament;
		public string mark;
		public string Name;
		public int Count;
		
		double metermass;
		
		public SortamentItem()
		{
		}
		public string MassString
		{
			get
			{
				return string.Format("{0:F2}",Mass).Replace('.',',');
			}
		}
		public double Mass
		{
			get
			{
				return Math.Round(metermass*(shape.Length*0.1))*0.01; //rount to 2nd digit and overall divide by 1000
			}
		}
		public double AllMass
		{
			get
			{
				return Mass*Count;
			}
		}
		public void Calculate()
		{
			metermass=sortament.MassFromMark(mark);
		}
	}
}
