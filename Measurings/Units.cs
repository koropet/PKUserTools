/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 19.07.2019
 * Время: 11:01
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;

namespace PKUserTools.Measurings
{
	/// <summary>
	/// Description of Units.
	/// </summary>
	public class Units
	{
		public static double CurrentQuotient=1;
		
		
		double quotient; //коэффициент привдения к единицам СИ
		public string Name;
		public Units(double quotient, string Name)
		{
			this.Name=Name;
			this.quotient=quotient;
		}
		/// <summary>
		/// Перевод единиц
		/// </summary>
		/// <param name="units">исходные единицы</param>
		/// <param name="value">исходное значение</param>
		/// <returns></returns>
		public double Value(Units units, double value, int power=1)
		{
			//TODO здесь возможна оптимизация с помощью кэширования коэффициентов перевода заранее известных единиц измерения
			//для разового использования данный код подходит. Для производительности можно будет сделать.
			double power_quotient = Math.Pow(units.quotient/quotient,power);
			
			return value*power_quotient;
		}
		
	}
}
