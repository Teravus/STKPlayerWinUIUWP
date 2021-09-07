using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace STKPlayer
{
    public static class SceneLoader
    {
        public static List<SceneDefinition> LoadScenesFromAsset(string FileName)
        {
            List<SceneDefinition> defs = new List<SceneDefinition>();
            string[] lines = File.ReadAllLines(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", FileName));
            if (lines.Length > 0)
            {
                for(var i=0;i<lines.Length;i++)
                {
                    string[] linevals = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (linevals.Length > 0)
                    {
                        if (linevals[0].StartsWith("DN"))
                        {
                            switch (linevals.Length)
                            {
                                case 5:
                                    defs.Add(new SceneDefinition(SceneType.Inaction, linevals[0], Convert.ToInt32(linevals[2]), Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[1])));
                                    break;
                                case 4: // Older version of DM Line.  No Offset.
                                    defs.Add(new SceneDefinition(SceneType.Inaction, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0));
                                    break;
                                default:
                                    // I dunno
                                    break;
                            }
                        }
                        else
                        {
                            switch (linevals.Length)
                            {
                                //V012 2 23610 26009 26123 0 25675 v012.clu
                                case 8:
                                    defs.Add(new SceneDefinition(SceneType.Main, linevals[0], Convert.ToInt32(linevals[2]), Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[1]),Convert.ToInt32(linevals[4]), Convert.ToInt32(linevals[6])));
                                    break;
                                case 7: // Older version that has CDs of above.  No offset.
                                    defs.Add(new SceneDefinition(SceneType.Main, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0, Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[5])));
                                    break;
                                case 6:
                                    defs.Add(new SceneDefinition(SceneType.Bad, linevals[0], Convert.ToInt32(linevals[2]), Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[1]), 0, Convert.ToInt32(linevals[4])));
                                    break;
                                case 5:// Older version of above that has Cds.  No offset.
                                    if (linevals[0].StartsWith("ip"))
                                    {
                                        defs.Add(new SceneDefinition(SceneType.Info, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0));
                                    }
                                    else 
                                        defs.Add(new SceneDefinition(SceneType.Bad, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0, 0, Convert.ToInt32(linevals[3])));
                                    break;
                                default:
                                    // I dunno
                                    break;
                            }
                        }
                    }
                }
            }
            // Looking for hierarchical main track, alternate track options
            for (int i=0; i< defs.Count;i++)
            {
                for (int j=0;j<defs.Count;j++)
                {
                    if (defs[j].Name.Contains(defs[i].Name) && defs[j].SceneType == SceneType.Bad && defs[i].SceneType== SceneType.Main)
                    {
                        defs[i].ParentScene = defs[j];
                    }
                }
            }
            return defs;
        }

        public static List<SceneDefinition> LoadSupportingScenesFromAsset(string FileName)
        {
            List<SceneDefinition> defs = new List<SceneDefinition>();
            string[] lines = File.ReadAllLines(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", FileName));
            if (lines.Length > 0)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    string[] linevals = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (linevals.Length > 0)
                    {
                        defs.Add(new SceneDefinition() { SceneType=SceneType.Info,  Name = linevals[0], OffsetTimeMS = Convert.ToInt32(linevals[1]), FrameStart = Convert.ToInt32(linevals[2]), FrameEnd = Convert.ToInt32(linevals[3]) });
                    }
                }
            }

            return defs;
        }
        
    }
}
