namespace ILMerge.Fody
{
    using System;
    using System.Collections.Generic;

    using global::Fody;

    public class ModuleWeaver : BaseModuleWeaver
    {
        public override void Execute()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }
    }
}
