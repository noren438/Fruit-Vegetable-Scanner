using System;
using System.Collections.Generic;
using System.Text;

namespace Frugt_Grønt_Scanner.Models
{
    public class YoloBox
    {
        public int ClassIndex { get; set; }
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

    }
}
