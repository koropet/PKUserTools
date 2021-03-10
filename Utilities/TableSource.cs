/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 17.05.2019
 * Время: 13:14
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Предоставляет доступ к таблице, представленной в виде файла с разделителями, как к двумерному массиву
	/// </summary>
	public class TableSource
	{
		protected string[,] data;
		int width,heigth;
		
		public TableSource(string FromCSV)
		{
			var rows = FromCSV.Split('\n');
			
			int columns= rows[0].Count(c=>c=='\t')+1;
			
			data=new string[rows.GetLength(0)-1,columns];
			
			try
			{
				for(int i=0;i<rows.GetLength(0)-1;i++)  //-1 значит не берем последний пустой элемент
				{
					var cells = rows[i].Replace("\r","").Split(new []{'\t'});
					//Messaging.Tweet("Строка номер " + i + ":" + rows[i].Replace("\t","{tab}").Replace("\n", "{new}"));
					for(int j=0;j<columns;j++)
					{
						data[i,j]=cells[j];
						//Messaging.Tweet("Ячейка: " + i + ":" + j + "значение: " + data[i,j]);
					}
				}
				width=columns;
				heigth=rows.GetLength(0)-1;
			}
			catch(IndexOutOfRangeException ex)
			{
				Messaging.Tweet("Проверьте файл. Не совпадает количество ячеек в строках");
				Console.WriteLine(ex);
			}
			catch(Exception ex)
			{
				Messaging.Tweet("Неизвестная ошибка");
				Console.WriteLine(ex);
			}
			//Messaging.Tweet("\n" + Make(data));
		}
		public TableSource(string[,] data)
		{
			this.data=data;
			this.heigth = data.GetLength(0);
			this.width=data.GetLength(1);
		}
		

		public int Width {
			get {
				return width;
			}
		}

		public int Heigth {
			get {
				return heigth;
			}
		}
		public string this[int r, int c]
		{
			get
			{
				return data[r,c];
			}
			set
			{
				data[r,c]=value;
			}
		}
		public static string Make(object[,] data)
		{
			int rows = data.GetLength(0);
			int columns = data.GetLength(1);
			string sum="";
			
			for(int i=0;i<rows;i++)
			{
				for(int j=0;j<columns-1;j++)
				{
					sum+=data[i,j]+"\t";
				}
				sum+=data[i,columns-1]+"\n";
			}
			sum.Remove(sum.Length-1,1);
			return sum;
		}
	}
	
	/// <summary>
	/// Предоставляет доступ к таблице по ключевому полю
	/// </summary>
	public class SingleKeyTableSource:TableSource
	{
		KeyIndexer ki;
		
		public SingleKeyTableSource(string FromCSV, int keyRow=0):base(FromCSV)
		{
			if(Width>Heigth) //если вдруг сделали таблицу 2х2, то получится вертикальный индексатор TODO: сделать возможность выбора
				ki=new HorKeyIndexer(this,keyRow);
			else
				ki=new VertKeyIndexer(this,keyRow);
			
			
		}
		public string this[string key, int row=1]
		{
			get
			{
				var t = ki.Index(key,row);
				return data[t.Item1,t.Item2];
			}
			set
			{
				var t = ki.Index(key,row);
				data[t.Item1,t.Item2]=value;
			}
		}
	}
	abstract class KeyIndexer
	{
		protected Dictionary<string,int> keyDict;
		public virtual Tuple<int,int> Index(string key, int number)
		{
			return null;
		}
		public int this[string key]
		{
			get
			{
				return keyDict[key];
			}
		}
	}
	class HorKeyIndexer:KeyIndexer
	{
		public HorKeyIndexer(TableSource t, int keyRow)
		{
			keyDict=new Dictionary<string,int>();
			for(int i=0;i<t.Width;i++)
			{
				keyDict.Add(t[keyRow, i],i);
			}
		}
		public override Tuple<int, int> Index(string key, int number)
		{
			return new Tuple<int, int>(number, keyDict[key]);
		}
	}
	class VertKeyIndexer:KeyIndexer
	{
		public VertKeyIndexer(TableSource t, int keyColumn)
		{
			keyDict=new Dictionary<string,int>();
			for(int i=0;i<t.Heigth;i++)
			{
				keyDict.Add(t[i,keyColumn],i);
			}
		}
		public override Tuple<int, int> Index(string key, int number)
		{
			return new Tuple<int, int>(keyDict[key],number);
		}
	}
	
	public class DoubleKeyTableSource:TableSource
	{
		KeyIndexer vk,hk;
		
		public DoubleKeyTableSource(string FromCSV, int keyRow=0, int keyColumn=0):base(FromCSV)
		{
			vk=new VertKeyIndexer(this,keyColumn);
			hk=new HorKeyIndexer(this,keyRow);
		}
		public string this[string key, string secondKey]
		{
			get
			{
				return data[hk[key],vk[secondKey]];
			}
			set
			{
				data[hk[key],vk[secondKey]]=value;
			}
		}
	}
}
