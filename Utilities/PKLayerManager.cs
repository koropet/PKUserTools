/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 06.05.2019
 * Время: 11:28
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Linq;


using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Управляет слоями добавляемых объектов
	/// Режим использования слоя "по команде" используется только с применением управляемо закрываемого экземпляра с ключевым словом using
	/// чтобы не перекрывать другие команды
	/// </summary>
	public class PKLayerManager:IDisposable
	{
		static string commandLayer="";
		static string CurrentLayer="";
		public static LayerManagerOption Mode;
		static Dictionary<LayerManagerOption,Action<Entity>> actions = new Dictionary<LayerManagerOption,Action<Entity>>()
		{
			{LayerManagerOption.ByCommand,SetLayerByCommand},
			{LayerManagerOption.ByCurentLayer,SetLayerByCurrent},
			{LayerManagerOption.ByObject,(e)=>{}} //do nothing when we not changing layer
			
		};
		public static void SetLayer(Entity ent)
		{
			actions[Mode](ent);
		}
		
		static void SetLayerByCommand(Entity ent)
		{
			if(commandLayer=="") return;
			ent.Layer=commandLayer;
		}
		static void SetLayerByCurrent(Entity ent)
		{
			ent.Layer=CurrentLayer;
		}

		public string CommandLayer {
			get {
				return commandLayer;
			}
			set
			{
				var acDoc = App.DocumentManager.MdiActiveDocument;
				var acCurDb = acDoc.Database;
				
				bool success=false;
				using (var tr = acCurDb.TransactionManager.StartTransaction())
				{
					var lt = tr.GetObject(acCurDb.LayerTableId,OpenMode.ForRead) as LayerTable;
					foreach (ObjectId layerId in lt)
					{
						var layer = tr.GetObject (layerId, OpenMode.ForWrite) as LayerTableRecord;
						if(layer.Name==commandLayer) success=true;
					}
				}
				
				if(success) commandLayer = value;
			}
		}

		void GetCurrentLayer()
		{
			var acDoc = App.DocumentManager.MdiActiveDocument;
			var acCurDb = acDoc.Database;
			var acEd = acDoc.Editor;
			
			var layer_id = acCurDb.Clayer;
			
			using (var tr = acCurDb.TransactionManager.StartTransaction())
			{
				var lt = tr.GetObject(layer_id,OpenMode.ForRead) as LayerTableRecord;
				CurrentLayer=lt.Name;
			}
		}

		#region IDisposable implementation
		/*Когда используем экземпляр через using
		 * чтобы вернуть состояние после вызова команды
		 */
		string OldLayer;
		public PKLayerManager()
		{
			OldLayer=commandLayer;
			GetCurrentLayer();
		}

		public void Dispose()
		{
			commandLayer=OldLayer;
		}

		#endregion
	}
	public enum LayerManagerOption
	{
		ByObject,
		ByCurentLayer,
		ByCommand,
	}
}
