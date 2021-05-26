using ConTeXt_IDE.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConTeXt_IDE.Models
{
    public class ContextCommandList
    {
        public List<ContextCommand> ContextCommands = new List<ContextCommand>()
        {
            new ContextCommand("startsection", new List<ContextCommandOptiongroup>())


        };
    }
}