using System;
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

            try
            {
                //System.Diagnostics.Debugger.Launch();
            }
            catch (Exception e)
            {
                Log.LogMessage(MessageImportance.High, e.ToString());

            }
            Log.LogMessage(MessageImportance.High, "Aloha");
            if (Inputs is null)
                Log.LogMessage(MessageImportance.High, "Inputs was null");
            else if (Inputs.Length == 0)
                Log.LogMessage(MessageImportance.High, "Inputs was empty");
            else
                Log.LogMessage(MessageImportance.High, string.Join(" - ", Inputs));
            Log.LogMessage(MessageImportance.High, $"Aloha {Inputs?.Length.ToString() ?? "NoInput"}");
            Log.LogMessage(MessageImportance.High, $"Output {Output}");
            var code = Generator.Generat(Inputs.First(), Inputs.Skip(1));
            System.IO.File.WriteAllText(Output, code);

            return true;
        }
    }
}
