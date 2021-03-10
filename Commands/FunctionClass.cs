using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PKUserTools.Commands
{
    /// <summary>
    /// Класс, от которого наследуются классы, содержащие код функций
    /// </summary>
    abstract class FunctionClass:Utilities.UtilityClass
    {
       

        /// <summary>
        /// Через этот метод вызываются пользовательские команды в наследуемых классах
        /// </summary>
        public virtual void Execute()
        {
        }

        
    }
}
