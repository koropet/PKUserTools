using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using App = Autodesk.AutoCAD.ApplicationServices.Application;


namespace PKUserTools.Utilities
{
	//UPD если мы говорим про те классы, которые взаимодействуют с текущим чертежем 
    /// <summary>
    /// Содержит базовые свойства и методы для классов библиотеки
    /// </summary>
    public abstract class UtilityClass
    {
        /// <summary>
        /// Текущий документ
        /// </summary>
        protected Document acDoc;
        /// <summary>
        /// База данных чертежа текущего документа
        /// </summary>
        protected Database acCurDb;
        /// <summary>
        /// Редактор текущего документа
        /// </summary>
        protected Editor acEd;


        public UtilityClass()
        {
            Init();
        }
        /// <summary>
        /// Подключаем объекты приложения автокада
        /// </summary>
        protected void Init()
        {
            acDoc = App.DocumentManager.MdiActiveDocument;
            acCurDb = acDoc.Database;
            acEd = acDoc.Editor;
        }

        /// <summary>
        /// Сообщение в командную строку
        /// </summary>
        /// <param name="Message">Данные</param>
        protected void Tweet(object Message)
        {
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage(Message.ToString() + "\n");
        }
        /// <summary>
        /// Сообщение во всплывающий модальный диалог
        /// </summary>
        /// <param name="Message">Данные</param>
        protected void Alert(object Message)
        {
            App.ShowAlertDialog(Message.ToString());
        }
    }
}
