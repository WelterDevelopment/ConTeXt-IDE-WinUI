using System;
using System.Collections.Generic;
using System.Text;

namespace ConTeXt_IDE.Models
{
    public enum ContextCommandOptionValueType { DIMENSION, TEXT, NUMBER, COMMAND, STYLECOMMAND };

    public class ContextCommandOptionValue
    {
        public string Value { get; set; }

        public ContextCommandOptionValueType Type { get; set; }
    }
}
