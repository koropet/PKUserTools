/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 28.08.2018
 * Время: 17:28
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Drawing;
using System.Windows.Forms;

using PKUserTools.Utilities;
using CSScriptLibrary;

namespace PKUserTools.Forms
{
	/// <summary>
	/// Тестовая форма для отладки взаимодействия приложения с формой
	/// </summary>
	public partial class Form1 : Form
	{
		public Form1()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		void Button1Click(object sender, EventArgs e)
		{
			/*
			string result="Таки ви что-то нажали!";
			
			string code = richTextBox1.Text; //обязательно нужно прописать функцию string Execute() чтобы код ниже что-то возвратил
			
			try
			{
				dynamic scr = CSScript.LoadCode(code).CreateObject("*"); 
				//внимательно! есть метод LoadMethod где не нужно прописывать класс. 
				//Здесь же в скрипте надо создать класс и в нем описать методы
				
				result= scr.Execute();
			}
			catch(Exception ex)
			{
				result = ex.ToString();
			}
			
			Messaging.Alert(result);*/
			
			string result="";
			
			string code_core = richTextBox1.Text;
			
			string code = @"
using System;
using PKUserTools.Commands;
using PKUserTools.Utilities;

public class My
{
     public string Execute()
         {
" 
				+ code_core 
				+ @"}}";
			//таким образом автоматизируем шаблон, в котором не нужно прописывать классы
			
			try
			{
				dynamic scr = CSScript.LoadCode(code, "PKUserTools.dll").CreateObject("*"); 
				
				result= scr.Execute();
			}
			catch(Exception ex)
			{
				result = ex.ToString();
			}
			
			Messaging.Alert(result);
			
		}
	}
}
