﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EA;

namespace Arkitektum.Statkart.ShapeChange.EA.Addin
{
    public class Main
    {
        private bool m_ShowFullMenus = false;

        //Called Before EA starts to check Add-In Exists
        public String EA_Connect(Repository Repository)
        {
            //No special processing required.
            return "a string";
        }

        //Called when user Click Add-Ins Menu item from within EA.
        //Populates the Menu with our desired selections.
        public object EA_GetMenuItems(Repository Repository, string Location, string MenuName)
        {
            
            switch (MenuName)
            {
                case "":
                    
                    return "-&ShapeChange";
                case "-&ShapeChange":
                    string[] ar = { "&Transform...", "About..." };
                    return ar;
            
            }
            return "";
        }

        //Sets the state of the menu depending if there is an active project or not
        bool IsProjectOpen(Repository Repository)
        {
            try
            {
                Collection c = Repository.Models;
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Called once Menu has been opened to see what menu items are active.
        public void EA_GetMenuState(Repository Repository, string Location, string MenuName, string ItemName, ref bool IsEnabled, ref bool IsChecked)
        {
            if (IsProjectOpen(Repository))
            {
                if (ItemName == "About...")
                    IsChecked = m_ShowFullMenus;
               
            }
            else
                // If no open project, disable all menu options
                IsEnabled = false;
        }

        //Called when user makes a selection in the menu.
        //This is your main exit point to the rest of your Add-in
        public void EA_MenuClick(Repository Repository, string Location, string MenuName, string ItemName)
        {
            switch (ItemName)
            {
                case "&Transform...":
                    //if (Repository.GetTreeSelectedPackage().StereotypeEx.ToLower() == "applicationschema")
                    //{
                        frmGML frm = new frmGML();
                        frm.SetRepository(Repository);
                        frm.ShowDialog();
                    //}
                    //else
                    //    MessageBox.Show("Please select a package with stereotype applicationSchema.",
                    //                    "Missing data");
                    
                    break;
                
                //case "Generer &WSDL...":

                //    frmWsdlXsd frmWsdl = new frmWsdlXsd();
                //    frmWsdl.SetRepository(Repository);
                //    frmWsdl.ShowDialog();
                //    break;
                
                case "About...":
                    frmAbout anAbout = new frmAbout();
                    anAbout.ShowDialog();
                    break;
            }
        }


        public string WriteToOutputwindow(Repository repository, object args)
        {
            string[] currentpackage = (string[])args;
            string melding = currentpackage[0];
            repository.WriteOutput("System", melding, 0);
            return "";
        }

        




    }
}
