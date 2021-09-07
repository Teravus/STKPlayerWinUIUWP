﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STKPlayer
{
    public class SaveDefinition
    {
        public string SaveRowType { get; set; }
        public string SaveName { get; set; }
        public string SaveScene { get; set; }
        public int SaveSceneInt { get; set; }
        public int SaveFrame { get; set; }
        public int DoNothingCount { get; set; }
    }
}
