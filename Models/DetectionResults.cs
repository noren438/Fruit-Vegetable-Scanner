using System;
using System.Collections.Generic;
using System.Text;

namespace Frugt_Grønt_Scanner.Models
{
    public class DetectionResults
    {
        public string ProduktNavn { get; set; } = "Ukendt Produkt";
        public string ProduktKode { get; set; } = "0000";
        public string Type { get; set; } = "Ukendt";
        public float Confidence { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<DetectedItem> DetectedItems { get; set; } = new();
    }
}
