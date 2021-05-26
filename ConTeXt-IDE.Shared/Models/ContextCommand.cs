using System;
using System.Collections.Generic;
using System.Text;

namespace ConTeXt_IDE.Models
{
    public class ContextCommand
    {
        public ContextCommand(string command, List<ContextCommandOptiongroup> optiongroups)
        {
            Command = command;
            Optiongroups = optiongroups;
        }

        public string Command { get; set; }

        public List<ContextCommandOptiongroup> Optiongroups { get; set; }
    }
}
