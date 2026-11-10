using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSD.Common.Models
{

    public class NodeAttribute
    {
        public string id { get; set; }
        public string title { get; set; }
        public bool selected { get; set; }  
    }

    public class treeNode
    {
        public NodeAttribute attr { get; set; }

        public string data { get; set; }

        /// <summary>
        /// closed or open
        /// </summary>
        public string state { get; set; }

        public List<treeNode> children { get; set; }
        public treeNode()
        {
            children = new List<treeNode>();
        }
    }
}
