﻿using ConfigMaker.Csgo.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigMaker.Csgo.Config.interfaces
{
    public interface IEntry
    {
        ConfigEntry PrimaryKey { get; set; }
        Executable Cmd { get; set; }
        EntryType Type { get; set; }
        bool IsMetaScript { get; set; }
        CommandCollection Dependencies { get; set; }
    }
}
