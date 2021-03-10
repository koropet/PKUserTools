/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 22.10.2018
 * Время: 9:50
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
	/// Простые функции рисования арматуры
	/// </summary>
	class DrawRods
	{
		const double round=10;
		const double round_5=5;
		/// <summary>
		/// текущий диаметр
		/// </summary>
		static int diameter=12;
		
		/// <summary>
		/// базовый коэффициент анкеровки
		/// </summary>
		static int base_anchorage=38;
		
		static double min_layer=30;
		
		static bool left_anchorage=true;
		static bool right_anchorage=true;
		
		static public void DrawRodTwoPoints()
		{
			var keywords=new string[] {
				"LEft","ЛЕво",
				"RIght","ПРаво",
				"Diameter", "Диаметр",
				"ANchorage", "АНкеровка"};
			
			Point3d p1=Input.Point("Выберите первую точку",keywords, SetSides); if(Input.StatusCancel) return;
			Point3d p2=Input.Point("Выберите вторую точку",keywords, SetSides); if(Input.StatusCancel) return;
			Point3d p3=Input.Point("Выберите сторону смещения"); if(Input.StatusCancel) return;
			
			double anchorage=Math.Round(base_anchorage*diameter/round)*round;
			double offset=Math.Round((min_layer+diameter/2)/round_5,MidpointRounding.AwayFromZero)*round_5;
			
			var vp1p2=UT.GetUniteVector(p1,p2);
			
			var pl=new Plane(p1,vp1p2.GetPerpendicularVector());
			Point3d p4=p3.OrthoProject(pl);
			
			var vp4p3=UT.GetUniteVector(p4,p3);
			
			var A=p1.Add(vp4p3.MultiplyBy(offset)).Add(vp1p2.MultiplyBy(left_anchorage?-anchorage:0));
			var B=p2.Add(vp4p3.MultiplyBy(offset)).Add(vp1p2.MultiplyBy(right_anchorage?anchorage:0));
			
			using(var th=new TransactionHelper())
			{
				var AB=new Line(A,B);
				AB.SetDatabaseDefaults();
				th.WriteObject(AB);
			}
		}
		
		static void SetSides(string e)
		{
			if(e=="LEft") { left_anchorage=!left_anchorage; Messaging.Tweet(left_anchorage ? "Анкеровка слева":"Без анкеровки слева");}
			else if(e=="RIght") { right_anchorage=!right_anchorage; Messaging.Tweet(left_anchorage ? "Анкеровка справа":"Без анкеровки справа");}
			else if(e=="Diameter")
			{
				int inp=Input.Integer("Введите диаметр стержня"); if(Input.StatusBad) return;
				diameter=inp;
			}
			else if(e=="ANchorage")
			{
				int inp=Input.Integer("Введите базовый коэффициент анкеровки"); if(Input.StatusBad) return;
				base_anchorage=inp;
			}
		}
	}
}
