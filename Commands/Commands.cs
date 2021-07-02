using System;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;

using CSScriptLibrary;

namespace PKUserTools.Commands
{

	public class Commands
	{
		Forms.Form1 form;
		
		/// <summary>
		/// Показать пользовательскую форму
		/// </summary>
		[CommandMethod("PKUserToolsForm")]
		public void PKUserToolsForm()
		{
			
			form=new Forms.Form1();
			form.Text="PKUserTools";
			form.Show();
			
		}
		/// <summary>
		/// Создание обозначения проема из двух отрезков
		/// </summary>
		[CommandMethod("PKHoleSign", CommandFlags.UsePickSet)]
		public void PkHoleSign()
		{
			HoleSign HS = new HoleSign();
			HS.Execute();
		}


		[CommandMethod("PKDimChain", CommandFlags.UsePickSet)]
		public void DimChain()
		{
			DimChain DC = new DimChain();
			DC.Execute();
		}

		[CommandMethod("PKMEditText", CommandFlags.UsePickSet)]
		public void MultiEditText()
		{
			MultiEditText ME = new MultiEditText();
			ME.Execute();
		}

		[CommandMethod("PKMLeader", CommandFlags.UsePickSet)]
		public void MultiLeader()
		{
			MultiLeader ML = new MultiLeader();
			ML.Execute();
		}
		[CommandMethod("PKMLeaderInline", CommandFlags.UsePickSet)]
		public void MultiLeaderInline()
		{
			MultiLeader ML = new MultiLeader();
			ML.MakeArrowsInline();
		}
		ExportTable ET;
		[CommandMethod("PKExportTable", CommandFlags.UsePickSet)]
		public void ExportTable()
		{
			ET = new ExportTable();
			ET.Execute();
		}

		[CommandMethod("PKLineSum", CommandFlags.UsePickSet)]
		public void LineSum()
		{
			LineSum ls = new LineSum();
			ls.Execute();
		}
		[CommandMethod("PKPatternCopy", CommandFlags.UsePickSet)]
		public void PatternCopy()
		{
			PatternCopy PC = new PatternCopy();
			PC.Execute();
		}
		[CommandMethod("PKItemInput", CommandFlags.UsePickSet)]
		public void ItemInput()
		{
			var II = new PKUserTools.Commands.ItemInput.ItemInput();
			II.Execute();
		}
		[CommandMethod("PKTranslateTable", CommandFlags.UsePickSet)]
		public void TranslateTable()
		{
			var ET_TT=new ExportTable();
			ET_TT.ExecuteTranslator();
		}
		#region DimEdit
		DimEdit de;
		[CommandMethod("PKDimUnderline",CommandFlags.UsePickSet)]
		public void DimUnderline()
		{
			if(de==null) de=new DimEdit();
			de.UnderlineDim();
		}
		[CommandMethod("PKDimRewrite",CommandFlags.UsePickSet)]
		public void DimRewrite()
		{
			if(de==null) de=new DimEdit();
			de.RewriteDim();
		}
		
		[CommandMethod("PKDimSplit",CommandFlags.UsePickSet)]
		public void DimSplit()
		{
			if(de==null) de=new DimEdit();
			de.SplitDim();
		}
		
		[CommandMethod("PKDimMerge",CommandFlags.UsePickSet)]
		public void DimMerge()
		{
			if(de==null) de=new DimEdit();
			de.MergeDim();
		}
		
		[CommandMethod("PKDimTextWidth",CommandFlags.UsePickSet)]
		public void DimTextWidth()
		{
			if(de==null) de=new DimEdit();
			de.DimTextWidth();
		}
		
		
		[CommandMethod("PKEnableDimEvents")]
		public void EnableDimEvents()
		{
			if(de==null) de=new DimEdit();
			de.EnableDimEvents();
		}
		[CommandMethod("PKDisableDimEvents")]
		public void DisableDimEvents()
		{
			if(de==null) de=new DimEdit();
			de.DisableDimEvents();
		}
		
		#endregion
		
		[CommandMethod("PKBreakline",CommandFlags.UsePickSet)]
		public void PKBreakline()
		{
			//флаг UsePickSet добавлен для поддержки заранее выделенных отрезков и полилиний для вставки символов
			var bl=new BreakLine();
			bl.MakeBreakLine();
		}
		[CommandMethod("PKDrawTails",CommandFlags.UsePickSet)]
		public void DrawTailsmethod()
		{
			var dt=new DrawTails();
		}
		
		[CommandMethod("PKBendLine",CommandFlags.UsePickSet)]
		public void BendLine()
		{
			var bl = new BendLine();
			bl.Execute();
		}
		
		[CommandMethod("PKLengthList",CommandFlags.UsePickSet)]
		public void LengthList()
		{
			var ls=new LineSum();
			ls.LengthList();
		}
		[CommandMethod("PKCoordsList",CommandFlags.UsePickSet)]
		public void CoordsList()
		{
			var ls=new LineSum();
			ls.CoordsList();
		}
		[CommandMethod("PKDrawRebar")]
		public void DrawRebar()
		{
			var dr=new RebarDrawing();
		}
		
		[CommandMethod("PKDrawRod")]
		public void DrawRod()
		{
			DrawRods.DrawRodTwoPoints();
		}

		[CommandMethod("PKEditAttributes")]
		public void EditAttributes()
		{
			Utilities.AttributeEditor.EditAttributes();
		}

		[CommandMethod("PKEditAttributesSettings")]
		public void EditAttributesSettings()
		{
			Utilities.AttributeEditor.AttributeEditorSettingsSet();
		}
		
		[CommandMethod("PKListAttributes")]
		public void ListAttributes()
		{
			Utilities.AttributeEditor.ListAttributes();
		}
		[CommandMethod("PKVorLayouts")]
		public void VorLayouts()
		{
			Utilities.LayoutTools.VORLayouts();
		}
		[CommandMethod("PKParseSteps",CommandFlags.UsePickSet)]
		public void ParseSteps()
		{
			StepsParser.TestCommand();
		}
		[CommandMethod("PKChangeDrawingProperty",CommandFlags.UsePickSet)]
		public void ChangeDrawingProperty()
		{
			var cdp = new Utilities.ChangeDrawingProperty();
		}
		[CommandMethod("PKClineTransform",CommandFlags.UsePickSet)]
		public void ClineTransform()
		{
			TransForm.InCline();
		}
		[CommandMethod("PKRebarToVor",CommandFlags.UsePickSet)]
		public void RebarToVor()
		{
			Utilities.ReadToVor.MakeRebarString();
		}
		
		/// <summary>
		/// Использую для отладки разных частей, пока не оформлена команда
		/// </summary>
		[CommandMethod("PKSample",CommandFlags.UsePickSet)]
		public void Sample()
		{
			//PKUserTools.Utilities.PropertyWorker.PropertyWorker.AreaLeader();
			//Junction.CommandResult();
			//Utilities.PKReflection.ShowFields();
			
		}
		
		[CommandMethod("PKTableImport")]
		public void ImportTable()
		{
			TableImport.Sample();
		}
		[CommandMethod("PKTableFromClipboard")]
		public void TableFromClipBoard()
		{
			TableFromClipboard.Make();
		}
		#region XDatatools
		XdataTools xt;
		[CommandMethod("PKLink")]
		public void Link()
		{
			if(xt==null) xt=new XdataTools();
			xt.CreateLinkCommand();
		}
		
		[CommandMethod("PKAddPos")]
		public void AddPos()
		{
			if(xt==null) xt=new XdataTools();
			xt.AttachPosition();
		}
		[CommandMethod("PKReadPos")]
		public void ReadPos()
		{
			if(xt==null) xt=new XdataTools();
			xt.ReadPosition();
		}
		#endregion
		#region Units
		[CommandMethod("PKUnitsSettings")]
		public void UnitsSettings()
		{
			var uf = new Measurings.UnitsForm();
			uf.ShowDialog();
		}
		#endregion
	}
}
