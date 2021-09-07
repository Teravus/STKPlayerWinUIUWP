using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STKPlayer
{
    public static class Utilities
    {
        public static long Frames15fpsToMS(int frames)
        {
            long result = (long)(frames * 66.666666666666666666666666666667f);
            return result;
        }
        public static long MsTo15fpsFrames(long ms)
        {
            int frames = (int)((float)ms / 66.666666666666666666666666666667f);
            return frames;
        }

        // Find next forward direction scene based on SceneMS
        public static SceneDefinition FindNextMainScene(List<SceneDefinition> options, long SceneMS)
        {
            SceneDefinition nextScene = null;
            long SmallestDifferenceGreaterThanZero = 999999999999999999;
            foreach (var scene in options)
            {
                if (scene.SceneType != SceneType.Main)
                    continue;

                var diff = scene.StartMS - SceneMS;
                if (diff>0 && diff < SmallestDifferenceGreaterThanZero)
                {
                    nextScene = scene;
                    SmallestDifferenceGreaterThanZero = diff;
                }
            }
            return nextScene;
        }
        public static string GetReplayingAudioFromSceneName(string sceneName)
        {
            if (sceneName == null)
                return null;
            //V005
            //V018_0
            //R000200
            sceneName = sceneName.ToUpperInvariant();
            var scenenameArr = sceneName.Split('_');
            string chapter = scenenameArr[0].Substring(2, scenenameArr[0].Length - 2);
            return string.Format("R00{0}00", chapter);

        }
        public static AspectRatioMaxResult GetMax(double width, double height, double AspectDecimal)
        {
            var heightbywidth = width / AspectDecimal;
            var widthbyheight = height * AspectDecimal;
            string direction = string.Empty;
            int length = 0;
            System.Diagnostics.Debug.WriteLine(String.Format("\tAspect:({0},{1})-{2},{3}", width, height, heightbywidth, widthbyheight));
            if (widthbyheight < width)
            {
                direction = "W";
                length = (int)widthbyheight;
            }
            if (heightbywidth < height)
            {
                direction = "H";
                length = (int)heightbywidth;
            }

            return new AspectRatioMaxResult() { Direction = direction, Length = length };
            // we know if height is a certain thing and it isn't in ratio
        }

        public static bool ValidateSaveGameName(string SaveName)
        {
            bool GoodYN = true;

            if (SaveName.Contains(","))
                GoodYN = false;

            if (SaveName.Contains("\n"))
                GoodYN = false;

            if (SaveName.Contains("\r"))
                GoodYN = false;
            
            if (SaveName.Contains("<"))
                GoodYN = false;
            if (SaveName.Contains(">"))
                GoodYN = false;

            if (SaveName.Contains(";"))
                GoodYN = false;


            return GoodYN;
        }
    }
}
