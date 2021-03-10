/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 19.07.2019
 * Время: 11:17
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace PKUserTools.Measurings
{
	/// <summary>
	/// Description of UnitsForm.
	/// </summary>
	public partial class UnitsForm : Form
	{
		static List<Units> units;
		public static Units modelUnits;
		public static Units measuringUnits;
		
		
		
		static UnitsForm()
		{
			var meters = new Units(1,"Метры");
			var millimeters = new Units(1E-3,"Миллиметры");
			
			modelUnits=millimeters;
			measuringUnits=meters;
			
			units=new List<Units>() {
				meters,
				millimeters,
				new Units(0.0254, "Дюймы"),
				new Units(0.01, "Сантиметры"),
				
					};
			
			
		}
		
		public UnitsForm()
		{
			InitializeComponent();
			
			foreach(var u in units)
			{

				comboBox1.Items.Add(u.Name);
				
				comboBox2.Items.Add(u.Name);
				
			}
			
			int i=0;
			foreach(var u in units)
			{
				if(modelUnits.Name==u.Name) comboBox1.SelectedIndex=i;
				if(measuringUnits.Name==u.Name) comboBox2.SelectedIndex=i;
				i++;
			}
			
		}
		void UnitsFormFormClosing(object sender, FormClosingEventArgs e)
		{
			foreach(var u in units)
			{
				if(comboBox1.Text.Equals(u.Name)) modelUnits=u;
				if(comboBox2.Text.Equals(u.Name)) measuringUnits=u;
			}
			Units.CurrentQuotient=measuringUnits.Value(modelUnits,1);
		}
		
	}
}
