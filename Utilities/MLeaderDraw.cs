/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 22.05.2019
 * Время: 14:27
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

using UT = PKUserTools.Utilities.Utilities;
using PKUserTools.EditorInput;
using PKUserTools.Utilities;


namespace PKUserTools.Utilities
{
	/// <summary>
	/// Description of MLeaderDraw.
	/// </summary>
	public class MLeaderDraw
	{
		AnnotationScale asc;
		MText mt;
		
		Document acDoc;
		Database acCurDb;
		Editor acEd;
		MLeader mLeader;
		
		public MLeaderDraw(string content, List<Point3d> points, Point3d basePt)
		{
			Init();
			PrepareMtext(basePt,content);
			
			Vector3d offset=new Vector3d(2,3.6,0);
			MLeader mleader = new MLeader();
			mleader.SetDatabaseDefaults();
			mleader.ContentType = ContentType.MTextContent;
			
			mt.TransformBy(Matrix3d.Displacement(offset.DivideBy(asc.Scale)));
			
			mleader.MText = mt;
			
			foreach(Point3d ptt in points)
			{
				int idx = mleader.AddLeaderLine(basePt);
				mleader.SetFirstVertex(idx, ptt);
			}
		}
		void PrepareMtext(Point3d mtp, string text)
		{
			mt = new MText();
			mt.SetDatabaseDefaults();
			mt.Contents = text;

			//междустрочный интервал
			mt.LineSpacingStyle = LineSpacingStyle.Exactly;
			mt.LineSpacingFactor = 1.0d;

			mt.Location = mtp;
			mt.TextStyleId = acCurDb.Textstyle;
			mt.TextHeight = 3 / asc.Scale;
			mt.Attachment = AttachmentPoint.BottomCenter;
			mt.Annotative = AnnotativeStates.True;
			mt.Width = 5 / asc.Scale;
			mt.Height = 5 / asc.Scale;

			
		}
		void Init()
		{
			acDoc = App.DocumentManager.MdiActiveDocument;
			acCurDb = acDoc.Database;
			acEd = acDoc.Editor;
			mLeader=new MLeader();
			asc = UT.GetAnnoScale("CANNOSCALE");
		}
	}
}
