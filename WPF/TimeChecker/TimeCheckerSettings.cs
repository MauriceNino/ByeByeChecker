using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TimeChecker
{
    [Serializable()]
    public class TimeCheckerSettings
    {
        [System.Xml.Serialization.XmlElement("timeFormatRegex")]
        public string timeFormatRegex { get; set; }

        [System.Xml.Serialization.XmlArray("times")]
        public string[] times { get; set; }

        [System.Xml.Serialization.XmlElement("autoStartWhenTyping")]
        public bool autoStartWhenTyping { get; set; }
    }
}
