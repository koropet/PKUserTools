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
		public void CoordsList()
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
			var sset=Input.Objects("Выберите полилинии и отрезки",new [] {"Mode", "РЕжим"}, (s,e)=>
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
					if(o is Polyline)
					{
						var pl_o = o as Polyline;
						for(int i=0; i<pl_o.NumberOfVertices;i++)
						{
							var pt = pl_o.GetPoint2dAt(i);
							list.Add(String.Format("{0:F3}\t{1:F3}",pt.X,pt.Y));
						}
					}
					if(o is Line)
					{
						var l_o = o as Line;
						var pt=l_o.StartPoint;
						list.Add(String.Format("{0:F3}/t{1:F3}",pt.X,pt.Y));
						pt=l_o.EndPoint;
						list.Add(String.Format("{0:F3}/t{1:F3}",pt.X,pt.Y));
					}
				}
			}
			
			Clipboard.SetText(list.Aggregate((s1,s2)=>s1+delimiter+s2));
		}
	}
}
