using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

[assembly: ExtensionApplication(typeof(PKUserTools.Application.AppClass))]
[assembly: CommandClass(typeof(PKUserTools.Commands.Commands))]

namespace PKUserTools.Application
{
    public class AppClass : Utilities.UtilityClass, IExtensionApplication
    {
    	StreamWriter swr;

        public void Initialize()
        {
            swr=new StreamWriter(@"C:\ProgramData\Autodesk\ApplicationPlugins\PKUserTools.bundle\PKUserToolsLog.txt");
            swr.AutoFlush=true;
            
			Console.SetOut(swr);
			
			PKUserTools.Utilities.PKLayerManager.Mode=PKUserTools.Utilities.LayerManagerOption.ByCommand;//TEMPORARY TODO Оформить в форму выбра опции
			
			Tweet("Плагин загружен");
			Console.WriteLine("Плагин загружен " + string.Format("{0:F}",DateTime.Now));
        }

        public void Terminate()
        {
        	
        	
            Tweet("Закрываем плагин");
            
            Console.WriteLine("Закрываем плагин " + string.Format("{0:F}",DateTime.Now));
            swr.Close();
        }
    }
}
