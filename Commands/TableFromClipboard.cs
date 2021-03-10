/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 27.10.2020
 * Время: 9:20
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Diagnostics;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using XL = Microsoft.Office.Interop.Excel;

using PKUserTools.Utilities;
using PKUserTools.Commands.ItemInput;
using PKUserTools.EditorInput;
using System.Windows.Forms;

namespace PKUserTools.Commands
{
	/// <summary>
	/// Description of TableFromClipboard.
	/// </summary>
	public static class TableFromClipboard
	{
		static double minimumWidth = 10;
		static double heigth = 8;
		static double[] defaultWidth = {50};
		static double[] widthData = defaultWidth; 
		public static void Make()
		{
			
			string source = Clipboard.GetText();
			TableSource ts = new TableSource(source);
			if(ts.Heigth==0||ts.Width==0) return;
		
			var pt = Input.Point("Укажите левый верхний угол для вставки таблицы",
			                     new [] {"WIdth","Ширина"},
				         s => 
				         {
				         	if (s == "WIdth")
				         	{
				         		InputWidth();
				         		if(widthData.Length==0) widthData = defaultWidth;
				         	}
				         }
				        ); if(Input.StatusBad) return;
			                     
            
            Table tbl = new Table();
            tbl.SetSize(ts.Heigth, ts.Width);
            tbl.Position=pt;
            
            int widthDataCounter=0;
            for (int j = 0; j < tbl.Columns.Count; j++)
            {
            	tbl.Columns[j].Width=widthData[widthDataCounter];
            	widthDataCounter++; if(widthDataCounter==widthData.Length) widthDataCounter=0;
            }
            
            for (int i = 0; i < tbl.Rows.Count; i++)
            {
            	tbl.Rows[i].Height=heigth;
                for (int j = 0; j < tbl.Columns.Count; j++)
                {
                	tbl.Cells[i,j].Value=ts[i,j];
                }
            }
            
            
            using(var th = new TransactionHelper())
            {
            	th.WriteObject(tbl);
            }
                
            
		}
		static void InputWidth()
		{
			var list = new List<double>();
			while(true)
			{
				double inp = Input.Double("Введите следующее число"); if(Input.StatusBad) break;
				
				if(inp<minimumWidth) inp=minimumWidth;
				
				list.Add(inp);
				
			}
			widthData=list.ToArray();
		}
	}
}
