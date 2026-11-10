using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class checkElement
    {
        public decimal m_TOP { get; set; }
        public decimal m_Left { get; set; }
        public string m_FontName { get; set; }
        public int m_FontSize { get; set; }
        public string m_TextAlign { get; set; }
        public string m_fontweight { get; set; }
        public decimal m_Width { get; set; }
        public decimal m_height { get; set; }
        public string m_Direction { get; set; }
        public string ChequeLable { get; set; }
        public string labelValue { get; set; }
        public string labelMapToField { get; set; }
    }
}