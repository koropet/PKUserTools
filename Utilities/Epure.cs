/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 21.09.2018
 * Время: 10:46
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Класс реализующий эпюры. Состоит из суммы функций от Х на определенных интервалах
	/// </summary>
	public class Epure
	{
		List<EpureSegment> segments;
		
		public Epure()
		{
			
		}
		
		/// <summary>
		/// Ордината по Х
		/// </summary>
		/// <param name="X">Абсцисса</param>
		/// <param name="left">Если нужно значение слева скачка, ставим true</param>
		/// <returns></returns>
		public double GetY(double X, bool left=false)
		{
			double sum=0;
			foreach(var seg in segments)
			{
				if(seg.XisOnThis(X,left)) sum+=seg.function(X);
			}
			return sum;
		}
	}
	class EpureSegment
	{
		double startX;
		double endX;
		double[] K;
		
		public Func<double,double> function;
		
		public EpureSegment(double X1,double X2)
		{
			if(X2>X1) {startX=X1;endX=X2;}
			else {startX=X1;endX=X2;}
			
			//дефолтная функция константа 0
			SetConstant(0);
		}
		
		public void SetConstant(double c)
		{
			K=new double[]{c};
			function=(x)=>K[0];
		}
		public void SetLinear(double y1, double y2)
		{
			K=new double[] {y1,(y2-y1)/(endX-startX)};
			
			//линейная функция вида y=kx+c
			function=(x)=>(K[1]*x+K[0]);
		}
		
		public bool XisOnThis(double X,bool left=false)
		{
			if(left) return X<=endX && X>startX;
			else return X<endX && X>=startX;
		}
	}
}
