﻿// --------- code start -------------

using System;
using System.Collections;
using System.Configuration;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace SimpleTcpSocketWPF
{
    public class PortableSettingsProvider : SettingsProvider
    {

        const string SETTINGSROOT = "Settings";
        //XML Root Node

        public override void Initialize(string name, NameValueCollection col)
        {
            base.Initialize(this.ApplicationName, col);
        }

        public override string ApplicationName
        {
            get
            {
                if (Application.ProductName.Trim().Length > 0)
                {
                    return Application.ProductName;
                }
                else
                {
                    FileInfo fi = new FileInfo(Application.ExecutablePath);
                    return fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                }
            }
            set { }
            //Do nothing
        }

        public override string Name
        {
            get { return "PortableSettingsProvider"; }
        }
        public virtual string GetAppSettingsPath()
        {
            //Used to determine where to store the settings
            System.IO.FileInfo fi = new System.IO.FileInfo(Application.ExecutablePath);
            return fi.DirectoryName;
        }

        public virtual string GetAppSettingsFilename()
        {
            //Used to determine the filename to store the settings
            return this.ApplicationName + ".settings";
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection propvals)
        {
            //Iterate through the settings to be stored
            //Only dirty settings are included in propvals, and only ones relevant to this provider
            foreach (SettingsPropertyValue propval in propvals)
            {
                this.SetValue(propval);
            }

            try
            {
                this.SettingsXML.Save(Path.Combine(this.GetAppSettingsPath(), this.GetAppSettingsFilename()));
            }
            catch (Exception ex)
            {
            }
            //Ignore if cant save, device been ejected
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
        {
            //Create new collection of values
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();

            //Iterate through the settings to be retrieved
            foreach (SettingsProperty setting in props)
            {

                SettingsPropertyValue value = new SettingsPropertyValue(setting);
                value.IsDirty = false;
                value.SerializedValue = this.GetValue(setting);
                values.Add(value);
            }
            return values;
        }

        private XmlDocument _settingsXML = null;

        private XmlDocument SettingsXML
        {
            get
            {
                //If we dont hold an xml document, try opening one.  
                //If it doesnt exist then create a new one ready.
                if (this._settingsXML == null)
                {
                    this._settingsXML = new XmlDocument();

                    try
                    {
                        this._settingsXML.Load(Path.Combine(this.GetAppSettingsPath(), this.GetAppSettingsFilename()));
                    }
                    catch (Exception ex)
                    {
                        //Create new document
                        XmlDeclaration dec = this._settingsXML.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                        this._settingsXML.AppendChild(dec);

                        XmlNode nodeRoot = default(XmlNode);

                        nodeRoot = this._settingsXML.CreateNode(XmlNodeType.Element, SETTINGSROOT, "");
                        this._settingsXML.AppendChild(nodeRoot);
                    }
                }

                return this._settingsXML;
            }
        }

        private string GetValue(SettingsProperty setting)
        {
            string ret = "";

            try
            {
                if (this.IsRoaming(setting))
                {
                    ret = this.SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + setting.Name).InnerText;
                }
                else
                {
                    ret = this.SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + Environment.MachineName + "/" + setting.Name).InnerText;
                }
            }

            catch (Exception ex)
            {
                if ((setting.DefaultValue != null))
                {
                    ret = setting.DefaultValue.ToString();
                }
                else
                {
                    ret = "";
                }
            }

            return ret;
        }

        private void SetValue(SettingsPropertyValue propVal)
        {

            XmlElement MachineNode = default(XmlElement);
            XmlElement SettingNode = default(XmlElement);

            //Determine if the setting is roaming.
            //If roaming then the value is stored as an element under the root
            //Otherwise it is stored under a machine name node 
            try
            {
                if (this.IsRoaming(propVal.Property))
                {
                    SettingNode = (XmlElement)this.SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + propVal.Name);
                }
                else
                {
                    SettingNode = (XmlElement)this.SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + Environment.MachineName + "/" + propVal.Name);
                }
            }
            catch (Exception ex)
            {
                SettingNode = null;
            }

            //Check to see if the node exists, if so then set its new value
            if ((SettingNode != null))
            {
                SettingNode.InnerText = propVal.SerializedValue.ToString();
            }
            else
            {
                if (this.IsRoaming(propVal.Property))
                {
                    //Store the value as an element of the Settings Root Node
                    SettingNode = this.SettingsXML.CreateElement(propVal.Name);
                    SettingNode.InnerText = propVal.SerializedValue.ToString();
                    this.SettingsXML.SelectSingleNode(SETTINGSROOT).AppendChild(SettingNode);
                }
                else
                {
                    //Its machine specific, store as an element of the machine name node,
                    //creating a new machine name node if one doesnt exist.
                    try
                    {

                        MachineNode = (XmlElement)this.SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + Environment.MachineName);
                    }
                    catch (Exception ex)
                    {
                        MachineNode = this.SettingsXML.CreateElement(Environment.MachineName);
                        this.SettingsXML.SelectSingleNode(SETTINGSROOT).AppendChild(MachineNode);
                    }

                    if (MachineNode == null)
                    {
                        MachineNode = this.SettingsXML.CreateElement(Environment.MachineName);
                        this.SettingsXML.SelectSingleNode(SETTINGSROOT).AppendChild(MachineNode);
                    }

                    SettingNode = this.SettingsXML.CreateElement(propVal.Name);
                    SettingNode.InnerText = propVal.SerializedValue.ToString();
                    MachineNode.AppendChild(SettingNode);
                }
            }
        }

        private bool IsRoaming(SettingsProperty prop)
        {
            //Determine if the setting is marked as Roaming
            foreach (DictionaryEntry d in prop.Attributes)
            {
                Attribute a = (Attribute)d.Value;
                if (a is System.Configuration.SettingsManageabilityAttribute)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

// --------- code end -------------