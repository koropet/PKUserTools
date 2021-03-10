using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.ExportTable;
using PKUserTools.EditorInput;

using System.Windows.Forms;
namespace PKUserTools.Commands
{
	class LineSum:FunctionClass
	{

		List<Entity> objects = new List<Entity>();
		static string delimiter= "\t"; //разделитель в списке длин
		

		public override void Execute()
		{
			base.Execute();

			Tweet(string.Format("Сумма длин объектов равна {0:0} мм", GetLengthSum()));
		}
		public double GetLengthSum()
		{
			double sum = 0;
			var sset=Input.Objects("Выберите объекты для подсчета");
			if (Input.StatusBad) return 0;

			using (TransactionHelper th = new TransactionHelper())
			{
				objects = th.ReadObjects(sset);
				foreach (Entity ent in objects)
				{
					sum += UT.EntityLength(ent);
				}
			}
			return sum;
		}
		public void LengthList()
		{
			base.Execute();
			
			if(delimiter=="\n")
			{
				Tweet("Вертикальная группировка");
			}
			else if(delimiter=="\t")
			{
				Tweet("Горизонтальная группировка");
			}
			
			var list=new List<string>();
			var sset=Input.Objects("Выберите объекты",new [] {"Mode", "РЕжим"}, (s,e)=>
			                       {
			                       	if(delimiter=="\t")
			                       	{
			                       		delimiter="\n";
			                       		Tweet("Вертикальная группировка");
			                       	}
			                       	else if(delimiter=="\n")
			                       	{
			                       		delimiter="\t";
			                       		Tweet("Горизонтальная группировка");
			                       	}
			                       }
			                      ); if(Input.StatusBad) return;
			
			using(var th=new TransactionHelper())
			{
				objects=th.ReadObjects(sset);
				
				foreach(var o in objects)
				{
					list.Add(String.Format("{0:F0}",UT.EntityLength(o)));
				}
			}
			
			Clipboard.SetText(list.Aggregate((s1,s2)=>s1+delimiter+s2));
		}
	}
}
