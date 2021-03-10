using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace PKUserTools.Utilities
{
	class TransactionHelper : UtilityClass, IDisposable
	{
		Transaction acTrans;
		BlockTable acBlkTbl;
		BlockTableRecord acBlkTblRec;

		/// <summary>
		/// Здесь хранится наименование записи таблицы блоков, из которой читаем. По умолчанию моделспейс
		/// </summary>
		string btr = BlockTableRecord.ModelSpace;

		/// <summary>
		/// Создать транзакцию в текущем пространстве листа или модели
		/// </summary>
		public TransactionHelper()
		{
			base.Init();
			acTrans = acDoc.TransactionManager.StartTransaction();
			
			var lm = LayoutManager.Current;
			btr = Utilities.btr_from_layout(lm.CurrentLayout);
		}
		/// <summary>
		/// Создать транзакцию в заданном пространстве
		/// </summary>
		/// <param name="Bloctablerec"></param>
		public TransactionHelper(string Bloctablerec)
		{
			base.Init();
			acTrans = acDoc.TransactionManager.StartTransaction();
			btr = Bloctablerec;
		}

		/// <summary>
		/// Читаем объекты из БД на основе списка выбора
		/// </summary>
		/// <param name="SSet">Список выбора, полученный из запроса на выбор объектов в редакторе</param>
		/// <returns>Список графических объектов</returns>
		public List<Entity> ReadObjects(SelectionSet SSet)
		{
			List<Entity> objects = new List<Entity>();

			List<ObjectId> ids = new List<ObjectId>();
			for (int i = 0; i < SSet.Count; i++)
			{
				ids.Add(SSet[i].ObjectId);
			}
			List<DBObject> dbobjects = ReadObjects(ids);

			for (int i = 0; i < SSet.Count; i++)
			{
				Entity ent = dbobjects[i] as Entity;
				objects.Add(ent);
			}

			return objects;
		}

		/// <summary>
		/// Читаем объекты из БД на основе списка номеров ИД
		/// </summary>
		/// <param name="ObjectIDs">Номера</param>
		/// <returns>Объекты БД</returns>
		public List<DBObject> ReadObjects(List<ObjectId> ObjectIDs)
		{
			List<DBObject> objects = new List<DBObject>();

			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForRead) as BlockTable;


			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForRead) as BlockTableRecord;

			for (int i = 0; i < ObjectIDs.Count; i++)
			{
				DBObject dbo = acTrans.GetObject(ObjectIDs[i],
				                                 OpenMode.ForRead) as DBObject;
				objects.Add(dbo);
			}
			return objects;

		}

		/// <summary>
		/// Чтение одного объекта БД по его ИД номеру
		/// </summary>
		/// <param name="ObjectId">ИД номер</param>
		/// <returns>объект БД</returns>
		public DBObject ReadObject(ObjectId ObjectId)
		{
			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForRead) as BlockTable;

			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForRead) as BlockTableRecord;

			DBObject dbo = acTrans.GetObject(ObjectId,
			                                 OpenMode.ForRead) as DBObject;

			return dbo;

		}
		public void WriteObjects(List<Entity> entities)
		{
			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForWrite) as BlockTable;

			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForWrite) as BlockTableRecord;
			foreach (Entity ent in entities)
			{
				PKLayerManager.SetLayer(ent);
				acBlkTblRec.AppendEntity(ent);
				acTrans.AddNewlyCreatedDBObject(ent, true);
			}
		}
		public void WriteObjects(List<DBObject> objects)
		{
			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForWrite) as BlockTable;

			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForWrite) as BlockTableRecord;
			foreach (DBObject dbo in objects)
			{
				if (dbo is Entity)
				{
					PKLayerManager.SetLayer(dbo as Entity);
					acBlkTblRec.AppendEntity(dbo as Entity);
					
				}
				else
				{
					//если это не графический объект
				}
				acTrans.AddNewlyCreatedDBObject(dbo, true);
			}
		}
		public void WriteObject(DBObject dbo)
		{
			if (dbo == null)
			{
				Tweet("Попытка записать в БД нулл");
				return;
			}
			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForWrite) as BlockTable;

			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForWrite) as BlockTableRecord;

			if (dbo is Entity)
			{
				PKLayerManager.SetLayer(dbo as Entity);
				acBlkTblRec.AppendEntity(dbo as Entity);
			}
			else
			{
				//если это не графический объект
			}
			acTrans.AddNewlyCreatedDBObject(dbo, true);

		}
		public DBObject EditObject(ObjectId adress)
		{
			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForWrite) as BlockTable;

			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForWrite) as BlockTableRecord;
			return acTrans.GetObject(adress, OpenMode.ForWrite);
		}

		public List<DBObject> EditObjects(List<ObjectId> ObjectIDs)
		{
			List<DBObject> objects = new List<DBObject>();

			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForWrite) as BlockTable;


			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForWrite) as BlockTableRecord;

			for (int i = 0; i < ObjectIDs.Count; i++)
			{
				DBObject dbo = acTrans.GetObject(ObjectIDs[i],
				                                 OpenMode.ForWrite) as DBObject;
				objects.Add(dbo);
			}
			return objects;

		}

		public List<Entity> EditObjects(SelectionSet SSet)
		{
			List<Entity> objects = new List<Entity>();

			List<ObjectId> ids = new List<ObjectId>();
			for (int i = 0; i < SSet.Count; i++)
			{
				ids.Add(SSet[i].ObjectId);
			}
			List<DBObject> dbobjects = EditObjects(ids);

			for (int i = 0; i < SSet.Count; i++)
			{
				Entity ent = dbobjects[i] as Entity;
				objects.Add(ent);
			}

			return objects;
		}
		public void Erase(List<DBObject> dbobjects)
		{
			foreach(var dbo in dbobjects)
			{
				dbo.Erase();
			}
		}
		public void Erase(SelectionSet SSet)
		{
			List<DBObject> objects = new List<DBObject>();

			acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
			                             OpenMode.ForWrite) as BlockTable;


			acBlkTblRec = acTrans.GetObject(acBlkTbl[btr],
			                                OpenMode.ForWrite) as BlockTableRecord;

			for (int i = 0; i < SSet.Count; i++)
			{
				DBObject dbo = acTrans.GetObject(SSet[i].ObjectId,
				                                 OpenMode.ForWrite) as DBObject;
				dbo.Erase();
			}

		}

		/// <summary>
		/// Задаем запись таблицы блоков, с которой работаем
		/// </summary>
		public string BlockTablerecord
		{
			set
			{
				btr = value;
			}
		}
		public void Dispose()
		{
			acTrans.Commit();
			acTrans.Dispose();
		}
		
		public void AddNewlyCreatedDBObject(DBObject obj,bool add)
		{
			acTrans.AddNewlyCreatedDBObject(obj,add);
		}
	}
}
