using System;
using System.Linq;
using Microsoft.Build.Framework;

namespace Fluent.Generator
{
    public class GenerateFtlTask : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string[] Inputs { get; set; }


        [Required]
        public string Output { get; set; }
        public override bool Execute()
        {

            var code = Generator.Generat(Inputs.First(), Inputs.Skip(1));
            System.IO.File.WriteAllText(Output, code);

            return true;
        }
    }
}
