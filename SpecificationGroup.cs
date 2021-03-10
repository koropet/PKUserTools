/*
 * Сделано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 16.07.2018
 * Время: 15:45
 * 
 * Для изменения этого шаблона используйте Сервис | Настройка | Кодирование | Правка стандартных заголовков.
 */
using System;
using System.Collections.Generic;

using PKUserTools.Commands;
using PKUserTools.Commands.ItemInput;

using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools
{
	/// <summary>
	/// группы позиций в спецификации - "детали", "материалы"
	/// </summary>
	public class SpecificationGroup:ISpecification
	{
		static string t="\t";
		public List<SpecificationGroup> childs;
		string head;
		public SpecificationGroup(string head)
		{
			this.head=head;
			childs=new List<SpecificationGroup>();
		}
		public SpecificationGroup()
		{
			
		}
		
		
		public string SPname()
		{
			return head;
		}
		
		public string SPposition()
		{
			return "";
		}
		
		public string SPcount()
		{
			return "";
		}
		
		public string SPmass()
		{
			return "";
		}
		
		public virtual string Row()
		{
			return SPposition()+t+SPname()+t+SPcount()+t+SPmass()+"\r"+childrow();
			
		}
		string childrow()
		{
			string result="";
			foreach (SpecificationGroup spg in childs)
			{
				result+=spg.Row();
			}
			return result;
		}
	}
	public class ArmaturGroup:SpecificationGroup
	{
		
		const string t="\t";
		public string gost;
		public byte diam;
		List<ArmaturItem> Rods;

        public List<ArmaturItem> rods
		{
			set
			{
				Rods=value;
				diam=value[0].diameter;
				gost=value[0].GOST;
				
			}
			get
			{
				return Rods;
			}
			
		}
		
		public ArmaturGroup()
		{
			
		}
		public new string SPname()
		{
			return string.Format("∅{0:0}",diam)+gost;
		}
		public override string Row()
		{
			string result=SPposition()+t+SPname()+t+SPcount()+t+SPmass()+"\r";
            foreach (ArmaturItem spg in rods)
			{
				result+=spg.SPposition()+t+spg.SPname()+t+spg.SPcount()+t+spg.SPmass()+"\r";
			}
			return result;
		}
        public bool IsOwnItem(ArmaturItem tocheck)
		{
			if(tocheck.GOST==gost&&tocheck.diameter==diam) return true;
			else return false;
		}
        public void AddItem(ArmaturItem it)
		{
            if (Rods == null) Rods = new List<ArmaturItem>();
			Rods.Add(it);
		}
        public double Sum
        {
        	get
        	{
        		double sum=0;
        		foreach(ArmaturItem ai in Rods)
        		{
        			sum+=ai.ALLMass;
        		}
        		return sum;
        	}
        }
			
	}
}
