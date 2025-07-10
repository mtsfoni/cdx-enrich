using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;

namespace CdxEnrich.Actions
{
    public interface IReplaceAction
    {
        public InputTuple Execute(InputTuple input);

        public Result<ConfigRoot> CheckConfig(ConfigRoot config);

        public Result<InputTuple> CheckBomAndConfigCombination(InputTuple inputs);
    }
    
    public abstract class ReplaceAction : IReplaceAction
    {
        public abstract InputTuple Execute(InputTuple input);

        public abstract Result<ConfigRoot> CheckConfig(ConfigRoot config);
        
        public virtual Result<InputTuple> CheckBomAndConfigCombination(InputTuple inputs)
        {
            return new Ok<InputTuple>(inputs);
        }
    }
}