using System;
using System.Collections.Generic;
using System.Text;

namespace ConTeXt_IDE.Models
{
    public class ContextCommandOption
    {
        public string Option { get; set; }

        public List<ContextCommandOptionValue> Values { get; set; }
    }
}
