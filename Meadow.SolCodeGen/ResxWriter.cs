using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Meadow.SolCodeGen
{
    class ResxWriter
    {

        const string RESX_XML = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
</root>";

        XDocument _doc;

        Dictionary<string, string> _resources = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Resources => _resources;

        public ResxWriter()
        {
            _doc = XDocument.Parse(RESX_XML, LoadOptions.PreserveWhitespace);
        }

        public void AddEntry(string name, string value)
        {
            _resources.Add(name, value);
            var dataElement = new XElement("data", new XElement("value", value));
            dataElement.SetAttributeValue("name", name);
            dataElement.SetAttributeValue(XNamespace.Xml + "space", "preserve");
            _doc.Root.Add(dataElement);
        }

        public void Save(StreamWriter outputStream)
        {
            _doc.Save(outputStream, SaveOptions.DisableFormatting);
        }

        public void Save(TextWriter outputStream)
        {
            _doc.Save(outputStream, SaveOptions.DisableFormatting);
        }

        public void Save(Stream outputStream)
        {
            _doc.Save(outputStream, SaveOptions.DisableFormatting);
        }
    }
}
