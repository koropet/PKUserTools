using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.Utilities
{
    /// <summary>
    /// Система сообщений пользователю
    /// </summary>
    static class Messaging
    {
        /// <summary>
        /// Сообщение в командную строку
        /// </summary>
        /// <param name="Message">Данные</param>
        public static void Tweet(object Message)
        {
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage(Message.ToString() + "\n");
        }
        /// <summary>
        /// Сообщение во всплывающий модальный диалог
        /// </summary>
        /// <param name="Message">Данные</param>
        public static void Alert(object Message)
        {
            App.ShowAlertDialog(Message.ToString());
        }
    }
}
