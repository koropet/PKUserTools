/*
 * Сделано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 16.07.2018
 * Время: 15:28
 * 
 * Для изменения этого шаблона используйте Сервис | Настройка | Кодирование | Правка стандартных заголовков.
 */
using System;

namespace PKUserTools
{
	/// <summary>
	/// Реализует вывод данных в строки спецификации
	/// </summary>
	public interface ISpecification
	{
		string SPname();
		string SPposition();
		string SPcount();
		string SPmass();
	}
}
