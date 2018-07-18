﻿using ConfigMaker.Csgo.Config.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigMaker.Csgo.Commands;
using System.Xml.Serialization;

namespace ConfigMaker.Csgo.Config.Entries
{
    [XmlInclude(typeof(ParametrizedEntry<string[]>))]
    [XmlInclude(typeof(ParametrizedEntry<string>))]
    [XmlInclude(typeof(ParametrizedEntry<int[]>))]
    [XmlInclude(typeof(ParametrizedEntry<int>))]
    [XmlInclude(typeof(ParametrizedEntry<double[]>))]
    [XmlInclude(typeof(ParametrizedEntry<double>))]
    [XmlInclude(typeof(ParametrizedEntry<Entry[]>))]
    [XmlInclude(typeof(ParametrizedEntry<Entry>))]
    public class Entry : IEntry
    {
        public ConfigEntry PrimaryKey { get; set; }
        public Executable Cmd { get; set; }
        public EntryType Type { get; set; }
        public bool IsMetaScript { get; set; }
        public CommandCollection Dependencies { get; set; } = new CommandCollection();

        
    }
}
