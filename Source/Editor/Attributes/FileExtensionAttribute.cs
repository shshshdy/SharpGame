using System;
using System.Collections.Generic;
using System.Text;

namespace UniqueEditor
{
    public class FileExtensionAttribute : Attribute
    {
        public string[] Ext { get; }
        public FileExtensionAttribute(string ext)
        {
            Ext = ext.Split(new char[] { ' ', ';', ',' }, StringSplitOptions.None);
        }
    }
}
