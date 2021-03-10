/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 20.05.2019
 * Время: 15:27
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using App = Autodesk.AutoCAD.ApplicationServices.Application;
using PKUserTools.EditorInput;
using PKUserTools.Utilities;
using UT = PKUserTools.Utilities.Utilities;
namespace PKUserTools.Utilities.PropertyWorker
{
	public static class PropertyWorker
	{
		static Dictionary<Type, Func<Entity, PropertyWrapper>> wrappers = new Dictionary<Type, Func<Entity, PropertyWrapper>>() {
			{
				typeof(Entity),
				e => new PropertyWrapper(e)
			},
			{
				typeof(Polyline),
				p => new PolylinePropertyWrapper((Polyline)p)
			},
		};

		static public void SetProperties(List<Entity> entities, Property p)
		{
			foreach (var ent in entities) {
				PropertyWrapper wr;
				if (!wrappers.ContainsKey(ent.GetType()))
					wr = new PropertyWrapper(ent);
				//not supported kind of object. Make wrapper of entity
				else
					wr = wrappers[ent.GetType()](ent);
				try {
					wr.SetProperty(p);
				}
				catch (KeyNotFoundException) {
					Messaging.Tweet("Объект не имеет данного свойства");
				}
				catch (InvalidCastException) {
					Messaging.Tweet("Попытка присвоить свойству не тот тип");
				}
			}
		}

		static public Property GetProperty(Entity ent, string name)
		{
			PropertyWrapper wr;
			if (!wrappers.ContainsKey(ent.GetType()))
				wr = new PropertyWrapper(ent);
			//not supported kind of object. Make wrapper of entity
			else
				wr = wrappers[ent.GetType()](ent);
			try {
				return wr.GetProperty(name);
			}
			catch (KeyNotFoundException) {
				Messaging.Tweet("Объект не имеет данного свойства");
				return null;
			}
		}

		public static void TestCommand()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			var sset = Input.Objects("Выберите объекты");
			if (Input.StatusBad)
				return;
			using (var th = new TransactionHelper()) {
				var ents = th.EditObjects(sset);
				string name = "AREA";
				foreach (var e in ents) {
					var p = PropertyWorker.GetProperty(e, name);
					if (p == null) {
						Messaging.Tweet("Skip");
						continue;
					}
					Messaging.Tweet(p);
					Console.WriteLine(p);
					
				}
			}
		}
		
		/// <summary>
		/// Рисуем выноску со свойством AREA объектов
		/// </summary>
		public static void AreaLeader()
		{
			PropertyMLeader("AREA", 2);
		}
		public static void VolumeLeader()
		{
			PropertyMLeader("VOLUME", 3);
		}
		static void PropertyMLeader(string name, int pow)
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			var sset = Input.Objects("Выберите объект"); if (Input.StatusBad) return;
			
			asc = UT.GetAnnoScale("CANNOSCALE");
			Vector3d offset=new Vector3d(2,3.6,0);
			var areas = new List<double>();
			
			using (var th = new TransactionHelper())
			{
				var ents = th.EditObjects(sset);
				
				foreach (var e in ents)
				{
					var p = PropertyWorker.GetProperty(e, name);
					if (p == null) {
						Messaging.Tweet("Skip");
						continue;
					}
					areas.Add((double)p.pValue);
				}
			}
			foreach(var d in areas)
			{
				double measured_d = PKUserTools.Measurings.UnitsForm.measuringUnits.Value(PKUserTools.Measurings.UnitsForm.modelUnits,d,pow);
				
				using( var th = new TransactionHelper())
				{
					var pt1 = Input.Point("Первая точка выноски"); if (Input.StatusBad) return;
					var pt2 = Input.Point("Вторая точка выноски"); if (Input.StatusBad) return;
					
					//TODO сделать общий класс создания мультивыносок
					var	mleader = new MLeader();
					MText mt;
					PrepareMtext(0,out mt,acCurDb,pt2);
					
					mleader.SetDatabaseDefaults();
					mleader.ContentType = ContentType.MTextContent;
					
					mt.TransformBy(Matrix3d.Displacement(offset.DivideBy(asc.Scale)));
					
					mt.Contents = string.Format("{0:F3}",measured_d);
					
					mleader.MText = mt;
					
					int idx = mleader.AddLeaderLine(pt1);
					mleader.SetFirstVertex(idx, pt1);
					
					th.WriteObject(mleader);
				}
			}
		}
		
		static AnnotationScale asc;
		static void PrepareMtext(double angle, out MText mt, Database acCurDb, Point3d mtp)
		{
			mt = new MText();
			mt.SetDatabaseDefaults();
			

			//междустрочный интервал
			mt.LineSpacingStyle = LineSpacingStyle.Exactly;
			mt.LineSpacingFactor = 1.0d;

			mt.Location = mtp;
			mt.TextStyleId = acCurDb.Textstyle;
			mt.TextHeight = 3 / asc.Scale;
			mt.Attachment = AttachmentPoint.BottomCenter;
			mt.Annotative = AnnotativeStates.True;
			mt.Rotation = angle;
			mt.Width = 5 / asc.Scale;
			mt.Height = 5 / asc.Scale;

			
		}
		
	}

}


