/*
 * Создано в SharpDevelop.
 * Пользователь: PKorobkin
 * Дата: 16.03.2020
 * Время: 17:02
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors; //используется Color 

using PKUserTools.EditorInput;

namespace PKUserTools.Utilities
{
	/// <summary>
	/// Набор классов для установления свойств добавляемых объектов.
	/// Например, теущий цвет, толщина линий, тип линий и масштаб типа линий.
	/// На будущее - управление наборами свойств, именованные объекты и наборы объектов, возможно, с формой.
	/// Первоначальная цель - установление текущих рисуемых свойств и стилей на основе выбранного объекта.
	/// Естьт команда "СЛОЙУСТЕК" и она используется с псевдонимом "СТЕК" . Удобно будет сделать по этой команде установление не только слой текущим,
	/// но и свойств и стилей, если у объекта есть стили. (выноски, размеры, штриховки, полилинии, отрезки и т.п.)
	/// Реализовываться должно через классы-оболочки, в которых будет прописано какие свойства устанавливать в среду из объекта
	/// </summary>
	public class ChangeDrawingProperty: UtilityClass
	{
		public ChangeDrawingProperty()
		{
			var sset = Input.Objects("Выберите объекты для извлечения и установки свойств"); if(Input.StatusBad) return;
			Entity donorObject;
			using( var th = new TransactionHelper())
			{
				donorObject = th.ReadObject(sset[0].ObjectId) as Entity;
				if(donorObject==null) return;
			}
			
			// эта строчка будет меняться на код выбора нужных нам свойств исходя из того объекта, который нам попался
			ChangeLayer(donorObject.LayerId);
			ChangeLineWeight(donorObject.LineWeight);
			ChangeLineType(donorObject.LinetypeId);
			ChangeColor(donorObject.Color);
			ChangeLineTypeScele(donorObject.LinetypeScale);
		}
		void ChangeLayer(ObjectId layer)
		{
			acCurDb.Clayer=layer;
		}
		void ChangeLineWeight(LineWeight lw)
		{
			acCurDb.Celweight=lw; //Автодеск, блять, почему такие имена переменных
		}
		void ChangeLineType(ObjectId lineType)
		{
			acCurDb.Celtype=lineType; //сука, как разобраться и не обкуриться. Celtype это CurrentLineType;
		}
		void ChangeColor(Color c)
		{
			acCurDb.Cecolor=c;
		}
		void ChangeLineTypeScele(double scale)
		{
			acCurDb.Celtscale=scale;
		}
	}
}
